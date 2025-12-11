namespace CalculatorMcpServer.Tools;

using CalculatorMcpServer.Models;
using Mcp.Gateway.Tools;

public class CalculatorTools
{
    [McpTool("add_numbers",
        Title = "Add Numbers",
        Description = "Adds two numbers and return result. Example: 5 + 3 = 8",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""number1"":{""type"":""number"",""description"":""First number to add""},
                ""number2"":{""type"":""number"",""description"":""Second number to add""}
            },
            ""required"":[""number1"",""number2""]
        }")]

    public async Task<JsonRpcMessage> AddNumbersTool(JsonRpcMessage request)
    {
        var args = request.GetParams<AddNumbersRequest>()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");

        return ToolResponse.Success(
            request.Id,
            new AddNumbersResponse(args.Number1 + args.Number2));
    }

    [McpTool("multiply_numbers",
        Title = "Multiply",
        Description = "Multiplies two numbers and return result. Example: 5 * 3 = 15",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""number1"":{""type"":""number""},
                ""number2"":{""type"":""number""}
            },
            ""required"":[""number1"",""number2""]
        }")]
    public async Task<JsonRpcMessage> MultiplyTool(JsonRpcMessage request)
    {
        var args = request.GetParams<MultiplyRequest>()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");

        return ToolResponse.Success(
            request.Id,
            new MultiplyResponse(args.Number1 * args.Number2));
    }
}
