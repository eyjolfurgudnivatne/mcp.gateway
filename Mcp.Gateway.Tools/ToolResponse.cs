namespace Mcp.Gateway.Tools;

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
}
