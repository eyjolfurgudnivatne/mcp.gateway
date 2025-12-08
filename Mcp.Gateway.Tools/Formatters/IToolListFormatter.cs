namespace Mcp.Gateway.Tools.Formatters;

/// <summary>
/// Interface for formatting tool lists in different AI platform formats.
/// Implementations convert MCP tool definitions to platform-specific formats.
/// </summary>
public interface IToolListFormatter
{
    /// <summary>
    /// Format identifier (e.g., "mcp", "ollama", "microsoft-ai").
    /// Used to route tools/list/{format} requests.
    /// </summary>
    string FormatName { get; }
    
    /// <summary>
    /// Converts MCP tool definitions to the target platform format.
    /// </summary>
    /// <param name="tools">MCP tool definitions from ToolService</param>
    /// <returns>Formatted tool list ready for the target platform</returns>
    object FormatToolList(IEnumerable<ToolService.ToolDefinition> tools);
}
