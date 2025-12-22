namespace MEAIIntegration;

using Microsoft.Extensions.AI;
using System.Reflection;
using System.Text.Json;

public interface IMEAIInvoker
{
    ValueTask<AIFunction[]> BuildToolListAsync();
}

public class ToolDetails
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JsonElement JsonSchema { get; set; } = JsonDocument.Parse("{\"type\": \"object\", \"properties\": {}}").RootElement;
    public JsonElement? ReturnJsonSchema { get; set; } = null;
}

/// <summary>
/// Custom AIFunction that wraps MCP Gateway tool functionality
/// </summary>
public sealed class McpGatewayTool(
    ToolDetails toolDetails,
    Func<AIFunctionArguments, CancellationToken, ValueTask<object?>> invokeFunc) : AIFunction
{
    public override string Name => toolDetails.Name;
    public override string Description => toolDetails.Description;
    public override JsonElement JsonSchema => toolDetails.JsonSchema;
    public override JsonElement? ReturnJsonSchema => toolDetails.ReturnJsonSchema;

    public override string ToString() => Name;
    public override MethodInfo? UnderlyingMethod => null;

    public new ValueTask<object?> InvokeAsync(
        AIFunctionArguments? arguments = null,
        CancellationToken cancellationToken = default) =>
        InvokeCoreAsync(arguments ?? [], cancellationToken);

    // Here we call out tool
    protected override ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        return invokeFunc(arguments, cancellationToken);
    }
}
