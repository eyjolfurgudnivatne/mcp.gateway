namespace ClientTestMcpServer.Models;

using System.Text.Json.Serialization;

public sealed record DynamicResourceResponse(
    [property: JsonPropertyName("message")] string Message);