#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// JsonOptions
/// Centralized JSON serializer configuration used by all models
/// </summary>
public static class JsonOptions
{
    /// <summary>
    /// Default JsonSerializerOptions used throughout the MCP Gateway
    /// </summary>
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

/// <summary>
/// Represents a JSON-RPC 2.0 message, which can be a request, notification, or response, including both success and
/// error responses.
/// </summary>
/// <remarks>This type provides a unified representation for all JSON-RPC 2.0 message types, including requests,
/// notifications, and both success and error responses. Use the provided factory methods to create messages of the
/// appropriate type. The properties and structure of the message must conform to the JSON-RPC 2.0 specification. The
/// type does not enforce thread safety.</remarks>
/// <param name="JsonRpc">The JSON-RPC protocol version. Must be "2.0" for compliance with the JSON-RPC 2.0 specification.</param>
/// <param name="Method">The name of the method to be invoked. Required for requests and notifications; otherwise, null for responses.</param>
/// <param name="Id">The identifier for correlating requests and responses. Can be a string, number, or null. Must be present for
/// responses; null for notifications.</param>
/// <param name="Params">The parameters to be passed to the method. Can be an array, an object, or null if the method does not require
/// parameters.</param>
/// <param name="Result">The result returned by a successful response. Should be null for requests and notifications.</param>
/// <param name="Error">The error object returned by an error response. Should be null for requests, notifications, and successful
/// responses.</param>
public sealed record JsonRpcMessage(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("method")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Method = null,
    [property: JsonPropertyName("id")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] object? Id = null,  // Changed from string? to object?
    [property: JsonPropertyName("params")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Params = null,
    [property: JsonPropertyName("result")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Result = null,
    [property: JsonPropertyName("error")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] JsonRpcError? Error = null)
{
    /// <summary>
    /// Creates a new JSON-RPC request message with the specified method, identifier, and parameters.
    /// </summary>
    /// <remarks>If <paramref name="Id"/> is null, the message is treated as a notification and no response
    /// will be returned by the server. The <paramref name="Params"/> argument can be an array, an object, or null,
    /// depending on the method's requirements.</remarks>
    /// <param name="Method">The name of the method to invoke on the remote server. Cannot be null or empty.</param>
    /// <param name="Id">An identifier established by the client to correlate the request with its response. May be null for
    /// notifications.</param>
    /// <param name="Params">The parameters to pass to the method. May be null if the method does not require parameters.</param>
    /// <returns>A <see cref="JsonRpcMessage"/> representing the JSON-RPC request message.</returns>
    public static JsonRpcMessage CreateRequest(string Method, object? Id = null, object? Params = null) =>
        new("2.0", Method: Method, Id: Id, Params: Params);

    /// <summary>
    /// Creates a new JSON-RPC notification message with the specified method and parameters.
    /// </summary>
    /// <remarks>A notification is a JSON-RPC message that does not expect a response from the remote
    /// endpoint. The "Id" field is always null in notification messages.</remarks>
    /// <param name="Method">The name of the method to invoke on the remote endpoint. Cannot be null or empty.</param>
    /// <param name="Params">An object representing the parameters to include with the notification. May be null if the method does not
    /// require parameters.</param>
    /// <returns>A <see cref="JsonRpcMessage"/> representing the notification message. The message will have a null identifier,
    /// as required by the JSON-RPC specification for notifications.</returns>
    public static JsonRpcMessage CreateNotification(string Method, object? Params = null) =>
        new("2.0", Method: Method, Id: null, Params: Params);

    /// <summary>
    /// Creates a new JSON-RPC message representing a successful response with the specified identifier and result.
    /// </summary>
    /// <param name="Id">The identifier that correlates the response to a specific request. Can be null for notifications or if the
    /// request did not include an identifier.</param>
    /// <param name="Result">The result value to include in the response. Can be null if the method does not return a value.</param>
    /// <returns>A <see cref="JsonRpcMessage"/> instance representing a successful JSON-RPC response with the specified
    /// identifier and result.</returns>
    public static JsonRpcMessage CreateSuccess(object? Id, object? Result = null) =>
        new("2.0", Id: Id, Result: Result);

    /// <summary>
    /// Creates a new JSON-RPC error message with the specified identifier and error details.
    /// </summary>
    /// <param name="Id">The identifier of the request that caused the error. Can be null for notifications or if the request identifier
    /// is unknown.</param>
    /// <param name="Error">The error details to include in the message. Must not be null.</param>
    /// <returns>A <see cref="JsonRpcMessage"/> representing a JSON-RPC error response containing the specified identifier and
    /// error information.</returns>
    public static JsonRpcMessage CreateError(object? Id, JsonRpcError Error) =>
        new("2.0", Id: Id, Error: Error);

    /// <summary>
    /// Is this a JSON-RPC request message?
    /// </summary>
    [JsonIgnore] public bool IsRequest => Method is not null && Result is null && Error is null;

    /// <summary>
    /// Is this a JSON-RPC notification message?
    /// </summary>
    [JsonIgnore] public bool IsNotification => IsRequest && Id is null;

    /// <summary>
    /// Is this a JSON-RPC response message (either success or error)?
    /// </summary>
    [JsonIgnore] public bool IsResponse => Method is null && Id is not null && (Result is not null || Error is not null);

    /// <summary>
    /// Is this a JSON-RPC error response message?
    /// </summary>
    [JsonIgnore] public bool IsErrorResponse => IsResponse && Error is not null;

    /// <summary>
    /// Is this a JSON-RPC success response message?
    /// </summary>
    [JsonIgnore] public bool IsSuccessResponse => IsResponse && Error is null;

    /// <summary>
    /// Is this a JSON-RPC request message with parameters?
    /// </summary>
    [JsonIgnore] public bool IsParams => IsRequest && Params is not null;

    /// <summary>
    /// Helper to get ID as string for logging/comparison.
    /// Gets the identifier as a string representation, or null if the identifier is not set.
    /// </summary>
    [JsonIgnore] public string? IdAsString => Id?.ToString();

    /// <summary>
    /// Converts the current object to a <see cref="JsonElement"/> representation using the default JSON serialization
    /// options.
    /// </summary>
    /// <remarks>The returned <see cref="JsonElement"/> is a deep clone and is independent of the original
    /// object. Changes to the object after calling this method are not reflected in the returned <see
    /// cref="JsonElement"/>.</remarks>
    /// <returns>A <see cref="JsonElement"/> that represents the serialized form of the current object.</returns>
    public JsonElement ToJsonElement() =>
        JsonDocument.Parse(JsonSerializer.Serialize(this, JsonOptions.Default)).RootElement.Clone();

    /// <summary>
    /// Gets the Result property as a JsonElement.
    /// </summary>
    /// <returns></returns>
    public JsonElement GetResult() =>
        JsonDocument.Parse(JsonSerializer.Serialize(Result, JsonOptions.Default)).RootElement.Clone();

    /// <summary>
    /// Gets the parameters as a JSON element suitable for serialization or inspection.
    /// </summary>
    /// <remarks>The returned <see cref="System.Text.Json.JsonElement"/> is detached from the underlying
    /// document and remains valid for the lifetime of the caller's code. This method is useful when parameters need to
    /// be passed to APIs expecting a <see cref="System.Text.Json.JsonElement"/> or for custom serialization
    /// scenarios.</remarks>
    /// <returns>A <see cref="System.Text.Json.JsonElement"/> representing the current parameters. The returned element is a deep
    /// clone and can be safely used or modified by the caller.</returns>
    public JsonElement GetParams() =>
        JsonDocument.Parse(JsonSerializer.Serialize(Params, JsonOptions.Default)).RootElement.Clone();

    /// <summary>
    /// Get the Result property deserialized to the specified type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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

    /// <summary>
    /// Attempts to extract and deserialize the result of a tools call to the specified type.
    /// </summary>
    /// <remarks>This method supports multiple result formats, including direct assignment, JSON element
    /// extraction, and fallback serialization. If the result cannot be converted to the specified type, <see
    /// langword="null"/> is returned.</remarks>
    /// <typeparam name="T">The type to which the result should be deserialized.</typeparam>
    /// <returns>An instance of type <typeparamref name="T"/> containing the deserialized result if successful; otherwise, <see
    /// langword="null"/>.</returns>
    public T? GetToolsCallResult<T>()
    {
        if (Result is null) return default;
        if (Result is T typed) return typed;
        if (Result is JsonElement je)
        {
            // 1) MCP structuredContent: { content: [ { text: "<json or text>" }, structuredContent : {} ] }
            if (je.TryGetProperty("structuredContent", out var structuredContentProp))
            {
                return structuredContentProp.Deserialize<T>(JsonOptions.Default);
            }


            // 2) MCP content-format: { content: [ { text: "<json or text>" } ] }
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

        // 3) Fallback: serialize + deserialize (sjelden nødvendig)
        return JsonSerializer.Deserialize<T>(
            JsonSerializer.Serialize(Result, JsonOptions.Default),
            JsonOptions.Default);
    }

    /// <summary>
    /// Attempts to retrieve the parameters as an object of the specified type.
    /// </summary>
    /// <remarks>If the parameters are stored as a JSON element, they are deserialized to the specified type.
    /// If the parameters are not directly assignable to the requested type, a serialization and deserialization
    /// fallback is used. This method may return <see langword="null"/> for reference types or the default value for
    /// value types if the parameters cannot be converted.</remarks>
    /// <typeparam name="T">The type to which the parameters should be deserialized.</typeparam>
    /// <returns>An instance of type <typeparamref name="T"/> representing the parameters if conversion is successful; otherwise,
    /// <see langword="null"/> or the default value for the type.</returns>
    public T? GetParams<T>()
    {
        if (Params is null) return default;
        if (Params is T typed) return typed;
        if (Params is JsonElement je) return je.Deserialize<T>(JsonOptions.Default);
        // fallback: serialize + deserialize (sjelden nødvendig)
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(Params, JsonOptions.Default), JsonOptions.Default);
    }

    /// <summary>
    /// Attempts to parse a JSON-RPC 2.0 message from the specified <see cref="JsonElement"/>.
    /// </summary>
    /// <remarks>The method validates the structure of the JSON-RPC message according to the JSON-RPC 2.0
    /// specification. It distinguishes between requests and responses based on the presence of the "method" property
    /// and ensures that required properties are present and mutually exclusive as appropriate.</remarks>
    /// <param name="element">The <see cref="JsonElement"/> representing the JSON-RPC message to parse.</param>
    /// <param name="message">When this method returns, contains the parsed <see cref="JsonRpcMessage"/> if the operation succeeds; otherwise,
    /// <see langword="null"/>. This parameter is passed uninitialized.</param>
    /// <returns><see langword="true"/> if the JSON element represents a valid JSON-RPC 2.0 message and was successfully parsed;
    /// otherwise, <see langword="false"/>.</returns>
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

/// <summary>
/// Represents a structured error object as defined by the JSON-RPC protocol.
/// </summary>
/// <remarks>This record corresponds to the 'error' object in a JSON-RPC response, containing the required 'code'
/// and 'message' fields, and an optional 'data' field for extended error details. It can be serialized and deserialized
/// for use in JSON-RPC communication.</remarks>
/// <param name="Code">The error code indicating the type of error that occurred. Standard JSON-RPC error codes are negative integers;
/// custom codes may be used for application-specific errors.</param>
/// <param name="Message">A short description of the error. This message is intended to provide a human-readable explanation of the error
/// condition.</param>
/// <param name="Data">Optional additional information about the error. This value may be null if no extra data is provided.</param>
public sealed record JsonRpcError(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] object? Data)
{
    /// <summary>
    /// Converts the current object to a <see cref="JsonElement"/> representation using the default JSON serialization
    /// options.
    /// </summary>
    /// <remarks>The returned <see cref="JsonElement"/> is a deep clone and is independent of the original
    /// object. Changes to the object after calling this method are not reflected in the returned <see
    /// cref="JsonElement"/>.</remarks>
    /// <returns>A <see cref="JsonElement"/> that represents the serialized form of the current object.</returns>
    public JsonElement ToJsonElement() =>
        JsonDocument.Parse(JsonSerializer.Serialize(this, JsonOptions.Default)).RootElement.Clone();

    /// <summary>
    /// Attempts to parse a <see cref="JsonRpcError"/> object from the specified <see cref="JsonElement"/>.
    /// </summary>
    /// <remarks>The JSON element must include both the "code" (integer) and "message" (string) properties to
    /// be considered a valid error object. The optional "data" property, if present, is deserialized as an object and
    /// included in the resulting <see cref="JsonRpcError"/>.</remarks>
    /// <param name="element">The JSON element to parse. Must contain the required properties "code" and "message".</param>
    /// <param name="message">When this method returns, contains the parsed <see cref="JsonRpcError"/> if parsing succeeded; otherwise, <see
    /// langword="null"/>.</param>
    /// <returns><see langword="true"/> if the JSON element contains a valid error object and parsing succeeded; otherwise, <see
    /// langword="false"/>.</returns>
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
