namespace Mcp.Gateway.Tools;

using System.Reflection;

/// <summary>
/// ToolService partial class - Resources support (v1.5.0)
/// </summary>
public partial class ToolService
{
    /// <summary>
    /// Validates that a resource URI follows the scheme://path pattern.
    /// </summary>
    /// <param name="uri">URI to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    private static bool IsValidResourceUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return false;
        
        // Check for scheme://path pattern
        var schemeIndex = uri.IndexOf("://");
        if (schemeIndex <= 0)
            return false;
        
        // Scheme must be at least 1 character
        var scheme = uri.Substring(0, schemeIndex);
        if (scheme.Length == 0)
            return false;
        
        // Path must exist after ://
        var path = uri.Substring(schemeIndex + 3);
        if (path.Length == 0)
            return false;
        
        return true;
    }

    /// <summary>
    /// Derives a human-readable name from a resource URI.
    /// Example: "file://logs/app.log" → "App Log"
    /// </summary>
    private static string DeriveNameFromUri(string uri)
    {
        // Extract filename or last path segment
        var schemeIndex = uri.IndexOf("://");
        if (schemeIndex < 0)
            return uri; // Fallback
        
        var path = uri.Substring(schemeIndex + 3);
        var segments = path.Split('/', '\\');
        var lastSegment = segments.LastOrDefault(s => !string.IsNullOrEmpty(s)) ?? path;
        
        // Remove extension and capitalize
        var nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(lastSegment);
        if (string.IsNullOrEmpty(nameWithoutExt))
            nameWithoutExt = lastSegment;
        
        // Simple title case (just capitalize first letter)
        if (nameWithoutExt.Length > 0)
        {
            return char.ToUpper(nameWithoutExt[0]) + nameWithoutExt.Substring(1);
        }
        
        return uri; // Fallback
    }

    /// <summary>
    /// Returns all registered resources with their metadata for MCP resources/list
    /// Sorted alphabetically by URI for consistent ordering (v1.6.0+)
    /// </summary>
    public IEnumerable<ResourceDefinition> GetAllResourceDefinitions()
    {
        EnsureFunctionsScanned();
        
        return ConfiguredFunctions
            .Where(x => x.Value.FunctionType == FunctionTypeEnum.Resource)
            .OrderBy(x => x.Key) // Sort alphabetically by URI for consistent ordering
            .Select(kvp =>
            {
                var uri = kvp.Key; // For resources, the key IS the URI
                var functionDetails = kvp.Value;
                
                // Get attribute from delegate method
                var method = functionDetails.FunctionDelegate.Method;
                var attr = method.GetCustomAttribute<McpResourceAttribute>();
                
                if (attr == null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"WARNING: Resource '{uri}' found but McpResourceAttribute is missing");
                    return null;
                }
                
                // Use attribute values or derive from URI
                var name = attr.Name ?? DeriveNameFromUri(uri);
                var description = attr.Description;
                var mimeType = attr.MimeType;
                var icon = attr.Icon;  // NEW: Extract icon (v1.6.5)
                
                return new ResourceDefinition(
                    Uri: uri,
                    Name: name,
                    Description: description,
                    MimeType: mimeType,
                    Icon: icon  // NEW: Include icon (v1.6.5)
                );
            })
            .Where(x => x != null)!; // Filter out nulls from missing attributes
    }

    /// <summary>
    /// Gets a specific resource definition by URI.
    /// </summary>
    /// <param name="uri">The resource URI to lookup</param>
    /// <returns>Resource definition</returns>
    /// <exception cref="ToolNotFoundException">Thrown if resource not found</exception>
    public ResourceDefinition GetResourceDefinition(string uri)
    {
        EnsureFunctionsScanned();
        
        if (!ConfiguredFunctions.TryGetValue(uri, out var functionDetails) || 
            functionDetails?.FunctionType != FunctionTypeEnum.Resource)
        {
            throw new ToolNotFoundException($"Resource '{uri}' is not configured.");
        }
        
        var method = functionDetails.FunctionDelegate.Method;
        var attr = method.GetCustomAttribute<McpResourceAttribute>();
        
        if (attr == null)
        {
            throw new ToolInternalErrorException($"Resource '{uri}' missing McpResourceAttribute");
        }
        
        return new ResourceDefinition(
            Uri: uri,
            Name: attr.Name ?? DeriveNameFromUri(uri),
            Description: attr.Description,
            MimeType: attr.MimeType,
            Icon: attr.Icon  // NEW: Include icon (v1.6.5)
        );
    }

    /// <summary>
    /// Invokes a resource delegate to read its content.
    /// </summary>
    /// <param name="uri">Resource URI</param>
    /// <param name="args">Arguments to pass to the delegate (typically JsonRpcMessage)</param>
    /// <returns>Result from the delegate invocation</returns>
    public object? InvokeResourceDelegate(string uri, params object[] args)
    {
        var functionDetails = GetFunctionDetails(uri);
        
        if (functionDetails.FunctionType != FunctionTypeEnum.Resource)
        {
            throw new ArgumentException($"'{uri}' is not a resource (it's a {functionDetails.FunctionType})");
        }
        
        return InvokeFunctionDelegate(uri, functionDetails, args);
    }
}
