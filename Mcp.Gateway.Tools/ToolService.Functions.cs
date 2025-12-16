namespace Mcp.Gateway.Tools;

using Mcp.Gateway.Tools.Schema;
using System.Reflection;
using System.Text.Json;

/// <summary>
/// ToolService partial class - Function Definitions (Tools & Prompts) (v1.5.0)
/// </summary>
public partial class ToolService
{
    /// <summary>
    /// Function definition for MCP protocol (used by tools/list and prompts/list)
    /// </summary>
    public record FunctionDefinition(
        string Name, 
        string Description, 
        string InputSchema,
        ToolCapabilities Capabilities = ToolCapabilities.Standard);

    /// <summary>
    /// Returns all registered functions with their metadata for MCP tools/list or prompts/list
    /// </summary>
    public IEnumerable<FunctionDefinition> GetAllFunctionDefinitions(FunctionTypeEnum functionType)
    {
        EnsureFunctionsScanned();
        
        return ConfiguredFunctions
            .Where(x => x.Value.FunctionType == functionType)
            .Select(kvp =>
            {
                var functionName = kvp.Key;
                var functionDetails = kvp.Value;
                
                // Get attribute from delegate method
                var method = functionDetails.FunctionDelegate.Method;
                string? description = null;
                string? attrInputSchema = null;
                ToolCapabilities capabilities = ToolCapabilities.Standard;

                // Tool
                if (functionDetails.FunctionType == FunctionTypeEnum.Tool)
                {
                    var attr = method.GetCustomAttribute<McpToolAttribute>();
                    description = attr?.Description ?? "No description available";
                    attrInputSchema = attr?.InputSchema;
                    capabilities = attr?.Capabilities ?? ToolCapabilities.Standard;                
                }

                // Prompt
                if (functionDetails.FunctionType == FunctionTypeEnum.Prompt)
                {
                    var attr = method.GetCustomAttribute<McpPromptAttribute>();
                    description = attr?.Description ?? "No description available";
                    attrInputSchema = attr?.InputSchema;
                }

                // Generate or use provided input schema
                string? inputSchema = null;
                if (string.IsNullOrWhiteSpace(attrInputSchema))
                {
                    // If TypedJsonRpc<T> and no InputSchema → try schema generator
                    var generated = ToolSchemaGenerator.TryGenerateForTool(method, functionDetails);
                    inputSchema = generated ?? @"{""type"":""object"",""properties"":{}}";
                }
                else
                {
                    inputSchema = attrInputSchema;
                }

                // Validate InputSchema at runtime
                ValidateInputSchema(functionName, inputSchema);
                
                return new FunctionDefinition(
                    Name: functionName,
                    Description: description!,
                    InputSchema: inputSchema,
                    Capabilities: capabilities
                );
            });
    }

    /// <summary>
    /// Returns functions filtered by transport capabilities.
    /// This ensures clients only see functions they can actually use.
    /// </summary>
    /// <param name="functionType">Tool or Prompt</param>
    /// <param name="transport">Transport type: "stdio", "http", "sse", or "ws"</param>
    /// <returns>List of functions compatible with the specified transport</returns>
    public IEnumerable<FunctionDefinition> GetFunctionsForTransport(FunctionTypeEnum functionType, string transport)
    {
        var allFunctions = GetAllFunctionDefinitions(functionType);

        // Determine allowed capabilities based on transport
        var allowedCapabilities = transport switch
        {
            "stdio" => ToolCapabilities.Standard,
            "http" => ToolCapabilities.Standard,
            "sse" => ToolCapabilities.Standard | ToolCapabilities.TextStreaming,
            "ws" => ToolCapabilities.Standard | ToolCapabilities.TextStreaming | ToolCapabilities.BinaryStreaming,
            _ => ToolCapabilities.Standard
        };
        
        // Filter functions based on capabilities
        return allFunctions.Where(func =>
        {
            // If function is Standard (default), it works on all transports
            if (func.Capabilities == ToolCapabilities.Standard)
                return true;
            
            // Check if function's capabilities are supported by this transport
            return (func.Capabilities & allowedCapabilities) != 0;
        });
    }

    /// <summary>
    /// Validates an InputSchema for common errors.
    /// </summary>
    private static void ValidateInputSchema(string functionName, string inputSchema)
    {
        if (string.IsNullOrEmpty(inputSchema))
            return;

        try
        {
            using var doc = JsonDocument.Parse(inputSchema);
            var root = doc.RootElement;

            // Check if it's a valid JSON object
            if (root.ValueKind != JsonValueKind.Object)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"WARNING: Function '{functionName}' has invalid InputSchema - root must be an object");
            }

            // Check if 'type' is 'object'
            if (root.TryGetProperty("type", out var typeElement))
            {
                var typeValue = typeElement.GetString();
                if (typeValue != "object")
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"WARNING: Function '{functionName}' InputSchema type is '{typeValue}', expected 'object'");
                }
            }

            // CRITICAL: Check if 'properties' is an object (not an array!)
            if (root.TryGetProperty("properties", out var propertiesElement))
            {
                if (propertiesElement.ValueKind == JsonValueKind.Array)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"ERROR: Function '{functionName}' has INVALID InputSchema - 'properties' must be an object {{}}, not an array []!");
                    System.Diagnostics.Debug.WriteLine(
                        $"  This will cause LLM clients to fail when parsing the schema.");
                    System.Diagnostics.Debug.WriteLine(
                        $"  Fix: Change \"properties\":[...] to \"properties\":{{...}}");
                }
                else if (propertiesElement.ValueKind != JsonValueKind.Object)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"WARNING: Function '{functionName}' InputSchema 'properties' should be an object");
                }
            }
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"ERROR: Function '{functionName}' has malformed InputSchema JSON: {ex.Message}");
        }
    }
}
