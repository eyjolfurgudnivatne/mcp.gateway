namespace Mcp.Gateway.Tools;

using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;
using System.Text.Json;

/// <summary>
/// Implementation of IToolService
/// </summary>
public class ToolService(IServiceProvider serviceProvider)
{
    private readonly ConcurrentDictionary<string, ToolDetails> ConfiguredTools = new(StringComparer.OrdinalIgnoreCase);
    private volatile bool _toolsScanned = false;
    private readonly object _scanLock = new();

    internal record ToolDetails(ToolDetailArgumentType ToolArgumentType, ToolDetailResultType ToolResultType, Delegate ToolDelegate);
    internal record ToolDetailArgumentType(bool IsToolConnector, bool IsStreamMessage, bool IsJsonElementMessage);
    internal record ToolDetailResultType(bool IsVoidTask, bool IsGenericTask, bool IsIAsyncEnumerable, bool IsJsonRpcResponse);
    
    internal ToolDetails GetToolDetails(string toolName)
    {
        // Lazy scan on first tool lookup
        EnsureToolsScanned();
        
        if (!ConfiguredTools.TryGetValue(toolName, out var toolDelegate) || toolDelegate is null)
            throw new ToolNotFoundException($"Tool '{toolName}' is not configured.");
        return toolDelegate;
    }
    
    /// <summary>
    /// Ensures tools are scanned exactly once, thread-safely.
    /// </summary>
    private void EnsureToolsScanned()
    {
        if (_toolsScanned)
            return;
            
        lock (_scanLock)
        {
            if (_toolsScanned)
                return;
                
            ScanAndRegisterTools();
            _toolsScanned = true;
        }
    }
    
    /// <summary>
    /// Scans all loaded assemblies for methods marked with [McpTool] and registers them.
    /// </summary>
    private void ScanAndRegisterTools()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var toolCount = 0;
        
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
                        var attribute = method.GetCustomAttribute<McpToolAttribute>();
                        if (attribute is null)
                            continue;
                        
                        // Debug: Found a tool!
                        System.Diagnostics.Debug.WriteLine($"Found tool: {attribute.Name} in {type.FullName}.{method.Name}");
                        
                        // Create delegate
                        Delegate toolDelegate;
                        
