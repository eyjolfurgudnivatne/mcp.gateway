namespace Mcp.Gateway.Client;

using Mcp.Gateway.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

/// <summary>
/// HTTP transport implementation for MCP.
/// Uses HTTP POST for RPC calls.
/// Supports server-initiated notifications via SSE if enabled.
/// </summary>
public class HttpMcpTransport : IMcpTransport
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly Channel<JsonRpcMessage> _incomingMessages = Channel.CreateUnbounded<JsonRpcMessage>();
    private readonly bool _ownsHttpClient;
    private readonly bool _enableSse;
    
    private string? _sessionId;
    private Task? _sseTask;
    private CancellationTokenSource? _sseCts;
    private bool _disposed;

    public bool IsBidirectional => _enableSse;

    public HttpMcpTransport(string baseUrl, string endpoint = "/mcp", bool enableSse = false)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _endpoint = endpoint;
        _ownsHttpClient = true;
        _enableSse = enableSse;
    }

    public HttpMcpTransport(HttpClient httpClient, string endpoint = "/mcp", bool enableSse = false)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _endpoint = endpoint;
        _ownsHttpClient = false;
        _enableSse = enableSse;
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        // HTTP is stateless, no connection needed initially.
        // SSE connection will be established after first request (when session ID is obtained).
        return Task.CompletedTask;
    }

    public async Task SendAsync(JsonRpcMessage message, CancellationToken ct = default)
    {
        // Create request manually to handle headers
        var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
        request.Content = JsonContent.Create(message, options: JsonOptions.Default);
        
        if (!string.IsNullOrEmpty(_sessionId))
        {
            request.Headers.Add("MCP-Session-Id", _sessionId);
        }

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        // Capture Session ID
        if (response.Headers.TryGetValues("MCP-Session-Id", out var values))
        {
            var newSessionId = values.FirstOrDefault();
            if (!string.IsNullOrEmpty(newSessionId) && _sessionId != newSessionId)
            {
                _sessionId = newSessionId;
                if (_enableSse)
                {
                    StartSseIfNotRunning();
                }
            }
        }

        // Read response
        // Note: Notifications might return 204 No Content
        if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
        {
            // Check content type for JSON Lines (streaming)
            // Or just try to read multiple JSON objects if possible.
            // ReadFromJsonAsync reads a single object.
            
            // If the response contains multiple JSON objects (JSON Lines), ReadFromJsonAsync might fail or only read the first one.
            // We need to handle streaming responses.
            
            // Let's read as stream and parse manually.
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);
            
            while (true)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null) break; // EOF
                
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                try
                {
                    var responseMessage = JsonSerializer.Deserialize<JsonRpcMessage>(line, JsonOptions.Default);
                    if (responseMessage != null)
                    {
                        // Fix ID if it's a JsonElement (System.Text.Json deserializes object properties as JsonElement)
                        var fixedMsg = NormalizeMessageId(responseMessage);
                        await _incomingMessages.Writer.WriteAsync(fixedMsg, ct);
                    }
                }
                catch (JsonException)
                {
                    // Ignore invalid lines
                }
            }
        }
    }

    private void StartSseIfNotRunning()
    {
        if (_sseTask != null && !_sseTask.IsCompleted) return;

        _sseCts = new CancellationTokenSource();
        _sseTask = SseLoopAsync(_sseCts.Token);
    }

    private async Task SseLoopAsync(CancellationToken ct)
    {
        string? lastEventId = null;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, _endpoint);
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
                
                if (!string.IsNullOrEmpty(_sessionId))
                {
                    request.Headers.Add("MCP-Session-Id", _sessionId);
                }
                
                if (lastEventId != null)
                {
                    request.Headers.Add("Last-Event-ID", lastEventId);
                }

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                
                if (!response.IsSuccessStatusCode)
                {
                    // If session expired (404), we might need to stop or re-init. 
                    // For now, just wait and retry (or exit if 404?)
                    // SESSION_MANAGEMENT.md says 404 means session expired.
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Session expired. Stop SSE loop.
                        // Client will need to re-initialize (which creates new session).
                        _sessionId = null; 
                        return; 
                    }
                    
                    await Task.Delay(2000, ct);
                    continue;
                }

                using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var reader = new StreamReader(stream);

                string? currentEvent = null;
                string? currentData = null;

                while (!ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(ct);
                    if (line == null) break;

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        // End of event
                        if (!string.IsNullOrEmpty(currentData))
                        {
                            // Process event
                            if (currentEvent == "message" || currentEvent == null)
                            {
                                try 
                                {
                                    var message = JsonSerializer.Deserialize<JsonRpcMessage>(currentData, JsonOptions.Default);
                                    if (message != null)
                                    {
                                        message = NormalizeMessageId(message);
                                        await _incomingMessages.Writer.WriteAsync(message, ct);
                                    }
                                }
                                catch { /* Ignore parse errors */ }
                            }
                        }
                        currentEvent = null;
                        currentData = null;
                        continue;
                    }

                    var parts = line.Split(':', 2);
                    var field = parts[0].Trim();
                    var value = parts.Length > 1 ? parts[1].Trim() : string.Empty;

                    switch (field)
                    {
                        case "id":
                            lastEventId = value;
                            break;
                        case "event":
                            currentEvent = value;
                            break;
                        case "data":
                            currentData = value; // Note: Simple implementation, doesn't handle multi-line data
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception)
            {
                // Connection error, wait and retry
                try { await Task.Delay(2000, ct); } catch { }
            }
        }
    }

    private static JsonRpcMessage NormalizeMessageId(JsonRpcMessage message)
    {
        if (message.Id is JsonElement idElem)
        {
            object? fixedId = idElem.ValueKind switch
            {
                JsonValueKind.String => idElem.GetString(),
                JsonValueKind.Number => idElem.TryGetInt32(out var i) ? i : idElem.GetInt64(),
                JsonValueKind.Null => null,
                _ => idElem.ToString()
            };
            return message with { Id = fixedId };
        }
        return message;
    }

    public IAsyncEnumerable<JsonRpcMessage> ReceiveLoopAsync(CancellationToken ct = default)
    {
        return _incomingMessages.Reader.ReadAllAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _sseCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Ignore if already disposed
        }

        if (_sseTask != null)
        {
            try { await _sseTask; } catch { }
        }
        _sseCts?.Dispose();

        _incomingMessages.Writer.TryComplete();
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
