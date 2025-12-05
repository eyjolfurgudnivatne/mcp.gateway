namespace Mcp.Gateway.Tools;

using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

/// <summary>
/// Provides stdio mode support for MCP Gateway.
/// This allows tools to be invoked via standard input/output (stdin/stdout),
/// which is required for local MCP clients like GitHub Copilot and Claude Desktop.
/// </summary>
public static class StdioMode
{
    /// <summary>
    /// Runs the MCP Gateway in stdio mode (stdin/stdout transport).
    /// This is a blocking call that will read from stdin and write to stdout until EOF.
    /// </summary>
    /// <param name="services">Service provider with ToolInvoker registered</param>
    /// <param name="logPath">Optional file path for logging (if null, logging is disabled)</param>
    public static async Task RunAsync(IServiceProvider services, string? logPath = null)
    {
        // Simple file logger
        StreamWriter? logWriter = null;
        if (logPath != null)
        {
            logWriter = new StreamWriter(logPath, append: true) { AutoFlush = true };
            await logWriter.WriteLineAsync($"=== MCP Gateway stdio started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        }
        
        void Log(string message)
        {
            if (logWriter == null) return;
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            logWriter.WriteLine(line);
            System.Diagnostics.Debug.WriteLine(line);
        }
        
        try
        {
            Log("Creating service scope...");
            
            // Create a scope for scoped services like ToolInvoker
            using var scope = services.CreateScope();
            var toolInvoker = scope.ServiceProvider.GetRequiredService<ToolInvoker>();
            
            Log("ToolInvoker resolved successfully");
            
            using var stdin = Console.OpenStandardInput();
            using var stdout = Console.OpenStandardOutput();
            using var reader = new StreamReader(stdin);
            using var writer = new StreamWriter(stdout) { AutoFlush = true };
            
            Log("stdio streams opened");
            
            // Create cancellation token for graceful shutdown
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                Log("Ctrl+C received");
                e.Cancel = true;
                cts.Cancel();
            };
            
            Log("Entering main loop...");
            
            while (!cts.Token.IsCancellationRequested)
            {
                string? line;
                
                try
                {
                    // ReadLineAsync with cancellation token
                    line = await reader.ReadLineAsync(cts.Token);
                    
                    // Log only length, not full JSON (avoid potential issues)
                    Log($"Received line (length: {line?.Length ?? 0})");
                }
                catch (OperationCanceledException)
                {
                    Log("Read cancelled");
                    break;
                }
                
                // EOF signals shutdown
                if (line == null)
                {
                    Log("EOF received, shutting down");
                    break;
                }
                
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                {
                    Log("Empty line, skipping");
                    continue;
                }
                
                Log($"Processing: {line.Substring(0, Math.Min(100, line.Length))}...");
                
                try
                {
                    // Parse request
                    using var doc = JsonDocument.Parse(line);
                    
                    Log($"JSON parsed, invoking tool...");
                    
                    // Invoke via existing ToolInvoker
                    var response = await toolInvoker.InvokeSingleStdioAsync(doc.RootElement, cts.Token);
                    
                    Log($"Tool invoked, response null: {response == null}");
                    
                    // Send response (if not notification)
                    if (response != null)
                    {
                        var json = JsonSerializer.Serialize(response, JsonOptions.Default);
                        Log($"Sending response (length: {json.Length})");
                        
                        // Use synchronous WriteLine (proven to work with MCP clients)
                        writer.WriteLine(json);
                        
                        Log("Response sent");
                    }
                }
                catch (JsonException ex)
                {
                    Log($"JSON parse error: {ex.Message}");
                    
                    var error = ToolResponse.Error(null, -32700, "Parse error", new { detail = ex.Message });
                    var errorJson = JsonSerializer.Serialize(error, JsonOptions.Default);
                    writer.WriteLine(errorJson);
                }
                catch (Exception ex)
                {
                    Log($"Error: {ex.GetType().Name}: {ex.Message}");
                    
                    var error = ToolResponse.Error(null, -32603, "Internal error", new { detail = ex.Message });
                    var errorJson = JsonSerializer.Serialize(error, JsonOptions.Default);
                    writer.WriteLine(errorJson);
                }
            }
            
            Log("Exiting main loop");
        }
        finally
        {
            Log("stdio mode stopped");
            logWriter?.Dispose();
        }
    }
}
