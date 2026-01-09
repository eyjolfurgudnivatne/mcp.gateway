namespace ClientTestMcpServer.EpTools;

using ClientTestMcpServer.Models;
using Mcp.Gateway.Tools;

public class DynamicResourcesTools
{
    [McpTool("add-test-resource",
        Title = "Add Resource",
        Description = "Add one dynamic test resource.")]
    public TypedJsonRpc<DynamicResourceResponse> AddResource(JsonRpcMessage request, ToolService toolService)
    {
        toolService.RegisterResource(
            "dynamic://test/resource1",
            async (JsonRpcMessage req) =>
            {
                var content = new ResourceContent(
                    Uri: "system://status",
                    MimeType: "application/json",
                    Text: "This is dynamic resource data."
                );

                return ToolResponse.Success(req.Id, content);
            },
            name: "Test Resource 1",
            description: "A dynamically added test resource.",
            mimeType: "application/json");

        return TypedJsonRpc<DynamicResourceResponse>.Success(
            request.Id,
            new DynamicResourceResponse("All resources added."));
    }

    [McpTool("remove-test-resource",
        Title = "Remove Resource",
        Description = "Remove one dynamic test resource.")]
    public TypedJsonRpc<DynamicResourceResponse> RemoveResource(JsonRpcMessage request, ToolService toolService)
    {
        toolService.UnregisterResource("dynamic://test/resource1");

        return TypedJsonRpc<DynamicResourceResponse>.Success(
            request.Id,
            new DynamicResourceResponse("Resource removed."));
    }
}
