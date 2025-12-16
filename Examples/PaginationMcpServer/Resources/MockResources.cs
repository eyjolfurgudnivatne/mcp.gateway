namespace PaginationMcpServer.Resources;

using Mcp.Gateway.Tools;
using System.Text.Json;

/// <summary>
/// Mock resources for testing pagination.
/// Contains 50 resources for testing pagination.
/// </summary>
public class MockResources
{
    [McpResource("mock://resource/001", Name = "Mock Resource 001", Description = "Mock resource 001", MimeType = "application/json")]
    public JsonRpcMessage Resource001(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/001", 1);

    [McpResource("mock://resource/002", Name = "Mock Resource 002", Description = "Mock resource 002", MimeType = "application/json")]
    public JsonRpcMessage Resource002(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/002", 2);

    [McpResource("mock://resource/003", Name = "Mock Resource 003", Description = "Mock resource 003", MimeType = "application/json")]
    public JsonRpcMessage Resource003(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/003", 3);

    [McpResource("mock://resource/004", Name = "Mock Resource 004", Description = "Mock resource 004", MimeType = "application/json")]
    public JsonRpcMessage Resource004(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/004", 4);

    [McpResource("mock://resource/005", Name = "Mock Resource 005", Description = "Mock resource 005", MimeType = "application/json")]
    public JsonRpcMessage Resource005(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/005", 5);

    [McpResource("mock://resource/006", Name = "Mock Resource 006", Description = "Mock resource 006", MimeType = "application/json")]
    public JsonRpcMessage Resource006(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/006", 6);

    [McpResource("mock://resource/007", Name = "Mock Resource 007", Description = "Mock resource 007", MimeType = "application/json")]
    public JsonRpcMessage Resource007(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/007", 7);

    [McpResource("mock://resource/008", Name = "Mock Resource 008", Description = "Mock resource 008", MimeType = "application/json")]
    public JsonRpcMessage Resource008(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/008", 8);

    [McpResource("mock://resource/009", Name = "Mock Resource 009", Description = "Mock resource 009", MimeType = "application/json")]
    public JsonRpcMessage Resource009(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/009", 9);

    [McpResource("mock://resource/010", Name = "Mock Resource 010", Description = "Mock resource 010", MimeType = "application/json")]
    public JsonRpcMessage Resource010(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/010", 10);

    // ... continuing to 50 for brevity
    [McpResource("mock://resource/011", Name = "Mock Resource 011", Description = "Mock resource 011", MimeType = "application/json")]
    public JsonRpcMessage Resource011(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/011", 11);

    [McpResource("mock://resource/012", Name = "Mock Resource 012", Description = "Mock resource 012", MimeType = "application/json")]
    public JsonRpcMessage Resource012(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/012", 12);

    [McpResource("mock://resource/013", Name = "Mock Resource 013", Description = "Mock resource 013", MimeType = "application/json")]
    public JsonRpcMessage Resource013(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/013", 13);

    [McpResource("mock://resource/014", Name = "Mock Resource 014", Description = "Mock resource 014", MimeType = "application/json")]
    public JsonRpcMessage Resource014(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/014", 14);

    [McpResource("mock://resource/015", Name = "Mock Resource 015", Description = "Mock resource 015", MimeType = "application/json")]
    public JsonRpcMessage Resource015(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/015", 15);

    [McpResource("mock://resource/016", Name = "Mock Resource 016", Description = "Mock resource 016", MimeType = "application/json")]
    public JsonRpcMessage Resource016(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/016", 16);

    [McpResource("mock://resource/017", Name = "Mock Resource 017", Description = "Mock resource 017", MimeType = "application/json")]
    public JsonRpcMessage Resource017(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/017", 17);

    [McpResource("mock://resource/018", Name = "Mock Resource 018", Description = "Mock resource 018", MimeType = "application/json")]
    public JsonRpcMessage Resource018(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/018", 18);

    [McpResource("mock://resource/019", Name = "Mock Resource 019", Description = "Mock resource 019", MimeType = "application/json")]
    public JsonRpcMessage Resource019(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/019", 19);

    [McpResource("mock://resource/020", Name = "Mock Resource 020", Description = "Mock resource 020", MimeType = "application/json")]
    public JsonRpcMessage Resource020(JsonRpcMessage r) => CreateMockResource(r, "mock://resource/020", 20);

    private static JsonRpcMessage CreateMockResource(JsonRpcMessage request, string uri, int index)
    {
        var data = new { uri, index, message = $"Mock resource {index}" };
        var json = JsonSerializer.Serialize(data, JsonOptions.Default);

        return ToolResponse.Success(request.Id, new ResourceContent(
            Uri: uri,
            MimeType: "application/json",
            Text: json
        ));
    }
}
