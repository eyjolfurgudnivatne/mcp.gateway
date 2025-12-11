namespace DevTestServer.Tools;

using DevTestServer.MyServices;
using Mcp.Gateway.Tools;
using System.Text.Json;
using System.Text.Json.Serialization;

public class Calculator
{
    public sealed record NumbersRequest(
        [property: JsonPropertyName("number1")] double Number1,
        [property: JsonPropertyName("number2")] double Number2);

    public sealed record NumbersResponse(
        [property: JsonPropertyName("result")] double Result);

    [McpTool("add_numbers",
    Title = "Add Numbers",
    Description = "Adds two numbers and return result. Example: 5 + 3 = 8",
    InputSchema = @"{
            ""type"":""object"",
            ""properties"":
            {
                ""number1"":{""type"":""number"",""description"":""First number to add""},
                ""number2"":{""type"":""number"",""description"":""Second number to add""}
            },
            ""required"":[""number1"",""number2""]
        }")]
    public async Task<JsonRpcMessage> AddNumbersTool(JsonRpcMessage request, CalculatorService calculatorService)
    {
        // Validate params exist and contain required fields
        var paramsElement = request.GetParams();
        
        if (paramsElement.ValueKind == JsonValueKind.Undefined || 
            paramsElement.ValueKind == JsonValueKind.Null)
        {
            throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");
        }

        // Check for required properties
        if (!paramsElement.TryGetProperty("number1", out _) || 
            !paramsElement.TryGetProperty("number2", out _))
        {
            throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");
        }

        // Now safe to deserialize
        var args = request.GetParams<NumbersRequest>()!;
        double result = calculatorService.Add(args.Number1, args.Number2);

        return ToolResponse.Success(
            request.Id,
            new NumbersResponse(result));
    }
}
