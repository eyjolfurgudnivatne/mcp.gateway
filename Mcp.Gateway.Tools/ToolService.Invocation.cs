namespace Mcp.Gateway.Tools;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// ToolService partial class - Function Invocation (v1.5.0)
/// </summary>
public partial class ToolService
{
    /// <summary>
    /// Invokes a function delegate with dependency injection support.
    /// </summary>
    /// <param name="functionName">Name of the function being invoked (for error messages)</param>
    /// <param name="functionDetails">Function metadata and delegate</param>
    /// <param name="extraArgs">Additional arguments to pass to the function</param>
    /// <returns>Result from the delegate invocation</returns>
    /// <remarks>
    /// - If the tool method has more (or no matching) parameters than extraArgs, these will be resolved from the DI container.
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
            if (extraArgIndex < extraArgs.Length && 
                paramType.IsAssignableFrom(extraArgs[extraArgIndex]?.GetType() ?? typeof(object)))
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
}
