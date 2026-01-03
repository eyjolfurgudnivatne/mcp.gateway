namespace Mcp.Gateway.Tools;

using System.Collections.Concurrent;

/// <summary>
/// Core service for discovering, registering, and invoking MCP functions (Tools, Prompts, Resources).
/// This is the main partial class - see other ToolService.*.cs files for specific functionality.
/// </summary>
/// <remarks>
/// Partial class files:
/// - ToolService.cs (this file) - Core infrastructure
/// - ToolService.Scanning.cs - Function scanning and registration
/// - ToolService.Functions.cs - Function definitions (Tools and Prompts)
/// - ToolService.Invocation.cs - Function invocation with DI
/// - ToolService.Prompts.cs - Prompts-specific functionality
/// - ToolService.Resources.cs - Resource-specific functionality
/// </remarks>
public partial class ToolService(IServiceProvider serviceProvider)
{
    private readonly ConcurrentDictionary<string, FunctionDetails> ConfiguredFunctions = new(StringComparer.OrdinalIgnoreCase);
    private volatile bool _functionsScanned = false;
    private readonly object _scanLock = new();

    /// <summary>
    /// Type of MCP function (Tool, Prompt, or Resource)
    /// </summary>
    public enum FunctionTypeEnum
    {
        Tool,
        Prompt,
        Resource
    }

    /// <summary>
    /// Internal record containing function metadata and delegate
    /// </summary>
    internal record FunctionDetails(
        FunctionTypeEnum FunctionType, 
        FunctionDetailArgumentType FunctionArgumentType, 
        FunctionDetailResultType FunctionResultType, 
        Delegate FunctionDelegate);
    
    /// <summary>
    /// Information about function input parameter types
    /// </summary>
    internal record FunctionDetailArgumentType(
        bool IsToolConnector, 
        bool IsJsonElementMessage, 
        bool IsTypedJsonRpc, 
        Type ParameterType);
    
    /// <summary>
    /// Information about function return type
    /// </summary>
    internal record FunctionDetailResultType(
        bool IsVoidTask, 
        bool IsGenericTask, 
        bool IsIAsyncEnumerable, 
        bool IsJsonRpcResponse,
        bool IsTypedJsonRpcResponse,
        Type ReturnType);
    
    /// <summary>
    /// Gets function details by name (or URI for resources).
    /// Triggers lazy scanning on first call.
    /// </summary>
    /// <param name="functionName">Function name or resource URI</param>
    /// <returns>Function details</returns>
    /// <exception cref="ToolNotFoundException">Thrown if function is not found</exception>
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
}
