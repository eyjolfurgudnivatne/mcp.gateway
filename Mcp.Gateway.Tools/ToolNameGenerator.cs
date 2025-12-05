namespace Mcp.Gateway.Tools;

using System.Text.RegularExpressions;

/// <summary>
/// Helper class for generating tool names from method names.
/// </summary>
public static class ToolNameGenerator
{
    /// <summary>
    /// Converts a method name to snake_case tool name.
    /// Examples:
    /// - "AddNumbersTool" → "add_numbers_tool"
    /// - "GetUserById" → "get_user_by_id"
    /// - "add_numbers" → "add_numbers" (already snake_case)
    /// - "Ping" → "ping"
    /// </summary>
    public static string ToSnakeCase(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
            return methodName;

        // Insert underscore before uppercase letters (except at start)
        // "AddNumbers" → "Add_Numbers"
        var withUnderscores = Regex.Replace(methodName, "(?<!^)([A-Z])", "_$1");

        // Convert to lowercase
        // "Add_Numbers" → "add_numbers"
        return withUnderscores.ToLowerInvariant();
    }

    /// <summary>
    /// Generates a humanized title from a tool name.
    /// Examples:
    /// - "add_numbers_tool" → "Add Numbers Tool"
    /// - "get_user_by_id" → "Get User By Id"
    /// - "ping" → "Ping"
    /// </summary>
    public static string ToHumanizedTitle(string toolName)
    {
        if (string.IsNullOrEmpty(toolName))
            return toolName;

        // Replace underscores and hyphens with spaces
        var withSpaces = toolName.Replace('_', ' ').Replace('-', ' ');

        // Capitalize each word
        var words = withSpaces.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var capitalizedWords = words.Select(word =>
            char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant());

        return string.Join(' ', capitalizedWords);
    }
}
