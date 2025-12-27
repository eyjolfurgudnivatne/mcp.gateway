namespace ClientTestMcpServer.Tools;

using ClientTestMcpServer.Models;
using Mcp.Gateway.Tools;

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

        // NEW: Return with structured content (v1.6.5)
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
