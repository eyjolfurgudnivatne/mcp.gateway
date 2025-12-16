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
        string? InputSchema = null,  // For tools (JSON Schema object)
        IReadOnlyList<PromptArgument>? Arguments = null,  // For prompts (array of arguments)
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
                IReadOnlyList<PromptArgument>? arguments = null;
                ToolCapabilities capabilities = ToolCapabilities.Standard;

                // Tool
                if (functionDetails.FunctionType == FunctionTypeEnum.Tool)
                {
                    var attr = method.GetCustomAttribute<McpToolAttribute>();
                    description = attr?.Description ?? "No description available";
                    attrInputSchema = attr?.InputSchema;
                    capabilities = attr?.Capabilities ?? ToolCapabilities.Standard;
                    
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
                        Arguments: null,  // Tools don't have arguments array
                        Capabilities: capabilities
                    );
                }

                // Prompt
                if (functionDetails.FunctionType == FunctionTypeEnum.Prompt)
                {
                    var attr = method.GetCustomAttribute<McpPromptAttribute>();
                    description = attr?.Description ?? "No description available";
                    
                    // For prompts, we need to extract Arguments from PromptResponse
                    // We do this by calling the prompt method with a dummy request
                    try
                    {
                        var dummyRequest = JsonRpcMessage.CreateRequest("prompts/get", "dummy", new { name = functionName });
                        var response = (JsonRpcMessage)functionDetails.FunctionDelegate.DynamicInvoke(dummyRequest)!;
                        
                        // Parse PromptResponse from result
                        if (response.Result is not null)
                        {
                            var promptResponse = response.GetResult<PromptResponse>();
                            if (promptResponse?.Arguments is not null)
                            {
                                // Parse Arguments object into PromptArgument array
                                arguments = ParsePromptArguments(promptResponse.Arguments);
                            }
                        }
                    }
                    catch
                    {
                        // If parsing fails, return empty arguments
                        arguments = Array.Empty<PromptArgument>();
                    }
                    
                    return new FunctionDefinition(
                        Name: functionName,
                        Description: description!,
                        InputSchema: null,  // Prompts don't have inputSchema
                        Arguments: arguments ?? Array.Empty<PromptArgument>(),
                        Capabilities: capabilities
                    );
                }

                // Fallback (should never happen)
                return new FunctionDefinition(
                    Name: functionName,
                    Description: "Unknown function type",
                    InputSchema: @"{""type"":""object"",""properties"":{}}",
                    Arguments: null,
                    Capabilities: ToolCapabilities.Standard
                );
            });
    }

    /// <summary>
    /// Parses PromptResponse.Arguments object into PromptArgument array for prompts/list
    /// </summary>
    private static IReadOnlyList<PromptArgument> ParsePromptArguments(object argumentsObj)
    {
        try
        {
            // Arguments is a JSON object like: { "name": { "type": "string", "description": "..." }, ... }
            var json = JsonSerializer.Serialize(argumentsObj, JsonOptions.Default);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var result = new List<PromptArgument>();

            foreach (var property in root.EnumerateObject())
            {
                var argName = property.Name;
                var argValue = property.Value;

                // Extract description and determine if required
                string description = argValue.TryGetProperty("description", out var descProp)
                    ? descProp.GetString() ?? ""
                    : "";

                // For now, assume all arguments in the object are required
                // (we can enhance this later if PromptResponse includes required metadata)
                bool required = true;

                result.Add(new PromptArgument(argName, description, required));
            }

            return result;
        }
        catch
        {
            return Array.Empty<PromptArgument>();
        }
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
