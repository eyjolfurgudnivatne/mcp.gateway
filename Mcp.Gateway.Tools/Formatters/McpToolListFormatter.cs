namespace Mcp.Gateway.Tools.Formatters;

using System.Text.Json;

/// <summary>
/// Formats tool lists in standard MCP protocol format.
/// This is the default format used by GitHub Copilot, Claude Desktop, and other MCP clients.
/// </summary>
public class McpToolListFormatter : IToolListFormatter
{
    public string FormatName => "mcp";
    
    public object FormatToolList(IEnumerable<ToolService.FunctionDefinition> tools)
    {
        var toolsList = tools.Select(t =>
        {
            object? schema = null;
            try
            {
                schema = JsonSerializer.Deserialize<object>(t.InputSchema!, JsonOptions.Default);
            }
            catch
            {
                // Fallback to empty object schema if deserialization fails
                schema = new { type = "object", properties = new { } };
            }

            return new
            {
                name = t.Name,
                description = t.Description,
                inputSchema = schema
            };
        }).ToList();

        return new { tools = toolsList };
    }
}
