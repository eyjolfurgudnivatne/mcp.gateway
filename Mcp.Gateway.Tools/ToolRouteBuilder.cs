namespace Mcp.Gateway.Tools;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

public class ToolRouteBuilder(WebApplication app, string toolPath)
{
    private readonly string _toolPath = toolPath;

    /// <summary>
    /// Creates a new route builder for a sub-tool group with the specified name, allowing further configuration of tool
    /// service routes.
    /// </summary>
    /// <param name="toolName">The name of the sub-tool group to map. Must be a valid tool method name and cannot be null or empty.</param>
    /// <returns>A new instance of ToolServiceRouteBuilder configured for the specified sub-tool group.</returns>
    /// <exception cref="ArgumentException">Thrown if the specified tool name is invalid according to tool method naming rules.</exception>
    public ToolRouteBuilder MapToolGroup(string toolName)
    {
        string _subToolPath = _toolPath;
        if (_subToolPath.Length > 0)
            _subToolPath += ".";

        _subToolPath += toolName;
        if (!ToolMethodNameValidator.IsValid(_subToolPath, out var error))
        {
            throw new ArgumentException($"Invalid tool name '{_subToolPath}': {error}");
        }

        return new ToolRouteBuilder(app, _subToolPath);
    }

    /// <summary>
    /// Registers a tool with the application using the specified name and handler delegate.
    /// </summary>
    /// <param name="toolName">The name of the tool to register. Must be a valid tool method name and cannot be null or empty.</param>
    /// <param name="handler">A delegate that defines the handler logic for the tool. This delegate will be invoked when the tool is executed.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="toolName"/> is not a valid tool method name.</exception>
    public void MapTool(string toolName, Delegate handler)
    {
        string _path = _toolPath;
        if (_path.Length > 0 && toolName.Length > 0)
            _path += ".";

        _path += toolName;
        if (!ToolMethodNameValidator.IsValid(_path, out var error))
        {
            throw new ArgumentException($"Invalid tool name '{_path}': {error}");
        }

        app.Services.GetRequiredService<ToolService>()
            .RegisterTool(_path, handler);
    }
}