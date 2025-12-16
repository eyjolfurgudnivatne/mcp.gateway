namespace Mcp.Gateway.Tools.Formatters;

using System.Text.Json;

/// <summary>
/// Formats tool lists for Microsoft.Extensions.AI.
/// Converts MCP tool definitions to Microsoft.Extensions.AI function format.
/// </summary>
/// <remarks>
/// Microsoft.Extensions.AI format structure:
/// {
///   "name": "tool_name",
///   "description": "tool description",
///   "parameters": {
///     "param_name": {
///       "type": "string",
///       "description": "param description",
///       "required": true
///     }
///   }
/// }
/// 
/// ⚠️ WARNING: Integration with Microsoft.Extensions.AI v10+ (future/anticipated version) and OllamaSharp
/// 
/// While this formatter produces valid Microsoft.Extensions.AI format, there are
/// known compatibility issues when using UseFunctionInvocation() middleware with
/// OllamaSharp 5.x (current as of December 2025; future versions may differ):
/// 
/// - UseFunctionInvocation() may not properly intercept OllamaSharp tool calls
/// - Tool call routing between M.E.AI and OllamaSharp can fail silently
/// - Recommended: Use OllamaToolListFormatter + DirectToolInvoker instead
/// 
/// This formatter is provided for:
/// 1. Anticipated future M.E.AI API stabilization (v10+ is not yet released as of December 2025)
/// 2. Non-Ollama AI providers using Microsoft.Extensions.AI
/// 3. Documentation and reference purposes
/// 
/// For OllamaSharp integration, see OllamaToolListFormatter and DirectToolInvoker.
/// </remarks>
public class MicrosoftAIToolListFormatter : IToolListFormatter
{
    public string FormatName => "microsoft-ai";
    
    public object FormatToolList(IEnumerable<ToolService.FunctionDefinition> tools)
    {
        var formattedTools = tools.Select(t =>
        {
            var schema = JsonSerializer.Deserialize<JsonElement>(t.InputSchema);
            
            // Extract properties from InputSchema
            var properties = schema.TryGetProperty("properties", out var props)
                ? props
                : JsonDocument.Parse("{}").RootElement;
            
            // Extract required fields
            var requiredSet = schema.TryGetProperty("required", out var req)
                ? req.EnumerateArray().Select(x => x.GetString()!).ToHashSet()
                : new HashSet<string>();
            
            // Build parameters dictionary
            var parameters = new Dictionary<string, object>();
            foreach (var prop in properties.EnumerateObject())
            {
                var propName = prop.Name;
                var propValue = prop.Value;
                
                // Extract type and description
                var typeValue = propValue.TryGetProperty("type", out var typeEl)
                    ? typeEl.GetString()
                    : "string";
                
                var description = propValue.TryGetProperty("description", out var descEl)
                    ? descEl.GetString()
                    : null;
                
                parameters[propName] = new
                {
                    type = typeValue,
                    description,
                    required = requiredSet.Contains(propName)
                };
            }
            
            return new
            {
                name = t.Name,
                description = t.Description,
                parameters
            };
        }).ToList();
        
        return new { tools = formattedTools };
    }
}
