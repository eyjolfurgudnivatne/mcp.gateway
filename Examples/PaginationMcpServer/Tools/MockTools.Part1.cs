namespace PaginationMcpServer.Tools;

using Mcp.Gateway.Tools;

/// <summary>
/// Mock tools for testing pagination.
/// Contains 120 tools for testing pagination with different page sizes.
/// </summary>
public partial class MockTools
{
    // Tools 001-040
    [McpTool("mock_tool_001", Description = "Mock tool 001")] public JsonRpcMessage Tool001(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 1 });
    [McpTool("mock_tool_002", Description = "Mock tool 002")] public JsonRpcMessage Tool002(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 2 });
    [McpTool("mock_tool_003", Description = "Mock tool 003")] public JsonRpcMessage Tool003(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 3 });
    [McpTool("mock_tool_004", Description = "Mock tool 004")] public JsonRpcMessage Tool004(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 4 });
    [McpTool("mock_tool_005", Description = "Mock tool 005")] public JsonRpcMessage Tool005(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 5 });
    [McpTool("mock_tool_006", Description = "Mock tool 006")] public JsonRpcMessage Tool006(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 6 });
    [McpTool("mock_tool_007", Description = "Mock tool 007")] public JsonRpcMessage Tool007(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 7 });
    [McpTool("mock_tool_008", Description = "Mock tool 008")] public JsonRpcMessage Tool008(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 8 });
    [McpTool("mock_tool_009", Description = "Mock tool 009")] public JsonRpcMessage Tool009(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 9 });
    [McpTool("mock_tool_010", Description = "Mock tool 010")] public JsonRpcMessage Tool010(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 10 });
    
    [McpTool("mock_tool_011", Description = "Mock tool 011")] public JsonRpcMessage Tool011(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 11 });
    [McpTool("mock_tool_012", Description = "Mock tool 012")] public JsonRpcMessage Tool012(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 12 });
    [McpTool("mock_tool_013", Description = "Mock tool 013")] public JsonRpcMessage Tool013(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 13 });
    [McpTool("mock_tool_014", Description = "Mock tool 014")] public JsonRpcMessage Tool014(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 14 });
    [McpTool("mock_tool_015", Description = "Mock tool 015")] public JsonRpcMessage Tool015(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 15 });
    [McpTool("mock_tool_016", Description = "Mock tool 016")] public JsonRpcMessage Tool016(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 16 });
    [McpTool("mock_tool_017", Description = "Mock tool 017")] public JsonRpcMessage Tool017(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 17 });
    [McpTool("mock_tool_018", Description = "Mock tool 018")] public JsonRpcMessage Tool018(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 18 });
    [McpTool("mock_tool_019", Description = "Mock tool 019")] public JsonRpcMessage Tool019(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 19 });
    [McpTool("mock_tool_020", Description = "Mock tool 020")] public JsonRpcMessage Tool020(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 20 });

    [McpTool("mock_tool_021", Description = "Mock tool 021")] public JsonRpcMessage Tool021(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 21 });
    [McpTool("mock_tool_022", Description = "Mock tool 022")] public JsonRpcMessage Tool022(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 22 });
    [McpTool("mock_tool_023", Description = "Mock tool 023")] public JsonRpcMessage Tool023(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 23 });
    [McpTool("mock_tool_024", Description = "Mock tool 024")] public JsonRpcMessage Tool024(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 24 });
    [McpTool("mock_tool_025", Description = "Mock tool 025")] public JsonRpcMessage Tool025(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 25 });
    [McpTool("mock_tool_026", Description = "Mock tool 026")] public JsonRpcMessage Tool026(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 26 });
    [McpTool("mock_tool_027", Description = "Mock tool 027")] public JsonRpcMessage Tool027(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 27 });
    [McpTool("mock_tool_028", Description = "Mock tool 028")] public JsonRpcMessage Tool028(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 28 });
    [McpTool("mock_tool_029", Description = "Mock tool 029")] public JsonRpcMessage Tool029(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 29 });
    [McpTool("mock_tool_030", Description = "Mock tool 030")] public JsonRpcMessage Tool030(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 30 });

    [McpTool("mock_tool_031", Description = "Mock tool 031")] public JsonRpcMessage Tool031(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 31 });
    [McpTool("mock_tool_032", Description = "Mock tool 032")] public JsonRpcMessage Tool032(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 32 });
    [McpTool("mock_tool_033", Description = "Mock tool 033")] public JsonRpcMessage Tool033(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 33 });
    [McpTool("mock_tool_034", Description = "Mock tool 034")] public JsonRpcMessage Tool034(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 34 });
    [McpTool("mock_tool_035", Description = "Mock tool 035")] public JsonRpcMessage Tool035(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 35 });
    [McpTool("mock_tool_036", Description = "Mock tool 036")] public JsonRpcMessage Tool036(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 36 });
    [McpTool("mock_tool_037", Description = "Mock tool 037")] public JsonRpcMessage Tool037(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 37 });
    [McpTool("mock_tool_038", Description = "Mock tool 038")] public JsonRpcMessage Tool038(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 38 });
    [McpTool("mock_tool_039", Description = "Mock tool 039")] public JsonRpcMessage Tool039(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 39 });
    [McpTool("mock_tool_040", Description = "Mock tool 040")] public JsonRpcMessage Tool040(JsonRpcMessage r) => ToolResponse.Success(r.Id, new { index = 40 });
}
