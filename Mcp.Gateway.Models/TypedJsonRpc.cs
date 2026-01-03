#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Text.Json;

/// <summary>
/// Represents a strongly-typed wrapper around a JSON-RPC message, providing type-safe access to parameters or results.
/// </summary>
/// <typeparam name="T">The type of the parameters or result payload.</typeparam>
/// <param name="inner">The underlying JSON-RPC message.</param>
public readonly struct TypedJsonRpc<T>(JsonRpcMessage inner)
{
    /// <summary>
    /// The identifier for correlating requests and responses. Can be a string, number, or null. Must be present for
    /// responses; null for notifications.
    /// </summary>
    public object? Id => inner.Id;
    /// <summary>
    /// Helper to get ID as string for logging/comparison.
    /// Gets the identifier as a string representation, or null if the identifier is not set.
    /// </summary>
    public string? IdAsString => inner.IdAsString;
    /// <summary>
    /// The name of the method to be invoked. Required for requests and notifications; otherwise, null for responses.
    /// </summary>
    public string? Method => inner.Method;

    /// <summary>
    /// Gets the underlying raw JSON-RPC message.
    /// </summary>
    public JsonRpcMessage Inner => inner;

    /// <summary>
    /// Attempts to retrieve the parameters as an object of the specified type.
    /// </summary>
    /// <remarks>If the parameters are stored as a JSON element, they are deserialized to the specified type.
    /// If the parameters are not directly assignable to the requested type, a serialization and deserialization
    /// fallback is used. This method may return <see langword="null"/> for reference types or the default value for
    /// value types if the parameters cannot be converted.</remarks>
    /// <returns>An instance of type <typeparamref name="T"/> representing the parameters if conversion is successful; otherwise,
    /// <see langword="null"/> or the default value for the type.</returns>
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
