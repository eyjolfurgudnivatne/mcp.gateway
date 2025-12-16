namespace Mcp.Gateway.Tools;

using System.Text.RegularExpressions;

/// <summary>
/// Validates tool method names according to MCP protocol requirements.
/// MCP 2025-11-25: Tool names SHOULD contain only letters, numbers, underscores, hyphens, and dots.
/// Pattern: ^[a-zA-Z0-9_.-]{1,128}$
/// </summary>
public static partial class ToolMethodNameValidator
{
    // MCP 2025-11-25 compliant pattern: letters, numbers, underscores, hyphens, AND DOTS (1-128 chars)
    [GeneratedRegex(@"^[a-zA-Z0-9_.-]{1,128}$", RegexOptions.Compiled)]
    private static partial Regex McpCompliantRegex();
    
    // Legacy pattern (for backward compatibility warnings) - now same as compliant
    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9._-]{0,127}$", RegexOptions.Compiled)]
    private static partial Regex LegacyRegex();

    /// <summary>
    /// Validates a tool method name according to MCP 2025-11-25 protocol requirements.
    /// </summary>
    /// <param name="methodName">Tool method name to validate</param>
    /// <param name="error">Error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(string? methodName, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(methodName))
        {
            error = "Method name cannot be empty.";
            return false;
        }

        if (methodName.Length > 128)
        {
            error = "Method name too long (max 128 characters).";
            return false;
        }

        // Check MCP 2025-11-25 compliant pattern (now allows dots!)
        if (!McpCompliantRegex().IsMatch(methodName))
        {
            error = $"Invalid method name '{methodName}'. " +
                    $"Allowed: letters (a-z, A-Z), numbers (0-9), underscores (_), hyphens (-), dots (.). " +
                    $"NO spaces, NO special characters. " +
                    $"Pattern: ^[a-zA-Z0-9_.-]{{1,128}}$";
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// Validates a tool name and returns a warning if it's valid but not recommended.
    /// </summary>
    public static (bool isValid, string? error, string? warning) ValidateWithWarnings(string? methodName)
    {
        if (!IsValid(methodName, out var error))
        {
            return (false, error, null);
        }
        
        string? warning = null;
        
        // Check for potential issues
        if (methodName!.Contains("__"))
        {
            warning = "Tool name contains double underscores (__). Consider using single underscores for better readability.";
        }
        else if (methodName.Contains("--"))
        {
            warning = "Tool name contains double hyphens (--). Consider using single hyphens for better readability.";
        }
        else if (methodName.Contains(".."))
        {
            warning = "Tool name contains consecutive dots (..). Consider using single dots for better readability.";
        }
        else if (methodName.Length > 64)
        {
            warning = $"Tool name is quite long ({methodName.Length} chars). Consider shorter names for better usability.";
        }
        else if (methodName.StartsWith('.') || methodName.EndsWith('.'))
        {
            warning = "Tool name starts or ends with a dot. This may be confusing for users.";
        }
        
        return (true, null, warning);
    }
}
