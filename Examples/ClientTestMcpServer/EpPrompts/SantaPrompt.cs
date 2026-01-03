namespace ClientTestMcpServer.EpPrompts;

using Mcp.Gateway.Tools;
using System.ComponentModel;
using System.Text.Json.Serialization;

public class SantaPrompt
{
    public record SantaReportPromptRequest(
        [property: JsonPropertyName("name")]
        [property: DisplayName("Child's Name")]
        [property: Description("Name of the child")] string Name,

        [property: JsonPropertyName("behavior")]
        [property: DisplayName("Child's Behavior")]
        [property: Description("Behavior of the child (e.g., Good, Naughty)")] BehaviorEnum Behavior);

    public enum BehaviorEnum
    {
        Good,
        Naughty
    }

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
}
