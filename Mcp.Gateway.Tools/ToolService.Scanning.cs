namespace Mcp.Gateway.Tools;

using System.Reflection;
using System.Net.WebSockets;
using System.Text.Json;

/// <summary>
/// ToolService partial class - Scanning and Registration (v1.5.0)
/// </summary>
public partial class ToolService
{
    /// <summary>
    /// Scans all loaded assemblies for methods marked with [McpTool], [McpPrompt], or [McpResource] and registers them.
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
                    var methods = type.GetMethods(BindingFlags.Public | 
                                                 BindingFlags.Static | 
                                                 BindingFlags.Instance);
                    
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
                                // Auto-generate tool name if not specified
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
                                // Auto-generate prompt name if not specified
                                functionName = attribute.Name ?? ToolNameGenerator.ToSnakeCase(method.Name);
                            }
                        }
                        
                        // Check for McpResource attribute
                        if (functionName is null)
                        {
                            var attribute = method.GetCustomAttribute<McpResourceAttribute>();
                            if (attribute != null)
                            {
                                functionType = FunctionTypeEnum.Resource;
                                // Resources use URI as the key (not a snake_case name)
                                functionName = attribute.Uri;
                                
                                // Validate URI format
                                if (!IsValidResourceUri(attribute.Uri))
                                {
                                    System.Diagnostics.Debug.WriteLine(
                                        $"WARNING: Skipping resource '{method.Name}' in {type.FullName} - Invalid URI '{attribute.Uri}'");
                                    functionName = null;
                                    continue;
                                }
                            }
                        }

                        // Skip methods without custom attribute
                        if (functionName is null)
                            continue;
                        
                        // Validate name (skip URI validation for resources)
                        if (functionType != FunctionTypeEnum.Resource && 
                            !ToolMethodNameValidator.IsValid(functionName, out var validationError))
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"WARNING: Skipping function '{method.Name}' in {type.FullName} - Invalid function name '{functionName}': {validationError}");
                            continue;
                        }
                        
                        // Debug: Found a function!
                        System.Diagnostics.Debug.WriteLine(
                            $"Found function: {functionName} (from method: {method.Name}) in {type.FullName}");
                        
                        // Create delegate
                        Delegate functionDelegate;
                        
                        if (method.IsStatic)
                        {
                            functionDelegate = Delegate.CreateDelegate(
                                System.Linq.Expressions.Expression.GetDelegateType(
                                    [.. method.GetParameters().Select(p => p.ParameterType), method.ReturnType]),
                                method);
                        }
                        else
                        {
                            var instance = Activator.CreateInstance(type);
                            if (instance is null)
                                continue;
                                
                            functionDelegate = Delegate.CreateDelegate(
                                System.Linq.Expressions.Expression.GetDelegateType(
                                    [.. method.GetParameters().Select(p => p.ParameterType), method.ReturnType]),
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
    /// <param name="name">The name of the function to register. Must be a valid function name (or URI for resources).</param>
    /// <param name="functionType">Type of function (Tool, Prompt, or Resource)</param>
    /// <param name="functionDelegate">The delegate for the function's operations.</param>
    /// <exception cref="ArgumentException">Thrown if validation fails</exception>
    internal void RegisterFunction(string name, FunctionTypeEnum functionType, Delegate functionDelegate)
    {
        // For Resources, name is the URI - skip tool name validation
        if (functionType != FunctionTypeEnum.Resource)
        {
            if (!ToolMethodNameValidator.IsValid(name, out var error))
                throw new ArgumentException($"Invalid function name '{name}': {error}");
        }

        var method = functionDelegate.Method;
        var returnType = method.ReturnType;
        var parameters = method.GetParameters();

        // --- Sjekk parameter ---
        if (parameters.Length == 0)
            throw new ArgumentException($"Function delegate for '{name}' must accept at least one parameter.");

        var firstParam = parameters[0].ParameterType;

        // --- Sjekk inputtype JsonRpc ---
        bool isJsonElementMessage = firstParam == typeof(JsonRpcMessage);

        // --- Sjekk inputtype TypedJsonRpc ---
        bool isTypedJson = firstParam.IsGenericType &&
            firstParam.GetGenericTypeDefinition() == typeof(TypedJsonRpc<>);

        // --- Sjekk inputtype stream ---
        bool isInputToolConnector = firstParam == typeof(ToolConnector);

        // Validate input parameter types
        if (!isJsonElementMessage && !isTypedJson && !isInputToolConnector)
            throw new ArgumentException(
                $"Function delegate for '{name}' must take JsonRpcMessage, TypedJsonRpc<T> or ToolConnector as first parameter.");

        // Prompt-specific validation
        if (functionType == FunctionTypeEnum.Prompt && !isJsonElementMessage && !isTypedJson)
            throw new ArgumentException(
                $"Prompt delegate for '{name}' must take JsonRpcMessage or TypedJsonRpc<T> as first parameter.");

        // Resource-specific validation
        if (functionType == FunctionTypeEnum.Resource && !isJsonElementMessage && !isTypedJson)
            throw new ArgumentException(
                $"Resource delegate for '{name}' must take JsonRpcMessage or TypedJsonRpc<T> as first parameter.");

        // --- Sjekk returtype JsonRpc ---
        bool isJsonRpcResponse = returnType == typeof(JsonRpcMessage) ||
            (returnType.IsGenericType &&
             returnType.GetGenericTypeDefinition() == typeof(Task<>) &&
             returnType.GenericTypeArguments[0] == typeof(JsonRpcMessage));

        // --- Sjekk returtype stream ---
        bool isStreamMessageResponse =
            (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)) ||
            (returnType.IsGenericType &&
             returnType.GetGenericTypeDefinition() == typeof(Task<>) &&
             returnType.GenericTypeArguments[0].IsGenericType &&
             returnType.GenericTypeArguments[0].GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));

        // --- Sjekk returtype Task void ---
        bool isGenericTask = returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>);
        bool isVoidTask = returnType == typeof(Task);

        // Validate return types based on function type
        if (functionType == FunctionTypeEnum.Tool)
        {
            if (!isJsonRpcResponse && !isStreamMessageResponse && !isVoidTask)
                throw new ArgumentException(
                    $"Tool delegate for '{name}' must return Task, Task<JsonRpcMessage>, JsonRpcMessage or IAsyncEnumerable<T>.");
        }
        else if (functionType == FunctionTypeEnum.Prompt)
        {
            if (!isJsonRpcResponse && !isGenericTask)
                throw new ArgumentException(
                    $"Prompt delegate for '{name}' must return Task<JsonRpcMessage> or JsonRpcMessage.");
        }
        else if (functionType == FunctionTypeEnum.Resource)
        {
            if (!isJsonRpcResponse && !isGenericTask)
                throw new ArgumentException(
                    $"Resource delegate for '{name}' must return Task<JsonRpcMessage> or JsonRpcMessage.");
        }

        // Kun Task tillatt for ToolConnector
        if (isInputToolConnector && !isVoidTask)
            throw new ArgumentException(
                $"Tool delegate for '{name}' use ToolConnector and must return Task.");

        var argumentType = new FunctionDetailArgumentType(
            IsToolConnector: isInputToolConnector,
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

        // Register the function
        ConfiguredFunctions[name] = new FunctionDetails(
            FunctionType: functionType,
            FunctionArgumentType: argumentType,
            FunctionResultType: resultType,
            FunctionDelegate: functionDelegate);
    }
}
