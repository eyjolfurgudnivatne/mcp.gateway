namespace CalculatorMcpServer.Tools;

using CalculatorMcpServer.Models;
using Mcp.Gateway.Tools;

public class CalculatorTools
{
    [McpTool("add_numbers",
        Title = "Add Numbers",
        Description = "Adds two numbers and return result. Example: 5 + 3 = 8",
        Icon = "https://example.com/icons/calculator.png",  // NEW: Icon for testing (v1.6.5)
        OutputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""result"":{""type"":""number"",""description"":""The sum of the two numbers""},
                ""operation"":{""type"":""string"",""description"":""The operation performed""}
            },
            ""required"":[""result""]
        }")]  // NEW: OutputSchema for testing (v1.6.5)
    public JsonRpcMessage AddNumbersTool(TypedJsonRpc<AddNumbersRequest> request)
    {
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");

        // NEW: Return with structured content (v1.6.5)
        return ToolResponse.SuccessWithStructured(
            request.Id,
            textContent: $"Result: {args.Number1 + args.Number2}",
            structuredContent: new
            {
                result = args.Number1 + args.Number2,
                operation = "addition"
            });
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
