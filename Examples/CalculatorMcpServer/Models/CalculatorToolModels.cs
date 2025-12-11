namespace CalculatorMcpServer.Models;

using System.Text.Json.Serialization;

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