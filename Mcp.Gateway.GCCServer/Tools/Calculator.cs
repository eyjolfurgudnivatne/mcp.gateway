namespace Mcp.Gateway.GCCServer.Tools;

using Mcp.Gateway.Tools;
using System.Text.Json.Serialization;

public class Calculator
{
    public sealed record NumbersRequest(
        [property: JsonPropertyName("number1")] double Number1,
        [property: JsonPropertyName("number2")] double Number2);

    public sealed record NumbersResponse(
        [property: JsonPropertyName("result")] double Result);


    /// <summary>
    /// Processes a JSON-RPC request to add two numbers and returns the result as a JSON-RPC response.
    /// </summary>
    /// <param name="request">The JSON-RPC message containing the parameters 'number1' and 'number2' to be added. Both parameters are required
    /// and must be numbers.</param>
    /// <returns>A JSON-RPC message containing the sum of 'number1' and 'number2' in the response payload.</returns>
    /// <exception cref="ToolInvalidParamsException">Thrown when the request does not include both 'number1' and 'number2' parameters, or if either parameter is not
    /// a valid number.</exception>
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
        var args = request.GetParams<NumbersRequest>()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");

        return ToolResponse.Success(
            request.Id,
            new NumbersResponse(args.Number1 + args.Number2));
    }

    /// <summary>
    /// Processes a JSON-RPC request to multiply two numbers and returns the result in a JSON-RPC response message.
    /// </summary>
    /// <param name="request">The JSON-RPC request message containing the parameters 'number1' and 'number2' to be multiplied. Both parameters
    /// are required and must be numeric values.</param>
    /// <returns>A JSON-RPC response message containing the product of 'number1' and 'number2'.</returns>
    /// <exception cref="ToolInvalidParamsException">Thrown if the request does not include valid 'number1' and 'number2' parameters, or if either parameter is
    /// missing or not a number.</exception>
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
        var args = request.GetParams<NumbersRequest>()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");

        return ToolResponse.Success(
            request.Id,
            new NumbersResponse(args.Number1 * args.Number2));
    }
}
