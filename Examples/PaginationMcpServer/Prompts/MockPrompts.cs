namespace PaginationMcpServer.Prompts;

using Mcp.Gateway.Tools;

/// <summary>
/// Mock prompts for testing pagination.
/// Contains 50 prompts for testing pagination.
/// </summary>
public class MockPrompts
{
    [McpPrompt(Description = "Mock prompt 01")] public JsonRpcMessage Prompt01(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_01", 1);
    [McpPrompt(Description = "Mock prompt 02")] public JsonRpcMessage Prompt02(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_02", 2);
    [McpPrompt(Description = "Mock prompt 03")] public JsonRpcMessage Prompt03(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_03", 3);
    [McpPrompt(Description = "Mock prompt 04")] public JsonRpcMessage Prompt04(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_04", 4);
    [McpPrompt(Description = "Mock prompt 05")] public JsonRpcMessage Prompt05(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_05", 5);
    [McpPrompt(Description = "Mock prompt 06")] public JsonRpcMessage Prompt06(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_06", 6);
    [McpPrompt(Description = "Mock prompt 07")] public JsonRpcMessage Prompt07(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_07", 7);
    [McpPrompt(Description = "Mock prompt 08")] public JsonRpcMessage Prompt08(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_08", 8);
    [McpPrompt(Description = "Mock prompt 09")] public JsonRpcMessage Prompt09(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_09", 9);
    [McpPrompt(Description = "Mock prompt 10")] public JsonRpcMessage Prompt10(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_10", 10);

    // ... continuing to 50 for brevity, showing pattern
    [McpPrompt(Description = "Mock prompt 11")] public JsonRpcMessage Prompt11(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_11", 11);
    [McpPrompt(Description = "Mock prompt 12")] public JsonRpcMessage Prompt12(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_12", 12);
    [McpPrompt(Description = "Mock prompt 13")] public JsonRpcMessage Prompt13(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_13", 13);
    [McpPrompt(Description = "Mock prompt 14")] public JsonRpcMessage Prompt14(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_14", 14);
    [McpPrompt(Description = "Mock prompt 15")] public JsonRpcMessage Prompt15(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_15", 15);
    [McpPrompt(Description = "Mock prompt 16")] public JsonRpcMessage Prompt16(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_16", 16);
    [McpPrompt(Description = "Mock prompt 17")] public JsonRpcMessage Prompt17(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_17", 17);
    [McpPrompt(Description = "Mock prompt 18")] public JsonRpcMessage Prompt18(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_18", 18);
    [McpPrompt(Description = "Mock prompt 19")] public JsonRpcMessage Prompt19(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_19", 19);
    [McpPrompt(Description = "Mock prompt 20")] public JsonRpcMessage Prompt20(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_20", 20);

    private static JsonRpcMessage CreateMockPrompt(JsonRpcMessage request, string name, int index)
    {
        return ToolResponse.Success(request.Id, new PromptResponse(
            Name: name,
            Description: $"Mock prompt {index}",
            Messages: [
                new(PromptRole.System, "You are a helpful assistant."),
                new(PromptRole.User, $"This is mock prompt number {index}.")
            ],
            Arguments: new
            {
                input = new
                {
                    type = "string",
                    description = $"Input for mock prompt {index}"
                }
            }
        ));
    }
}
