namespace Mcp.Gateway.Tools;

using Mcp.Gateway.Tools.Schema;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;
using System.Text.Json;


public class ToolService(IServiceProvider serviceProvider)
{
    private readonly ConcurrentDictionary<string, FunctionDetails> ConfiguredFunctions = new(StringComparer.OrdinalIgnoreCase);
    private volatile bool _functionsScanned = false;
    private readonly object _scanLock = new();

    public enum FunctionTypeEnum
    {
        Tool,
        Prompt
    }

    internal record FunctionDetails(FunctionTypeEnum FunctionType, FunctionDetailArgumentType FunctionArgumentType, FunctionDetailResultType FunctionResultType, Delegate FunctionDelegate);
    internal record FunctionDetailArgumentType(bool IsToolConnector, bool IsStreamMessage, bool IsJsonElementMessage, bool IsTypedJsonRpc, Type ParameterType);
    internal record FunctionDetailResultType(bool IsVoidTask, bool IsGenericTask, bool IsIAsyncEnumerable, bool IsJsonRpcResponse);
    
    internal FunctionDetails GetFunctionDetails(string functionName)
    {
        // Lazy scan on first function lookup
        EnsureFunctionsScanned();

        if (!ConfiguredFunctions.TryGetValue(functionName, out var functionDetails) || functionDetails is null)
            throw new ToolNotFoundException($"Function '{functionName}' is not configured.");
        return functionDetails;
    }
    
    /// <summary>
    /// Ensures functions are scanned exactly once, thread-safely.
    /// </summary>
    private void EnsureFunctionsScanned()
    {
        if (_functionsScanned)
            return;
            
        lock (_scanLock)
        {
            if (_functionsScanned)
                return;
                
            ScanAndRegisterFunctions();
            _functionsScanned = true;
        }
    }

    /// <summary>
    /// Scans all loaded assemblies for methods marked with [McpTool] or [McpPrompt] and registers them.
    /// </summary>
    private void ScanAndRegisterFunctions()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var functionCount = 0;
        
        foreach (var assembly in assemblies)
        {
            // Skip system assemblies
            if (assembly.FullName?.StartsWith("System") == true ||
                assembly.FullName?.StartsWith("Microsoft") == true)
                continue;
            
            // Debug: Log which assemblies we're scanning
            System.Diagnostics.Debug.WriteLine($"Scanning assembly: {assembly.GetName().Name}");
                
            try
            {
                var types = assembly.GetTypes();
                
                foreach (var type in types)
                {
                    var methods = type.GetMethods(System.Reflection.BindingFlags.Public | 
                                                 System.Reflection.BindingFlags.Static | 
                                                 System.Reflection.BindingFlags.Instance);
                    
                    foreach (var method in methods)
                    {
                        FunctionTypeEnum functionType = FunctionTypeEnum.Tool;
                        string? functionName = null;

                        // Check for McpTool attribute
                        if (functionName is null)
                        {
                            var attribute = method.GetCustomAttribute<McpToolAttribute>();
                            if (attribute != null)
                            {
                                functionType = FunctionTypeEnum.Tool;
                                functionName = attribute.Name ?? ToolNameGenerator.ToSnakeCase(method.Name);
                            }
                        }
                        // Check for McpPrompt attribute
                        if (functionName is null)
                        {
                            var attribute = method.GetCustomAttribute<McpPromptAttribute>();
                            if (attribute != null)
                            {
                                functionType = FunctionTypeEnum.Prompt;
                                functionName = attribute.Name ?? ToolNameGenerator.ToSnakeCase(method.Name);
                            }
                        }

                        // Skip methods without custom attribute
                        if (functionName is null)
                            continue;
                        
                        // Auto-generate tool name if not specified
                        //var functionName = attributeName ?? ToolNameGenerator.ToSnakeCase(method.Name);
                        
                        // Validate tool name
                        if (!ToolMethodNameValidator.IsValid(functionName, out var validationError))
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"WARNING: Skipping function '{method.Name}' in {type.FullName} - Invalid function name '{functionName}': {validationError}");
                            continue;
                        }
                        
                        // Debug: Found a tool!
                        System.Diagnostics.Debug.WriteLine(
                            $"Found function: {functionName} (from method: {method.Name}) in {type.FullName}");
                        
                        // Create delegate
                        Delegate functionDelegate;
                        
                        if (method.IsStatic)
                        {
                            functionDelegate = Delegate.CreateDelegate(
                                System.Linq.Expressions.Expression.GetDelegateType(
                                    method.GetParameters().Select(p => p.ParameterType)
                                        .Concat(new[] { method.ReturnType })
                                        .ToArray()),
                                method);
                        }
                        else
                        {
                            var instance = Activator.CreateInstance(type);
                            if (instance is null)
                                continue;
                                
                            functionDelegate = Delegate.CreateDelegate(
                                System.Linq.Expressions.Expression.GetDelegateType(
                                    method.GetParameters().Select(p => p.ParameterType)
                                        .Concat(new[] { method.ReturnType })
                                        .ToArray()),
                                instance,
                                method);
                        }

                        // Register the function with auto-generated or explicit name
                        RegisterFunction(functionName, functionType, functionDelegate);
                        functionCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scanning assembly {assembly.GetName().Name}: {ex.Message}");
                continue;
            }
        }

