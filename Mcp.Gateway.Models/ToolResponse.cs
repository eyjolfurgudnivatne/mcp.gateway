#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class ToolResponse
{
    public static JsonRpcMessage Success(object? Id, object? Result) =>
        JsonRpcMessage.CreateSuccess(
            Id: Id,
            Result: Result);

    public static JsonRpcMessage Error(object? Id, int Code, string Message, object? Data = null) =>
        JsonRpcMessage.CreateError(
            Id: Id,
            Error: new JsonRpcError(
                Code: Code,
                Message: Message,
                Data: Data));

    public static JsonRpcMessage Error(object? Id, JsonRpcError Error) =>
        JsonRpcMessage.CreateError(
            Id: Id,
            Error: Error);

    /// <summary>
    /// Creates a successful tool response with structured content (MCP 2025-11-25).
    /// </summary>
    /// <param name="Id">Request ID</param>
    /// <param name="textContent">Text content to include in the content array</param>
    /// <param name="structuredContent">Structured content object (will be serialized as JSON)</param>
    /// <returns>JsonRpcMessage with both content array and structuredContent</returns>
    public static JsonRpcMessage SuccessWithStructured(object? Id, string textContent, object structuredContent) =>
        Success(Id, new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = textContent
                }
            },
            structuredContent
        });

    /// <summary>
    /// Creates a successful tool response with structured content and custom content array (MCP 2025-11-25).
    /// </summary>
    /// <param name="Id">Request ID</param>
    /// <param name="content">Custom content array (array of content objects)</param>
    /// <param name="structuredContent">Structured content object (will be serialized as JSON)</param>
    /// <returns>JsonRpcMessage with both content array and structuredContent</returns>
    public static JsonRpcMessage SuccessWithStructured(object? Id, object[] content, object structuredContent) =>
        Success(Id, new
        {
            content,
            structuredContent
        });
}
