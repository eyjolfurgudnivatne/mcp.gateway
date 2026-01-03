#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Text.Json;

public readonly struct TypedJsonRpc<T>(JsonRpcMessage inner)
{
    public object? Id => inner.Id;
    public string? IdAsString => inner.IdAsString;
    public string? Method => inner.Method;
    public JsonRpcMessage Inner => inner;

    public T? GetParams() => inner.GetParams<T>();

    /// <summary>
    /// Creates a successful response with the given result.
    /// The result will be serialized into structuredContent.
    /// </summary>
    public static TypedJsonRpc<T> Success(object? id, T result)
    {
        // Serialize result to string for text content (backward compatibility)
        var json = JsonSerializer.Serialize(result, JsonOptions.Default);
        
        var message = ToolResponse.SuccessWithStructured(
            id, 
            textContent: json, 
            structuredContent: result!);

        return new TypedJsonRpc<T>(message);
    }

    /// <summary>
    /// Creates an error response.
    /// </summary>
    public static TypedJsonRpc<T> Error(object? id, int code, string message, object? data = null)
    {
        var msg = ToolResponse.Error(id, code, message, data);
        return new TypedJsonRpc<T>(msg);
    }

    /// <summary>
    /// Creates an error response.
    /// </summary>
    public static TypedJsonRpc<T> Error(object? Id, JsonRpcError Error) =>
        new(JsonRpcMessage.CreateError(
            Id: Id,
            Error: Error));
}
