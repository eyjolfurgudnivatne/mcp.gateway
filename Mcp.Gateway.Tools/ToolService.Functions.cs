namespace Mcp.Gateway.Tools;

using Mcp.Gateway.Tools.Schema;
using System.Reflection;
using System.Text.Json;

/// <summary>
/// ToolService partial class - Function Definitions (Tools and Prompts) (v1.5.0)
/// </summary>
public partial class ToolService
{
    /// <summary>
    /// Function definition for MCP protocol (used by tools/list and prompts/list)
    /// </summary>
    public record FunctionDefinition(
        string Name, 
        string Description, 
        string? InputSchema = null,  // For tools (JSON Schema object)
        IReadOnlyList<PromptArgument>? Arguments = null,  // For prompts (array of arguments)
        ToolCapabilities Capabilities = ToolCapabilities.Standard,
        string? Icon = null,  // MCP 2025-11-25 icon URL
        string? OutputSchema = null);  // NEW: MCP 2025-11-25 output schema (JSON Schema)

    /// <summary>
    /// Returns all registered functions with their metadata for MCP tools/list or prompts/list
    /// Sorted alphabetically by name for consistent ordering (v1.6.0+)
    /// </summary>
    public IEnumerable<FunctionDefinition> GetAllFunctionDefinitions(FunctionTypeEnum functionType)
    {
        EnsureFunctionsScanned();
        
        return ConfiguredFunctions
            .Where(x => x.Value.FunctionType == functionType)
            .OrderBy(x => x.Key) // Sort alphabetically by function name for consistent ordering
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
                    var icon = attr?.Icon;  // NEW: Extract icon (v1.6.5)
                    var outputSchema = attr?.OutputSchema;  // NEW: Extract outputSchema (v1.6.5)
                                        
                    // Generate or use provided input schema
                    string? inputSchema = null;
                    if (string.IsNullOrWhiteSpace(attrInputSchema))
                    {
                        // If TypedJsonRpc<T> and no InputSchema → try schema generator
                        var generated = ToolSchemaGenerator.TryGenerateForTool(method, functionDetails);

                        // https://modelcontextprotocol.io/specification/2025-11-25/server/tools#tool-with-no-parameters:
                        inputSchema = generated ?? @"{""type"":""object"",""additionalProperties"":false}";
                    }
                    else
                    {
                        inputSchema = attrInputSchema;
                    }

                    // Generate output schema if not provided and return type is TypedJsonRpc<T>
                    if (string.IsNullOrWhiteSpace(outputSchema) && functionDetails.FunctionResultType.IsTypedJsonRpcResponse)
                    {
                        // Extract T from TypedJsonRpc<T>
                        var returnType = functionDetails.FunctionResultType.ReturnType;
                        
                        // Handle Task<TypedJsonRpc<T>>
                        if (functionDetails.FunctionResultType.IsGenericTask)
                        {
                            returnType = returnType.GenericTypeArguments[0];
                        }
                        
                        // Now returnType is TypedJsonRpc<T>
                        var tResponse = returnType.GetGenericArguments().FirstOrDefault();
                        
                        if (tResponse != null)
                        {
                            // Use ToolSchemaGenerator to generate schema for TResponse
                            // We can reuse the existing generator logic as it generates schema for a Type
                            outputSchema = ToolSchemaGenerator.GenerateSchemaForTypePublic(tResponse);
                        }
                    }

                    // Validate InputSchema at runtime
                    ValidateInputSchema(functionName, inputSchema);
                    
                    return new FunctionDefinition(
                        Name: functionName,
                        Description: description!,
                        InputSchema: inputSchema,
                        Arguments: null,  // Tools don't have arguments array
                        Capabilities: capabilities,
                        Icon: icon,  // NEW: Include icon (v1.6.5)
                        OutputSchema: outputSchema  // NEW: Include outputSchema (v1.6.5)
                    );
                }

                // Fallback (should never happen)
                return new FunctionDefinition(
                    Name: functionName,
                    Description: "Unknown function type",
                    InputSchema: @"{""type"":""object"",""properties"":{}}",
                    Arguments: null,
                    Capabilities: ToolCapabilities.Standard,
                    Icon: null,  // NEW
                    OutputSchema: null  // NEW
                );
            });
    }

    /// <summary>
    /// Returns functions filtered by transport capabilities.
    /// This ensures clients only see functions they can actually use.
    /// </summary>
    /// <param name="functionType">Tool or Prompt</param>
    /// <param name="transport">Transport type: "stdio", "http", "sse", or "ws"</param>
    /// <param name="cursor">Optional cursor for pagination (v1.6.0+)</param>
    /// <param name="pageSize">Number of items per page (default: 100)</param>
    /// <returns>Paginated list of functions compatible with the specified transport</returns>
    public Pagination.CursorHelper.PaginatedResult<FunctionDefinition> GetFunctionsForTransport(
        FunctionTypeEnum functionType, 
        string transport,
        string? cursor = null,
        int pageSize = Pagination.CursorHelper.DefaultPageSize)
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
        var filteredFunctions = allFunctions.Where(func =>
        {
            // If function is Standard (default), it works on all transports
            if (func.Capabilities == ToolCapabilities.Standard)
                return true;
            
            // Check if function's capabilities are supported by this transport
            return (func.Capabilities & allowedCapabilities) != 0;
        });

        // Apply pagination (v1.6.0+)
        return Pagination.CursorHelper.Paginate(filteredFunctions, cursor, pageSize);
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
