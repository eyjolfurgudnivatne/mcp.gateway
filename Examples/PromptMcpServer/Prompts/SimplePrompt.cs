namespace PromptMcpServer.Prompts;

using Mcp.Gateway.Tools;
using System.ComponentModel;
using System.Text.Json.Serialization;

public class SimplePrompt
{
    public record SantaReportPromptRequest(
        [property: JsonPropertyName("name")]
        [property: DisplayName("Child's Name")]
        [property: Description("Name of the child")] string Name,

        [property: JsonPropertyName("behavior")]
        [property: DisplayName("Child's Behavior")]
        [property: Description("Behavior of the child (e.g., Good, Naughty)")] BehaviorEnum Behavior);


    [McpPrompt(Description = "Report to Santa Claus")]
    public JsonRpcMessage SantaReportPrompt(TypedJsonRpc<SantaReportPromptRequest> request)
    {
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'name' and 'behavior' are required and must be strings.");

        return ToolResponse.Success(
            request.Id,
            new PromptResponse
            {
                Description = "A prompt that reports to Santa Claus",
                Messages = [
                    new(
                        PromptRole.System,
                        new TextContent {
                            Text = "You are a very helpful assistant for Santa Claus."
                        }),
                    new (
                        PromptRole.User,
                        new TextContent {
                            Text = $"Send a letter to Santa Claus and tell him that {args.Name} has behaved {args.Behavior}."
                        })
                ]
            }
        );
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
