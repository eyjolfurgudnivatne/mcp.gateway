namespace ClientTestMcpServer.EpTools;

using ClientTestMcpServer.Models;
using Mcp.Gateway.Tools;
using Mcp.Gateway.Tools.Notifications;

public class CalculatorTools
{
    [McpTool("add_numbers",
        Title = "Add Numbers",
        Description = "Adds two numbers and return result. Example: 5 + 3 = 8")]
    public JsonRpcMessage AddNumbersTool(TypedJsonRpc<AddNumbersRequest> request)
    {
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");

        return ToolResponse.Success(
            request.Id,
            new AddNumbersResponse(args.Number1 + args.Number2));
    }

    [McpTool("add_numbers_notification",
    Title = "Add Numbers",
    Description = "Adds two numbers and return result. Example: 5 + 3 = 8")]
    public async Task<JsonRpcMessage> AddNumbersWNotificationTool(
        TypedJsonRpc<AddNumbersRequest> request,
        INotificationSender notificationSender)
    {
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");


        await notificationSender.SendNotificationAsync(
            new NotificationMessage(
                "2.0",
                "notifications/progress",
                new { message = "Starting addition..." }),
            CancellationToken.None);

        await Task.Delay(1000);

        await notificationSender.SendNotificationAsync(
            new NotificationMessage(
                "2.0",
                "notifications/progress",
                new { message = "Halfway done..." }),
            CancellationToken.None);

        await Task.Delay(1000);

        return ToolResponse.Success(
            request.Id,
            new AddNumbersResponse(args.Number1 + args.Number2));
    }

    [McpTool("multiply_numbers",
        Title = "Multiply two numbers",
        Description = "Multiplies two numbers and return result. Example: 5 * 3 = 15")]
    public JsonRpcMessage MultiplyTool(TypedJsonRpc<MultiplyRequest> request)
    {
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");

        return ToolResponse.Success(
            request.Id,
            new MultiplyResponse(args.Number1 * args.Number2));
    }
}
