namespace Mcp.Gateway.Tools;

using System.Text.RegularExpressions;

/// <summary>
/// Validates tool method names according to MCP protocol requirements.
/// GitHub Copilot and other MCP clients enforce strict naming: ^[a-zA-Z0-9_-]{1,128}$
/// </summary>
public static partial class ToolMethodNameValidator
{
    // MCP-compliant pattern: letters, numbers, underscores, hyphens only (1-128 chars)
    [GeneratedRegex(@"^[a-zA-Z0-9_-]{1,128}$", RegexOptions.Compiled)]
    private static partial Regex McpCompliantRegex();
    
    // Legacy pattern (allows dots) - for backward compatibility check
    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9._]{0,128}$", RegexOptions.Compiled)]
    private static partial Regex LegacyRegex();

    /// <summary>
    /// Validates a tool method name according to MCP protocol requirements.
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

        // Check MCP-compliant pattern (strict)
        if (!McpCompliantRegex().IsMatch(methodName))
        {
            // Check if it matches legacy pattern (has dots)
            if (LegacyRegex().IsMatch(methodName) && methodName.Contains('.'))
            {
                error = $"⚠️ WARNING: Method name '{methodName}' contains dots (.) which are NOT allowed by MCP clients like GitHub Copilot. " +
                        $"Use underscores (_) or hyphens (-) instead. " +
                        $"Example: '{methodName.Replace('.', '_')}' " +
                        $"Allowed pattern: ^[a-zA-Z0-9_-]{{1,128}}$";
                return false;
            }
            
            error = $"Invalid method name '{methodName}'. " +
                    $"Allowed: letters (a-z, A-Z), numbers (0-9), underscores (_), hyphens (-). " +
                    $"NO dots (.), NO spaces, NO special characters. " +
                    $"Pattern: ^[a-zA-Z0-9_-]{{1,128}}$";
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
        else if (methodName.Length > 64)
        {
            warning = $"Tool name is quite long ({methodName.Length} chars). Consider shorter names for better usability.";
        }
        
        return (true, null, warning);
    }
}
