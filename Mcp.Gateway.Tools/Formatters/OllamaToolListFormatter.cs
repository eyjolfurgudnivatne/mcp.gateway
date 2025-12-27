namespace Mcp.Gateway.Tools.Formatters;

using System.Text.Json;

/// <summary>
/// Formats tool lists for OllamaSharp (OpenAI function calling format).
/// Converts MCP tool definitions to Ollama's function calling structure.
/// </summary>
/// <remarks>
/// Ollama format structure:
/// {
///   "type": "function",
///   "function": {
///     "name": "tool_name",
///     "description": "tool description",
///     "parameters": {
///       "type": "object",
///       "properties": { ... },
///       "required": [ ... ]
///     }
///   }
/// }
/// </remarks>
public class OllamaToolListFormatter : IToolListFormatter
{
    public string FormatName => "ollama";
    
    public object FormatToolList(IEnumerable<ToolService.FunctionDefinition> tools)
    {
        var formattedTools = tools.Select(t =>
        {
            var schema = JsonSerializer.Deserialize<JsonElement>(t.InputSchema!);
            
            // Extract properties and required fields from MCP InputSchema
            var properties = schema.TryGetProperty("properties", out var props)
                ? props
                : JsonDocument.Parse("{}").RootElement;
            
            var required = schema.TryGetProperty("required", out var req)
                ? req.EnumerateArray().Select(x => x.GetString()).ToArray()
                : [];
            
            var typeValue = schema.TryGetProperty("type", out var typeElement)
                ? typeElement.GetString()
                : "object";
            
            return new
            {
                type = "function",
                function = new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = new
                    {
                        type = typeValue,
                        properties,
                        required
                    }
                }
            };
        }).ToList();
        
        return new { tools = formattedTools };
    }
}
