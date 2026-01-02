#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Text.Json;
using System.Text.Json.Serialization;

// -----------------------------------------------------------------------------
// JsonOptions
// Centralized JSON serializer configuration used by all models
// -----------------------------------------------------------------------------
public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        // Ensures camelCase JSON output (jsonRpc → jsonrpc, etc.)
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        
        // NOTE: JSON Source Generators not enabled due to polymorphic object? types
        // See JsonSourceGenerationContext.cs for details
        // TypeInfoResolver = JsonSourceGenerationContext.Default
    };

    static JsonOptions()
    {
        Default.Converters.Add(new JsonStringEnumConverter());
    }
}

public sealed record JsonRpcMessage(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("method")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Method = null,
    [property: JsonPropertyName("id")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] object? Id = null,  // Changed from string? to object?
    [property: JsonPropertyName("params")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Params = null,
    [property: JsonPropertyName("result")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Result = null,
    [property: JsonPropertyName("error")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] JsonRpcError? Error = null)
{
    // Factory: Request
    public static JsonRpcMessage CreateRequest(string Method, object? Id = null, object? Params = null) =>
        new("2.0", Method: Method, Id: Id, Params: Params);

    // Factory: Notification
    public static JsonRpcMessage CreateNotification(string Method, object? Params = null) =>
        new("2.0", Method: Method, Id: null, Params: Params);

    // Factory: Success response
    public static JsonRpcMessage CreateSuccess(object? Id, object? Result = null) =>
        new("2.0", Id: Id, Result: Result);

    // Factory: Error response
    public static JsonRpcMessage CreateError(object? Id, JsonRpcError Error) =>
        new("2.0", Id: Id, Error: Error);

    [JsonIgnore] public bool IsRequest => Method is not null && Result is null && Error is null;
    [JsonIgnore] public bool IsNotification => IsRequest && Id is null;
    [JsonIgnore] public bool IsResponse => Method is null && Id is not null && (Result is not null || Error is not null);
    [JsonIgnore] public bool IsErrorResponse => IsResponse && Error is not null;
    [JsonIgnore] public bool IsSuccessResponse => IsResponse && Error is null;
    [JsonIgnore] public bool IsParams => IsRequest && Params is not null;
    
    // Helper to get ID as string for logging/comparison
    [JsonIgnore] public string? IdAsString => Id?.ToString();

    public JsonElement ToJsonElement() =>
        JsonDocument.Parse(JsonSerializer.Serialize(this, JsonOptions.Default)).RootElement.Clone();

    public JsonElement GetResult() =>
        JsonDocument.Parse(JsonSerializer.Serialize(Result, JsonOptions.Default)).RootElement.Clone();

    public JsonElement GetParams() =>
        JsonDocument.Parse(JsonSerializer.Serialize(Params, JsonOptions.Default)).RootElement.Clone();

    public T? GetResult<T>()
    {
        if (Result is null) return default;
        if (Result is T typed) return typed;
        if (Result is JsonElement je)
        {
            // 1) Prøv direkte deserialisering til T fra root
            try
            {
                var direct = je.Deserialize<T>(JsonOptions.Default);
                if (direct is not null)
                {
                    return direct;
                }
            }
            catch (JsonException)
            {
                // Ignorer
            }            
        }

        // 2) Fallback: serialize + deserialize (sjelden nødvendig)
        return JsonSerializer.Deserialize<T>(
            JsonSerializer.Serialize(Result, JsonOptions.Default),
            JsonOptions.Default);
    }

    // tools/call Wrap result in MCP content format
    public T? GetToolsCallResult<T>()
    {
        if (Result is null) return default;
        if (Result is T typed) return typed;
        if (Result is JsonElement je)
        {
            // 1) MCP content-format: { content: [ { text: "<json or text>" } ] }
            if (je.TryGetProperty("content", out var contentProp))
            {
                var contentItem = contentProp.EnumerateArray().FirstOrDefault();
                if (contentItem.ValueKind != JsonValueKind.Undefined &&
                    contentItem.TryGetProperty("text", out var textProp))
                {
                    // text kan være enten en ren streng eller et JSON-objekt
                    if (textProp.ValueKind == JsonValueKind.String)
                    {
                        var str = textProp.GetString();
                        if (!string.IsNullOrEmpty(str))
                        {
                            return JsonSerializer.Deserialize<T>(str, JsonOptions.Default);
                        }
                    }
                    else
                    {
                        return textProp.Deserialize<T>(JsonOptions.Default);
                    }
                }
            }
        }

        // 2) Fallback: serialize + deserialize (sjelden nødvendig)
        return JsonSerializer.Deserialize<T>(
            JsonSerializer.Serialize(Result, JsonOptions.Default),
            JsonOptions.Default);
    }

    public T? GetParams<T>()
    {
        if (Params is null) return default;
        if (Params is T typed) return typed;
        if (Params is JsonElement je) return je.Deserialize<T>(JsonOptions.Default);
        // fallback: serialize + deserialize (sjelden nødvendig)
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(Params, JsonOptions.Default), JsonOptions.Default);
    }

    public static bool TryGetFromJsonElement(JsonElement element, out JsonRpcMessage? message)
    {
        message = null;
        if (!element.TryGetProperty("jsonrpc", out var verProp) || verProp.GetString() != "2.0")
            return false;

        string? method = element.TryGetProperty("method", out var mProp) ? mProp.GetString() : null;
        
        // Preserve original ID type (number, string, or null)
        object? id = null;
        if (element.TryGetProperty("id", out var idProp))
        {
            id = idProp.ValueKind switch
            {
                JsonValueKind.String => idProp.GetString(),
                JsonValueKind.Number => idProp.TryGetInt32(out var i32) ? i32 : idProp.GetInt64(),
                JsonValueKind.Null => null,
                _ => idProp.GetRawText() // Fallback
            };
        }

        object? @params = null;
        if (element.TryGetProperty("params", out var pProp))
            @params = pProp.Deserialize<object>(JsonOptions.Default);

        object? result = null;
        if (element.TryGetProperty("result", out var rProp))
            result = rProp.Deserialize<object>(JsonOptions.Default);

        JsonRpcError? error = null;
        if (element.TryGetProperty("error", out var eProp))
            _ = JsonRpcError.TryGetFromJsonElement(eProp, out error);

        var candidate = new JsonRpcMessage("2.0", method, id, @params, result, error);

        // Spec-validering
        if (candidate.Method is not null)
        {
            // Request må ikke ha result/error
            if (candidate.Result is not null || candidate.Error is not null) return false;
        }
        else
        {
            // Response må ha id og enten result eller error
            if (candidate.Id is null) return false;
            if ((candidate.Result is null && candidate.Error is null) ||
                (candidate.Result is not null && candidate.Error is not null))
                return false;
        }

        message = candidate;
        return true;
    }
}

// -----------------------------------------------------------------------------
// JsonRpcError
// Represents a structured JSON‑RPC error object
// -----------------------------------------------------------------------------
public sealed record JsonRpcError(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] object? Data)
{
    // Convert strongly‑typed error into JSON element
    public JsonElement ToJsonElement() =>
        JsonDocument.Parse(JsonSerializer.Serialize(this, JsonOptions.Default)).RootElement.Clone();

