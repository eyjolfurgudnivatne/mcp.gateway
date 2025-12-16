namespace PromptMcpServer.Prompts;

using Mcp.Gateway.Tools;
using System.ComponentModel;
using System.Text.Json.Serialization;

public class SimplePrompt
{

    [McpPrompt(Description = "Report to Santa Claus")]
    public JsonRpcMessage SantaReportPrompt(JsonRpcMessage request)
    {
        return ToolResponse.Success(
            request.Id,
            new PromptResponse(
                Name: "santa_report_prompt",
                Description: "A prompt that reports to Santa Claus",
                Messages: [
                    new(
                        PromptRole.System,
                        "You are a very helpful assistant for Santa Claus."),
                    new (
                        PromptRole.User,
                        "Send a letter to Santa Claus and tell him that {{name}} has been {{behavior}}.")
                ],
                Arguments: new {
                    name = new {
                        type = "string",
                        description = "Name of the child"
                    },
                    behavior = new {
                        type = "string",
                        description = "Behavior of the child (e.g., Good, Naughty)",
                        @enum = new[] { "Good", "Naughty" }
                    }
                }
            ));
    }

    public enum BehaviorEnum
    {
        Good,
        Naughty
    }

    public record LetterToSantaRequest(
        [property: JsonPropertyName("name")]
        [property: Description("Name of the child")] string Name,
        [property: JsonPropertyName("behavior")]
        [property: Description("Behavior of the child (e.g., Good, Naughty)")] BehaviorEnum Behavior,
        [property: JsonPropertyName("santaEmailAddress")]
        [property: Description("Email address to Santa Claus")] string? SantaEmailAddress);

    public record LetterToSantaResponse(
        [property: JsonPropertyName("sent")] bool Sent);

    // tool to demonstrate both prompt and tool in same class (and test that they don't conflict)
    [McpTool(Description = "Send letter to Santa Claus")]
    public JsonRpcMessage LetterToSanta(TypedJsonRpc<LetterToSantaRequest> request)
    {
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'name' and 'behavior' are required and must be strings.");

        return ToolResponse.Success(
            request.Id,
            new LetterToSantaResponse(true));
    }
}
