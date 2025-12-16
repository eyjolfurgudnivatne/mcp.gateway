namespace CalculatorMcpServer.Tools;

using CalculatorMcpServer.Models;
using Mcp.Gateway.Tools;

public class CalculatorTools
{
    [McpTool("add_numbers",
        Title = "Add Numbers",
        Description = "Adds two numbers and return result. Example: 5 + 3 = 8",
        Icon = "https://example.com/icons/calculator.png")]  // NEW: Icon for testing (v1.6.5)
    public JsonRpcMessage AddNumbersTool(TypedJsonRpc<AddNumbersRequest> request)
    {
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");

        return ToolResponse.Success(
            request.Id,
            new AddNumbersResponse(args.Number1 + args.Number2));
    }

    [McpTool("multiply_numbers",
        Title = "Multiply",
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
