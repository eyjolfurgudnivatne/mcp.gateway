namespace AuthorizationMcpServer.Authorization;

/// <summary>
/// Attribute to require specific roles for tool invocation (v1.8.0).
/// Can be used multiple times to allow multiple roles (OR logic).
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RequireRoleAttribute : Attribute
{
    public string Role { get; }
    
    public RequireRoleAttribute(string role)
    {
        Role = role;
    }
}

/// <summary>
/// Attribute to mark a tool as publicly accessible (no authentication required).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AllowAnonymousAttribute : Attribute
{
}
