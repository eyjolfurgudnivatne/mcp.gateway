namespace AuthorizationMcpServer.Authorization;

using Mcp.Gateway.Tools;
using Mcp.Gateway.Tools.Lifecycle;
using System.Reflection;

/// <summary>
/// Lifecycle hook that enforces role-based authorization on tool invocations (v1.8.0).
/// Reads [RequireRole] and [AllowAnonymous] attributes from tool methods.
/// </summary>
public class AuthorizationHook : IToolLifecycleHook
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthorizationHook> _logger;
    private readonly Dictionary<string, MethodInfo> _toolMethods = new();
    private bool _methodsCached = false;
    
    public AuthorizationHook(
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthorizationHook> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }
    
    public Task OnToolInvokingAsync(string toolName, JsonRpcMessage request)
    {
        // Get HttpContext (contains user info from middleware)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("No HttpContext available for authorization check");
            return Task.CompletedTask;
        }
        
        // Get tool method (cached after first scan)
        var method = GetToolMethod(toolName);
        if (method == null)
        {
            _logger.LogWarning("Tool method '{ToolName}' not found for authorization check", toolName);
            return Task.CompletedTask;
        }
        
        // Check for [AllowAnonymous]
        if (method.GetCustomAttribute<AllowAnonymousAttribute>() != null)
        {
            _logger.LogDebug("Tool '{ToolName}' allows anonymous access", toolName);
            return Task.CompletedTask;
        }
        
        // Get required roles from [RequireRole] attributes
        var requiredRoles = method.GetCustomAttributes<RequireRoleAttribute>()
            .Select(attr => attr.Role)
            .ToList();
        
        if (!requiredRoles.Any())
        {
            _logger.LogDebug("Tool '{ToolName}' has no role requirements", toolName);
            return Task.CompletedTask;
        }
        
        // Get user info from HttpContext
        var userId = httpContext.Items["UserId"] as string ?? "anonymous";
        var userRoles = httpContext.Items["UserRoles"] as List<string> 
            ?? new List<string>();
        
        // Check if user has ANY of the required roles (OR logic)
        var hasRequiredRole = requiredRoles.Any(role => 
            userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
        
        if (!hasRequiredRole)
        {
            _logger.LogWarning(
                "User '{UserId}' attempted to invoke tool '{ToolName}' without required roles. " +
                "Required: [{Required}], User has: [{UserRoles}]",
                userId, toolName, 
                string.Join(", ", requiredRoles),
                string.Join(", ", userRoles));
            
            // Throw ToolInvalidParamsException instead of UnauthorizedAccessException
            // This ensures proper JSON-RPC error response (-32602)
            // NOTE: Semantically this is "Insufficient permissions" but we use Invalid params
            // to leverage existing error handling in ToolInvoker (v1.8.0)
            throw new ToolInvalidParamsException(
                $"Insufficient permissions to invoke '{toolName}'. " +
                $"Required role(s): {string.Join(" or ", requiredRoles)}. " +
                $"User has: {string.Join(", ", userRoles)}.",
                toolName);
        }
        
        _logger.LogInformation(
            "User '{UserId}' authorized to invoke tool '{ToolName}' with roles: {Roles}",
            userId, toolName, string.Join(", ", userRoles));
        
        return Task.CompletedTask;
    }
    
    public Task OnToolCompletedAsync(string toolName, JsonRpcMessage response, TimeSpan duration)
    {
        // Optional: Log successful authorized invocations
        return Task.CompletedTask;
    }
    
    public Task OnToolFailedAsync(string toolName, Exception error, TimeSpan duration)
    {
        // Optional: Log failed authorized invocations
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Gets tool method by scanning assemblies for [McpTool] attributes.
    /// Results are cached after first scan.
    /// </summary>
    private MethodInfo? GetToolMethod(string toolName)
    {
        // Return cached result if available
        if (_methodsCached && _toolMethods.TryGetValue(toolName, out var cachedMethod))
        {
            return cachedMethod;
        }
        
        // Scan assemblies for tool methods (only once)
        if (!_methodsCached)
        {
            ScanToolMethods();
            _methodsCached = true;
        }
        
        // Try again after scan
        return _toolMethods.TryGetValue(toolName, out var method) ? method : null;
    }
    
    /// <summary>
    /// Scans all loaded assemblies for methods with [McpTool] attribute.
    /// </summary>
    private void ScanToolMethods()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        
        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();
                
                foreach (var type in types)
                {
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                    
                    foreach (var method in methods)
                    {
                        var toolAttr = method.GetCustomAttribute<McpToolAttribute>();
                        if (toolAttr != null)
                        {
                            var toolName = toolAttr.Name ?? "";
                            _toolMethods[toolName] = method;
                            _logger.LogDebug("Found tool method: {ToolName} in {Type}.{Method}", 
                                toolName, type.Name, method.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan assembly {Assembly} for tool methods", assembly.FullName);
            }
        }
        
        _logger.LogInformation("Scanned assemblies and found {Count} tool methods", _toolMethods.Count);
    }
}
