namespace Mcp.Gateway.Tools;

using Mcp.Gateway.Tools.Schema;
using System.Collections.Generic;
using System.Reflection;

public partial class ToolService
{
    /// <summary>
    /// Returns all registered functions with their metadata for MCP prompts/list
    /// Sorted alphabetically by name for consistent ordering (v1.6.0+)
    /// </summary>
    public IEnumerable<PromptDefinition> GetAllPromptsDefinitions()
    {
        EnsureFunctionsScanned();

        return ConfiguredFunctions
            .Where(x => x.Value.FunctionType == FunctionTypeEnum.Prompt)
            .OrderBy(x => x.Key) // Sort alphabetically by function name for consistent ordering
            .Select(kvp =>
            {
                var functionName = kvp.Key;
                var functionDetails = kvp.Value;

                // Get attribute from delegate method
                var method = functionDetails.FunctionDelegate.Method;
                string? description = null;
                List<PromptArgument> arguments = [];                                

                var attr = method.GetCustomAttribute<McpPromptAttribute>();
                description = attr?.Description ?? "No description available";

                if (string.IsNullOrWhiteSpace(attr?.InputSchema))
                {
                    // Try to generate schema from method parameter type
                    arguments = PromptSchemaGenerator.TryGenerateForPrompt(method, functionDetails);
                }

                // get icons from delegate method
                var iconsAttr = method.GetCustomAttributes<McpIconAttribute>();
                List<McpIconDefinition> icons = [];
                foreach (var iconA in iconsAttr)
                    icons.Add(new(
                        iconA.Src,
                        iconA.MimeType,
                        iconA.Sizes,
                        iconA.Theme?.ToString().ToLower()));

                if (!string.IsNullOrEmpty(attr?.Icon))
                    icons.Add(new(attr.Icon, null, null, null));

                return new PromptDefinition
                {
                    Name = functionName,
                    Title = attr?.Title,
                    Description = description!,
                    Arguments = arguments.Count == 0 ? null : arguments,
                    Icons = icons.Count == 0 ? null : icons
                };              
            });
    }
}
