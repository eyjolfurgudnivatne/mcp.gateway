---
layout: mcp-default
title: Authorization Server Example
description: Implement role-based access control for MCP tools
breadcrumbs:
  - title: Home
    url: /
  - title: Examples
    url: /examples/authorization/
  - title: Authorization Server
    url: /examples/authorization/
toc: true
---

# Authorization Server Example

Implement production-ready role-based access control using Lifecycle Hooks.

## Overview

The Authorization server demonstrates:
- ✅ **Role-based access control** - Admin, Manager, User roles
- ✅ **Custom attributes** - `[RequireRole]` and `[AllowAnonymous]`
- ✅ **Lifecycle hooks** - Enforce authorization before tool execution
- ✅ **JWT authentication** - Industry-standard token validation
- ✅ **Audit logging** - Track authorization decisions

## Complete Code

### Program.cs

```csharp
using Mcp.Gateway.Tools;
using AuthorizationMcpServer.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddHttpContextAccessor();

// Register MCP Gateway
builder.AddToolsService();

// Add authorization lifecycle hook
builder.AddToolLifecycleHook<AuthorizationHook>();

var app = builder.Build();

// stdio mode
if (args.Contains("--stdio"))
{
    await ToolInvoker.RunStdioModeAsync(app.Services);
    return;
}

// Authentication middleware
app.Use(async (context, next) =>
{
    // Skip auth for health check
    if (context.Request.Path == "/health")
    {
        await next();
        return;
    }
    
    // Check Authorization header
    if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        
        await context.Response.WriteAsJsonAsync(new
        {
            jsonrpc = "2.0",
            error = new
            {
                code = -32000,
                message = "Unauthorized",
                data = new { detail = "Missing Authorization header" }
            },
            id = (object?)null
        });
        return;
    }
    
    // Extract and validate token
    var token = authHeader.ToString().Replace("Bearer ", "");
    var validation = ValidateToken(token);
    
    if (!validation.IsValid)
    {
        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        
        await context.Response.WriteAsJsonAsync(new
        {
            jsonrpc = "2.0",
            error = new
            {
                code = -32002,
                message = "Forbidden",
                data = new { detail = validation.ErrorMessage }
            },
            id = (object?)null
        });
        return;
    }
    
    // Store user info for AuthorizationHook
    context.Items["UserId"] = validation.UserId;
    context.Items["UserRoles"] = validation.Roles;
    
    await next();
});

app.UseWebSockets();
app.UseProtocolVersionValidation();
app.MapStreamableHttpEndpoint("/mcp");

app.Run();

// Simple token validation (use JWT in production!)
TokenValidation ValidateToken(string token)
{
    // Demo tokens (use proper JWT validation in production)
    return token switch
    {
        "admin-token" => new(true, "admin-user", new[] { "Admin" }),
        "manager-token" => new(true, "manager-user", new[] { "Manager" }),
        "user-token" => new(true, "regular-user", new[] { "User" }),
        _ => new(false, null, null, "Invalid token")
    };
}

record TokenValidation(
    bool IsValid,
    string? UserId,
    string[]? Roles,
    string? ErrorMessage = null);
```

### AuthorizationHook.cs

```csharp
using Mcp.Gateway.Tools;
using Mcp.Gateway.Tools.Lifecycle;
using System.Reflection;

namespace AuthorizationMcpServer.Authorization;

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
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return Task.CompletedTask;
        
        var method = GetToolMethod(toolName);
        if (method == null) return Task.CompletedTask;
        
        // Check [AllowAnonymous]
        if (method.GetCustomAttribute<AllowAnonymousAttribute>() != null)
        {
            _logger.LogDebug("Tool '{ToolName}' allows anonymous access", toolName);
            return Task.CompletedTask;
        }
        
        // Get required roles
        var requiredRoles = method.GetCustomAttributes<RequireRoleAttribute>()
            .Select(attr => attr.Role)
            .ToList();
        
        if (!requiredRoles.Any()) return Task.CompletedTask;
        
        // Get user roles
        var userId = httpContext.Items["UserId"] as string ?? "anonymous";
        var userRoles = httpContext.Items["UserRoles"] as string[] 
            ?? Array.Empty<string>();
        
        // Check authorization (OR logic - any role matches)
        var hasRequiredRole = requiredRoles.Any(role => 
            userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
        
        if (!hasRequiredRole)
        {
            _logger.LogWarning(
                "User '{UserId}' denied access to '{ToolName}'. " +
                "Required: [{Required}], Has: [{UserRoles}]",
                userId, toolName,
                string.Join(", ", requiredRoles),
                string.Join(", ", userRoles));
            
            throw new ToolInvalidParamsException(
                $"Insufficient permissions. Required: {string.Join(" or ", requiredRoles)}",
                toolName);
        }
        
        _logger.LogInformation(
            "User '{UserId}' authorized to invoke '{ToolName}'",
            userId, toolName);
        
        return Task.CompletedTask;
    }
    
    public Task OnToolCompletedAsync(
        string toolName,
        JsonRpcMessage response,
        TimeSpan duration)
    {
        return Task.CompletedTask;
    }
    
    public Task OnToolFailedAsync(
        string toolName,
        Exception error,
        TimeSpan duration)
    {
        return Task.CompletedTask;
    }
    
    private MethodInfo? GetToolMethod(string toolName)
    {
        if (_methodsCached && _toolMethods.TryGetValue(toolName, out var cached))
            return cached;
        
        if (!_methodsCached)
        {
            ScanToolMethods();
            _methodsCached = true;
        }
        
        return _toolMethods.TryGetValue(toolName, out var method) ? method : null;
    }
    
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
                    var methods = type.GetMethods(
                        BindingFlags.Public | BindingFlags.Instance);
                    
                    foreach (var method in methods)
                    {
                        var toolAttr = method.GetCustomAttribute<McpToolAttribute>();
                        if (toolAttr != null)
                        {
                            _toolMethods[toolAttr.Name] = method;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "Failed to scan assembly {Assembly}", assembly.FullName);
            }
        }
    }
}
```