    // Attempts to parse a JsonElement into a JsonRpcError
    public static bool TryGetFromJsonElement(JsonElement element, out JsonRpcError? message)
    {
        // Error must always contain "code" and "message"
        if (!element.TryGetProperty("code", out var codeProp) ||
            !element.TryGetProperty("message", out var messageProp))
        {
            message = null;
            return false;
        }

        var code = codeProp.GetInt32();
        var messageText = messageProp.GetString() ?? string.Empty;

        // Optional data
        object? data = element.TryGetProperty("data", out var dataProp)
            ? JsonSerializer.Deserialize<object>(dataProp.GetRawText())
            : null;

        message = new JsonRpcError(
            Code: code,
            Message: messageText,
            Data: data);

        return true;
    }
}

// -----------------------------------------------------------------------------
// ToolCapabilities
// Defines capabilities required by a tool (transport compatibility)
// -----------------------------------------------------------------------------
/// <summary>
/// Defines capabilities required by a tool.
/// Used to filter tools based on transport capabilities (stdio, http, ws, sse).
/// </summary>
[Flags]
public enum ToolCapabilities
{
    /// <summary>
    /// Standard JSON-RPC tool (works on all transports: stdio, http, ws, sse).
    /// This is the default capability for tools that don't require streaming.
    /// </summary>
    Standard = 1,
    
    /// <summary>
    /// Requires text streaming support (WebSocket or SSE).
    /// Tools with this capability can send JSON chunks in real-time.
    /// </summary>
    TextStreaming = 2,
    
    /// <summary>
    /// Requires binary streaming support (WebSocket only).
    /// Tools with this capability send raw binary data via WebSocket frames.
    /// </summary>
    BinaryStreaming = 4,
    
    /// <summary>
    /// Must use WebSocket transport (no HTTP/stdio support).
    /// Typically combined with BinaryStreaming.
    /// </summary>
    RequiresWebSocket = 8
}