        System.Diagnostics.Debug.WriteLine($"Function scan complete. Registered {functionCount} functions.");
    }



    /// <summary>
    /// Registers a function with the specified name and delegate.
    /// </summary>
    /// <param name="name">The name of the function to register. Must be a valid function name.</param>
    /// <param name="functionType"></param>
    /// <param name="functionDelegate">The delegate for the function's operations.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="name"/> is not a valid function name.</exception>
    internal void RegisterFunction(string name, FunctionTypeEnum functionType, Delegate functionDelegate)
    {
        if (!ToolMethodNameValidator.IsValid(name, out var error))
            throw new ArgumentException($"Invalid function name '{name}': {error}");

        var method = functionDelegate.Method;
        var returnType = method.ReturnType;
        var parameters = method.GetParameters();

        // --- Sjekk parameter ---
        if (parameters.Length == 0)
            throw new ArgumentException($"Function delegate for '{name}' must accept at least one parameter.");

        var firstParam = parameters[0].ParameterType;

        // --- Sjekk inputtype JsonRpc ---
        bool isJsonElementMessage =
            firstParam == typeof(JsonRpcMessage);

        // --- Sjekk inputtype TypedJsonRpc ---
        bool isTypedJson =
            firstParam.IsGenericType &&
            firstParam.GetGenericTypeDefinition() == typeof(TypedJsonRpc<>);

        // --- Sjekk inputtype stream ---
        bool isInputStreamMessage =
            firstParam  == typeof(StreamMessage);

        bool isInputToolConnector =
            firstParam == typeof(ToolConnector);

        // auda, ingen av delene
        if (!isJsonElementMessage && !isTypedJson && !isInputStreamMessage && !isInputToolConnector)
            throw new ArgumentException(
                $"Function delegate for '{name}' must take JsonRpcMessage, TypedJsonRpc<T>, StreamMessage or ToolConnector as first parameter."
            );

        // prompt, ingen av delene
        if (functionType == FunctionTypeEnum.Prompt && !isJsonElementMessage && !isTypedJson)
            throw new ArgumentException(
                $"Prompt delegate for '{name}' must take JsonRpcMessage or TypedJsonRpc<T> as first parameter."
            );

        // TODO: Erstattes av ToolConnector. dersom det er en stream, må det være minst en parameter til (for WebSocket)
        if (isInputStreamMessage && parameters.Length < 2)
            throw new ArgumentException(
                $"Function delegate for '{name}' must take at least two parameters when using StreamMessage."
            );

        // TODO: Erstattes av ToolConnector. ved input stream melding må neste parameter være WebSocket
        if (isInputStreamMessage)
        {
            var secondParam = parameters[1].ParameterType;
            if (secondParam != typeof(WebSocket))
                throw new ArgumentException(
                    $"Function delegate for '{name}' must take WebSocket as second parameter when using StreamMessage."
                );
        }

        // --- Sjekk returtype JsonRpc ---
        bool isJsonRpcResponse =
             returnType == typeof(JsonRpcMessage)
             ||
            (returnType.IsGenericType &&
             returnType.GetGenericTypeDefinition() == typeof(Task<>) &&
             returnType.GenericTypeArguments[0] == typeof(JsonRpcMessage));

        // --- Sjekk returtype stream ---
        bool isStreamMessageResponse =
            (returnType.IsGenericType &&
             returnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            ||
            (returnType.IsGenericType &&
             returnType.GetGenericTypeDefinition() == typeof(Task<>) &&
             returnType.GenericTypeArguments[0].IsGenericType &&
             returnType.GenericTypeArguments[0].GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));

        // --- Sjekk returtype Task void ---
        bool isGenericTask = returnType.IsGenericType &&
            returnType.GetGenericTypeDefinition() == typeof(Task<>);

        bool isVoidTask = returnType == typeof(Task);

        // Tool: Sjekk retur verdier og kast feil dersom ugyldig
        if (functionType == FunctionTypeEnum.Tool && !isJsonRpcResponse && !isStreamMessageResponse && !isVoidTask)
            throw new ArgumentException(
                $"Tool delegate for '{name}' must return Task, Task<JsonRpcMessage>, JsonRpcMessage or IAsyncEnumerable<T>."
            );

        // Prompt: Sjekk retur verdier og kast feil dersom ugyldig
        if (functionType == FunctionTypeEnum.Prompt && !isJsonRpcResponse && !isGenericTask)
            throw new ArgumentException(
                $"Prompt delegate for '{name}' must return Task<JsonRpcMessage> or JsonRpcMessage."
            );

        // Kun Task tillatt for ToolConnector
        if (isInputToolConnector && !isVoidTask)
            throw new ArgumentException(
                $"Tool delegate for '{name}' use ToolConnector and must return Task."
            );

        var argumentType = new FunctionDetailArgumentType(
            IsToolConnector: isInputToolConnector,
            IsStreamMessage: isInputStreamMessage,
            IsJsonElementMessage: isJsonElementMessage,
            IsTypedJsonRpc: isTypedJson,
            ParameterType: firstParam
        );

        var resultType = new FunctionDetailResultType(
            IsVoidTask: isVoidTask,
            IsGenericTask: isGenericTask,
            IsIAsyncEnumerable: isStreamMessageResponse,
            IsJsonRpcResponse: isJsonRpcResponse
        );
        // --- Alt OK ---
        ConfiguredFunctions[name] = new FunctionDetails(
            FunctionType: functionType,
            FunctionArgumentType: argumentType,
            FunctionResultType: resultType,
            FunctionDelegate: functionDelegate);
    }



    /// <summary>
    /// Return tool delegate.
    /// </summary>
    /// <remarks>
    /// - If the tool method has more (or no matching) parameters than extraArgs, these will be resolved from the DI container.<br></br>
    /// </remarks>
    internal object? InvokeFunctionDelegate(string functionName, FunctionDetails functionDetails, params object[] extraArgs)
    {
        var parameters = functionDetails.FunctionDelegate.Method.GetParameters();
        var delegateArgs = new object[parameters.Length];

        using var scope = serviceProvider.CreateScope();
        var scopedServiceProvider = scope.ServiceProvider;

        int extraArgIndex = 0;
        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;

            // Hvis parameteren matcher en type i extraArgs, bruk den
            if (extraArgIndex < extraArgs.Length && paramType.IsAssignableFrom(extraArgs[extraArgIndex]?.GetType() ?? typeof(object)))
            {
                delegateArgs[i] = extraArgs[extraArgIndex];
                extraArgIndex++;
            }
            else
            {
                // Prøv å resolve fra DI
                delegateArgs[i] = scopedServiceProvider.GetService(paramType) ??
                    throw new ToolInternalErrorException($"{functionName}: Parameter type {paramType.Name} is missing");
            }
        }
        return functionDetails.FunctionDelegate.DynamicInvoke(delegateArgs);
    }

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
        
        return ConfiguredFunctions.Where(x => x.Value.FunctionType == functionType).Select(kvp =>
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

            string? inputSchema = null;
            if (string.IsNullOrWhiteSpace(attrInputSchema))
            {
                // 2. Hvis TypedJsonRpc<T> og ingen InputSchema → prøv schema-generator
                var generated = ToolSchemaGenerator.TryGenerateForTool(method, functionDetails);
                inputSchema = generated ?? @"{""type"":""object"",""properties"":{}}";
            }
            else
            {
                inputSchema = attrInputSchema;
            }

            // Validate InputSchema at runtime
            if (!string.IsNullOrEmpty(inputSchema))
            {
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
            
            return new FunctionDefinition(
                Name: functionName,
                Description: description!,
                InputSchema: inputSchema,
                Capabilities: capabilities
            );
        });
    }

    /// <summary>
    /// Returns tools filtered by transport capabilities.
    /// This ensures clients only see tools they can actually use.
    /// </summary>
    /// <param name="functionType">Tool, Prompt</param>
    /// <param name="transport">Transport type: "stdio", "http", "sse", or "ws"</param>
    /// <returns>List of tools compatible with the specified transport</returns>
    public IEnumerable<FunctionDefinition> GetFunctionsForTransport(FunctionTypeEnum functionType, string transport)
    {
        var allTools = GetAllFunctionDefinitions(functionType);

        // Determine allowed capabilities based on transport (tools)
        var allowedCapabilities = transport switch
        {
            "stdio" => ToolCapabilities.Standard,
            "http" => ToolCapabilities.Standard,
            "sse" => ToolCapabilities.Standard | ToolCapabilities.TextStreaming,
            "ws" => ToolCapabilities.Standard | ToolCapabilities.TextStreaming | ToolCapabilities.BinaryStreaming,
            _ => ToolCapabilities.Standard
        };
        
        // Filter tools based on capabilities
        return allTools.Where(tool =>
        {
            // If tool is Standard (default), it works on all transports
            if (tool.Capabilities == ToolCapabilities.Standard)
                return true;
            
            // Check if tool's capabilities are supported by this transport
            return (tool.Capabilities & allowedCapabilities) != 0;
        });
    }
}
