namespace CalculatorMcpServer.Models;

using System.ComponentModel;
using System.Text.Json.Serialization;

public sealed record AddNumbersRequestTyped(
    [property: JsonPropertyName("number1")]
    [property: Description("First number to add")] double Number1,
    [property: JsonPropertyName("number2")]
    [property: Description("Second number to add")] double Number2);

public sealed record AddNumbersRequest(
    [property: JsonPropertyName("number1")] double Number1,
    [property: JsonPropertyName("number2")] double Number2);

public sealed record AddNumbersResponse(
    [property: JsonPropertyName("result")] double Result);

public sealed record MultiplyRequest(
    [property: JsonPropertyName("number1")] double Number1,
    [property: JsonPropertyName("number2")] double Number2);

public sealed record MultiplyResponse(
    [property: JsonPropertyName("result")] double Result);