                        if (method.IsStatic)
                        {
                            toolDelegate = Delegate.CreateDelegate(
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
                                
                            toolDelegate = Delegate.CreateDelegate(
                                System.Linq.Expressions.Expression.GetDelegateType(
                                    method.GetParameters().Select(p => p.ParameterType)
                                        .Concat(new[] { method.ReturnType })
                                        .ToArray()),
                                instance,
                                method);
                        }
                        
                        // Register the tool (uses existing RegisterTool logic)
                        RegisterTool(attribute.Name, toolDelegate);
                        toolCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scanning assembly {assembly.GetName().Name}: {ex.Message}");
                continue;
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"Tool scan complete. Registered {toolCount} tools.");
    }



    /// <summary>
    /// Registers a tool with the specified name and delegate.
    /// </summary>
    /// <param name="name">The name of the tool to register. Must be a valid tool name.</param>
    /// <param name="toolDelegate">The delegate for the tool's operations.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="name"/> is not a valid tool name.</exception>
    internal void RegisterTool(string name, Delegate toolDelegate)
    {
        if (!ToolMethodNameValidator.IsValid(name, out var error))
            throw new ArgumentException($"Invalid tool name '{name}': {error}");

        var method = toolDelegate.Method;
        var returnType = method.ReturnType;
        var parameters = method.GetParameters();

        // --- Sjekk parameter ---
        if (parameters.Length == 0)
            throw new ArgumentException($"Tool delegate for '{name}' must accept at least one parameter.");

        var firstParam = parameters[0].ParameterType;

        // --- Sjekk inputtype JsonRpc ---
        bool isJsonElementMessage =
            firstParam == typeof(JsonRpcMessage);

        // --- Sjekk inputtype stream ---
        bool isInputStreamMessage =
            firstParam  == typeof(StreamMessage);

        bool isInputToolConnector =
            firstParam == typeof(ToolConnector);

        // auda, ingen av delene
        if (!isJsonElementMessage && !isInputStreamMessage && !isInputToolConnector)
            throw new ArgumentException(
                $"Tool delegate for '{name}' must take JsonRpcMessage, StreamMessage or ToolConnector as first parameter."
            );

        // TODO: Erstattes av ToolConnector. dersom det er en stream, må det være minst en parameter til (for WebSocket)
        if (isInputStreamMessage && parameters.Length < 2)
            throw new ArgumentException(
                $"Tool delegate for '{name}' must take at least two parameters when using StreamMessage."
            );

        // TODO: Erstattes av ToolConnector. ved input stream melding må neste parameter være WebSocket
        if (isInputStreamMessage)
        {
            var secondParam = parameters[1].ParameterType;
            if (secondParam != typeof(WebSocket))
                throw new ArgumentException(
                    $"Tool delegate for '{name}' must take WebSocket as second parameter when using StreamMessage."
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
        bool isTask = returnType.IsGenericType &&
             returnType.GetGenericTypeDefinition() == typeof(Task<>);

        bool isGenericTask = method.ReturnType.IsGenericType &&
                     method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
        bool isVoidTask = method.ReturnType == typeof(Task);

        // Sjekk retur verdier og kast feil dersom ugyldig
        if (!isJsonRpcResponse && !isStreamMessageResponse && !isVoidTask)
            throw new ArgumentException(
                $"Tool delegate for '{name}' must return Task, Task<JsonRpcMessage>, JsonRpcMessage or IAsyncEnumerable<T>."
            );

        // Kun Task tillatt for ToolConnector
        if (isInputToolConnector && !isVoidTask)
            throw new ArgumentException(
                $"Tool delegate for '{name}' use ToolConnector and must return Task."
            );

        var argumentType = new ToolDetailArgumentType(
            IsToolConnector: isInputToolConnector,
            IsStreamMessage: isInputStreamMessage,
            IsJsonElementMessage: isJsonElementMessage
        );

        var resultType = new ToolDetailResultType(
            IsVoidTask: isVoidTask,
            IsGenericTask: isGenericTask,
            IsIAsyncEnumerable: isStreamMessageResponse,
            IsJsonRpcResponse: isJsonRpcResponse
        );
        // --- Alt OK ---
        ConfiguredTools[name] = new ToolDetails(
            ToolArgumentType: argumentType,
            ToolResultType: resultType,
            ToolDelegate: toolDelegate);
    }



    /// <summary>
    /// Return tool delegate.
    /// </summary>
    /// <remarks>
    /// - If the tool method has more (or no matching) parameters than extraArgs, these will be resolved from the DI container.<br></br>
    /// </remarks>
    internal object? InvokeToolDelegate(string toolName, ToolDetails toolDetails, params object[] extraArgs)
    {
        var parameters = toolDetails.ToolDelegate.Method.GetParameters();
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
                    throw new ToolInternalErrorException($"{toolName}: Parameter type {paramType.Name} is missing");
            }
        }
        return toolDetails.ToolDelegate.DynamicInvoke(delegateArgs);
    }

    /// <summary>
    /// Tool definition for MCP protocol (used by tools/list)
    /// </summary>
    public record ToolDefinition(string Name, string Description, string InputSchema);

    /// <summary>
    /// Returns all registered tools with their metadata for MCP tools/list
    /// </summary>
    public IEnumerable<ToolDefinition> GetAllToolDefinitions()
    {
        EnsureToolsScanned();
        
        return ConfiguredTools.Select(kvp =>
        {
            var toolName = kvp.Key;
            var toolDetails = kvp.Value;
            
            // Get attribute from delegate method
            var method = toolDetails.ToolDelegate.Method;
            var attr = method.GetCustomAttribute<McpToolAttribute>();
            
            var description = attr?.Description ?? "No description available";
            var inputSchema = attr?.InputSchema ?? @"{""type"":""object"",""properties"":{}}";
            
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
                            $"WARNING: Tool '{toolName}' has invalid InputSchema - root must be an object");
                    }
                    
                    // Check if 'type' is 'object'
                    if (root.TryGetProperty("type", out var typeElement))
                    {
                        var typeValue = typeElement.GetString();
                        if (typeValue != "object")
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"WARNING: Tool '{toolName}' InputSchema type is '{typeValue}', expected 'object'");
                        }
                    }
                    
                    // CRITICAL: Check if 'properties' is an object (not an array!)
                    if (root.TryGetProperty("properties", out var propertiesElement))
                    {
                        if (propertiesElement.ValueKind == JsonValueKind.Array)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"ERROR: Tool '{toolName}' has INVALID InputSchema - 'properties' must be an object {{}}, not an array []!");
                            System.Diagnostics.Debug.WriteLine(
                                $"  This will cause LLM clients to fail when parsing the schema.");
                            System.Diagnostics.Debug.WriteLine(
                                $"  Fix: Change \"properties\":[...] to \"properties\":{{...}}");
                        }
                        else if (propertiesElement.ValueKind != JsonValueKind.Object)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"WARNING: Tool '{toolName}' InputSchema 'properties' should be an object");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"ERROR: Tool '{toolName}' has malformed InputSchema JSON: {ex.Message}");
                }
            }
            
            return new ToolDefinition(
                Name: toolName,
                Description: description,
                InputSchema: inputSchema
            );
        });
    }
}