### Authorization Attributes

```csharp
namespace AuthorizationMcpServer.Authorization;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RequireRoleAttribute : Attribute
{
    public string Role { get; }
    
    public RequireRoleAttribute(string role)
    {
        Role = role;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class AllowAnonymousAttribute : Attribute
{
}
```

### AdminTools.cs

```csharp
using Mcp.Gateway.Tools;
using AuthorizationMcpServer.Authorization;

namespace AuthorizationMcpServer.Tools;

public class AdminTools
{
    [McpTool("delete_user", Description = "Delete a user (Admin only)")]
    [RequireRole("Admin")]
    public JsonRpcMessage DeleteUser(TypedJsonRpc<DeleteUserArgs> request)
    {
        var args = request.GetParams()!;
        
        // Delete user logic...
        
        return ToolResponse.Success(request.Id, 
            new { deleted = true, userId = args.UserId });
    }
    
    [McpTool("create_user", Description = "Create user (Admin or Manager)")]
    [RequireRole("Admin")]
    [RequireRole("Manager")]  // Admin OR Manager
    public JsonRpcMessage CreateUser(TypedJsonRpc<CreateUserArgs> request)
    {
        var args = request.GetParams()!;
        
        // Create user logic...
        
        return ToolResponse.Success(request.Id, 
            new { created = true, username = args.Username });
    }
    
    [McpTool("get_public_info", Description = "Get public information")]
    [AllowAnonymous]
    public JsonRpcMessage GetPublicInfo(JsonRpcMessage request)
    {
        return ToolResponse.Success(request.Id, 
            new { info = "Public data available to all" });
    }
}

public record DeleteUserArgs(string UserId);
public record CreateUserArgs(string Username, string Email);
```

## Testing

### Test with curl

**Admin-only tool (success):**

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -H "Authorization: Bearer admin-token" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "delete_user",
      "arguments": { "UserId": "user-123" }
    },
    "id": 1
  }'
```

**Admin-only tool (denied):**

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -H "Authorization: Bearer user-token" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "delete_user",
      "arguments": { "UserId": "user-123" }
    },
    "id": 2
  }'
```

**Response (denied):**

```json
{
  "jsonrpc": "2.0",
  "error": {
    "code": -32603,
    "message": "Internal error",
    "data": {
      "detail": "Insufficient permissions. Required: Admin"
    }
  },
  "id": 2
}
```

## Production Patterns

### JWT Authentication

Replace simple token validation with proper JWT:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://your-identity-server.com";
        options.Audience = "mcp-gateway-api";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

app.UseAuthentication();
app.UseAuthorization();

// Extract claims
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Items["UserId"] = context.User.FindFirst("sub")?.Value;
        context.Items["UserRoles"] = context.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();
    }
    await next();
});
```

### Audit Logging

Track all authorization decisions:

```csharp
public class AuditLoggingHook : IToolLifecycleHook
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditLogger _auditLogger;
    
    public async Task OnToolInvokingAsync(string toolName, JsonRpcMessage request)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext?.Items["UserId"] as string ?? "anonymous";
        
        await _auditLogger.LogAsync(new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            ToolName = toolName,
            Action = "Invoking",
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }
    
    // ... other methods
}

// Register
builder.AddToolLifecycleHook<AuthorizationHook>();
builder.AddToolLifecycleHook<AuditLoggingHook>();
```

## Integration Tests

```csharp
[Fact]
public async Task AdminTool_WithAdminToken_Succeeds()
{
    // Arrange
    using var server = new McpGatewayFixture();
    var client = server.CreateClient();
    
    var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
    {
        Content = JsonContent.Create(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "delete_user",
                arguments = new { UserId = "user-123" }
            },
            id = 1
        })
    };
    request.Headers.Authorization = 
        new AuthenticationHeaderValue("Bearer", "admin-token");
    
    // Act
    var response = await client.SendAsync(request);
    
    // Assert
    response.EnsureSuccessStatusCode();
}

[Fact]
public async Task AdminTool_WithUserToken_ReturnsError()
{
    // Arrange
    using var server = new McpGatewayFixture();
    var client = server.CreateClient();
    
    var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
    {
        Content = JsonContent.Create(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "delete_user",
                arguments = new { UserId = "user-123" }
            },
            id = 1
        })
    };
    request.Headers.Authorization = 
        new AuthenticationHeaderValue("Bearer", "user-token");
    
    // Act
    var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    
    // Assert
    Assert.Contains("Insufficient permissions", content);
}
```

## Source Code

Full source code available at:
- **GitHub:** [Examples/AuthorizationMcpServer](https://github.com/eyjolfurgudnivatne/mcp.gateway/tree/main/Examples/AuthorizationMcpServer)
- **Tests:** [Examples/AuthorizationMcpServerTests](https://github.com/eyjolfurgudnivatne/mcp.gateway/tree/main/Examples/AuthorizationMcpServerTests)

## See Also

- [Authorization Guide](/mcp.gateway/features/authorization/) - Complete authorization patterns
- [Lifecycle Hooks](/mcp.gateway/features/lifecycle-hooks/) - Hook infrastructure
- [Lifecycle Hooks API](/mcp.gateway/api/lifecycle-hooks/) - Complete API docs
