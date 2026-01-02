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
    [McpPrompt(Description = "Mock prompt 21")] public JsonRpcMessage Prompt21(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_21", 21);
    [McpPrompt(Description = "Mock prompt 22")] public JsonRpcMessage Prompt22(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_22", 22);
    [McpPrompt(Description = "Mock prompt 23")] public JsonRpcMessage Prompt23(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_23", 23);
    [McpPrompt(Description = "Mock prompt 24")] public JsonRpcMessage Prompt24(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_24", 24);
    [McpPrompt(Description = "Mock prompt 25")] public JsonRpcMessage Prompt25(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_25", 25);
    [McpPrompt(Description = "Mock prompt 26")] public JsonRpcMessage Prompt26(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_26", 26);
    [McpPrompt(Description = "Mock prompt 27")] public JsonRpcMessage Prompt27(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_27", 27);
    [McpPrompt(Description = "Mock prompt 28")] public JsonRpcMessage Prompt28(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_28", 28);
    [McpPrompt(Description = "Mock prompt 29")] public JsonRpcMessage Prompt29(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_29", 29);
    [McpPrompt(Description = "Mock prompt 30")] public JsonRpcMessage Prompt30(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_30", 30);
    [McpPrompt(Description = "Mock prompt 31")] public JsonRpcMessage Prompt31(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_31", 31);
    [McpPrompt(Description = "Mock prompt 32")] public JsonRpcMessage Prompt32(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_32", 32);
    [McpPrompt(Description = "Mock prompt 33")] public JsonRpcMessage Prompt33(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_33", 33);
    [McpPrompt(Description = "Mock prompt 34")] public JsonRpcMessage Prompt34(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_34", 34);
    [McpPrompt(Description = "Mock prompt 35")] public JsonRpcMessage Prompt35(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_35", 35);
    [McpPrompt(Description = "Mock prompt 36")] public JsonRpcMessage Prompt36(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_36", 36);
    [McpPrompt(Description = "Mock prompt 37")] public JsonRpcMessage Prompt37(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_37", 37);
    [McpPrompt(Description = "Mock prompt 38")] public JsonRpcMessage Prompt38(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_38", 38);
    [McpPrompt(Description = "Mock prompt 39")] public JsonRpcMessage Prompt39(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_39", 39);
    [McpPrompt(Description = "Mock prompt 40")] public JsonRpcMessage Prompt40(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_40", 40);
    [McpPrompt(Description = "Mock prompt 41")] public JsonRpcMessage Prompt41(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_41", 41);
    [McpPrompt(Description = "Mock prompt 42")] public JsonRpcMessage Prompt42(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_42", 42);
    [McpPrompt(Description = "Mock prompt 43")] public JsonRpcMessage Prompt43(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_43", 43);
    [McpPrompt(Description = "Mock prompt 44")] public JsonRpcMessage Prompt44(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_44", 44);
    [McpPrompt(Description = "Mock prompt 45")] public JsonRpcMessage Prompt45(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_45", 45);
    [McpPrompt(Description = "Mock prompt 46")] public JsonRpcMessage Prompt46(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_46", 46);
    [McpPrompt(Description = "Mock prompt 47")] public JsonRpcMessage Prompt47(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_47", 47);
    [McpPrompt(Description = "Mock prompt 48")] public JsonRpcMessage Prompt48(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_48", 48);
    [McpPrompt(Description = "Mock prompt 49")] public JsonRpcMessage Prompt49(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_49", 49);
    [McpPrompt(Description = "Mock prompt 50")] public JsonRpcMessage Prompt50(JsonRpcMessage r) => CreateMockPrompt(r, "mock_prompt_50", 50);

    private static JsonRpcMessage CreateMockPrompt(JsonRpcMessage request, string name, int index)
    {
        return ToolResponse.Success(request.Id, new PromptResponse
        {
            Description = $"Mock prompt {index}",
            Messages= [
                new(PromptRole.System, new TextContent { Text = "You are a helpful assistant." }),
                new(PromptRole.User, new TextContent { Text = $"This is mock prompt number {index}." })
            ]
        });
    }
}
