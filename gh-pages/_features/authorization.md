---
layout: mcp-default
title: Authorization
description: Role-based authorization for MCP tools using lifecycle hooks
breadcrumbs:
  - title: Home
    url: /
  - title: Authorization
    url: /features/authorization/
toc: true
---

# Authorization

**Added in:** v1.8.0  
**Status:** Production-ready  
**Purpose:** Role-based authorization for MCP tools using lifecycle hooks

## Overview

MCP Gateway supports flexible authorization patterns through:
- **Custom attributes** - `[RequireRole]` and `[AllowAnonymous]`
- **Lifecycle hooks** - `IToolLifecycleHook` for enforcement
- **Middleware** - Transport-level authentication (HTTP/WebSocket/SSE)
- **DI integration** - Full dependency injection support

Perfect for:
- 🔐 **Role-based access control** - Admin, Manager, User roles
- 🎯 **Multi-tenant systems** - Per-tenant authorization
- 📊 **Audit logging** - Track who invoked which tools
- 🚀 **Production deployments** - Enterprise-ready patterns

## Quick Start

### 1. Define Authorization Attributes

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RequireRoleAttribute : Attribute
{
    public string Role { get; }
    public RequireRoleAttribute(string role) => Role = role;
}

[AttributeUsage(AttributeTargets.Method)]
public class AllowAnonymousAttribute : Attribute
{
}
```

### 2. Create Authorization Hook

```csharp
using Mcp.Gateway.Tools.Lifecycle;

public class AuthorizationHook : IToolLifecycleHook
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Dictionary<string, MethodInfo> _toolMethods = new();
    
    public Task OnToolInvokingAsync(string toolName, JsonRpcMessage request)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return Task.CompletedTask;
        
        var method = GetToolMethod(toolName);
        if (method == null) return Task.CompletedTask;
        
        // Check [AllowAnonymous]
        if (method.GetCustomAttribute<AllowAnonymousAttribute>() != null)
            return Task.CompletedTask;
        
        // Get required roles
        var requiredRoles = method.GetCustomAttributes<RequireRoleAttribute>()
            .Select(attr => attr.Role)
            .ToList();
        
        if (!requiredRoles.Any()) return Task.CompletedTask;
        
        // Get user roles
        var userRoles = httpContext.Items["UserRoles"] as List<string> ?? new();
        
        // Check authorization
        if (!requiredRoles.Any(role => userRoles.Contains(role)))
        {
            throw new ToolInvalidParamsException(
                $"Insufficient permissions. Required: {string.Join(" or ", requiredRoles)}",
                toolName);
        }
        
        return Task.CompletedTask;
    }
    
    // ... other interface methods
}
```

### 3. Add Authentication Middleware

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.AddToolsService();
builder.AddToolLifecycleHook<AuthorizationHook>();

var app = builder.Build();

// Authentication middleware
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        context.Response.StatusCode = 401;
        return;
    }
    
    var token = authHeader.ToString().Replace("Bearer ", "");
    var validation = await ValidateTokenAsync(token);
    
    if (!validation.IsValid)
    {
        context.Response.StatusCode = 403;
        return;
    }
    
    // Store user info for AuthorizationHook
    context.Items["UserId"] = validation.UserId;
    context.Items["UserRoles"] = validation.Roles;
    
    await next();
});

app.UseWebSockets();
app.MapStreamableHttpEndpoint("/mcp");
app.Run();
```

### 4. Use Attributes on Tools

```csharp
public class AdminTools
{
    [McpTool("delete_user")]
    [RequireRole("Admin")]
    public JsonRpcMessage DeleteUser(JsonRpcMessage request)
    {
        // Admin-only logic
        return ToolResponse.Success(request.Id, new { deleted = true });
    }
    
    [McpTool("create_user")]
    [RequireRole("Admin")]
    [RequireRole("Manager")]  // Admin OR Manager
    public JsonRpcMessage CreateUser(JsonRpcMessage request)
    {
        return ToolResponse.Success(request.Id, new { created = true });
    }
    
    [McpTool("get_public_info")]
    [AllowAnonymous]
    public JsonRpcMessage GetPublicInfo(JsonRpcMessage request)
    {
        return ToolResponse.Success(request.Id, new { info = "Public" });
    }
}
```

## Authorization Flow

```
Client Request (with Bearer token)
          ↓
Middleware validates token
          ↓
Store user info in HttpContext.Items
          ↓
ToolInvoker invokes lifecycle hooks
          ↓
AuthorizationHook checks [RequireRole]
          ↓
Tool executes (if authorized)
```

## Error Responses

### Missing Authorization Header (401)

```json
{
  "jsonrpc": "2.0",
  "error": {
    "code": -32000,
    "message": "Unauthorized",
    "data": {
      "detail": "Missing Authorization header"
    }
  }
}
```

### Insufficient Permissions (403)

```json
{
  "jsonrpc": "2.0",
  "error": {
    "code": -32603,
    "message": "Internal error",
    "data": {
      "detail": "Insufficient permissions. Required: Admin. User has: User."
    }
  }
}
```

## Production Patterns

### JWT Authentication

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://your-identity-server.com";
        options.Audience = "mcp-gateway-api";
    });

builder.Services.AddAuthorization();

app.UseAuthentication();
app.UseAuthorization();

// Extract claims to HttpContext.Items
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Items["UserId"] = context.User.FindFirst("sub")?.Value;
        context.Items["UserRoles"] = context.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
    }
    await next();
});
```

### Audit Logging

```csharp
public class AuditLoggingHook : IToolLifecycleHook
{
    private readonly IAuditLogger _auditLogger;
    
    public async Task OnToolInvokingAsync(string toolName, JsonRpcMessage request)
    {
        await _auditLogger.LogAccessAttemptAsync(new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            ToolName = toolName,
            UserId = GetUserId(),
            Action = "ToolInvoking"
        });
    }
    
    // ... other methods
}

// Register both hooks
builder.AddToolLifecycleHook<AuthorizationHook>();
builder.AddToolLifecycleHook<AuditLoggingHook>();
```

### Multi-Tenant Authorization

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class RequireTenantAttribute : Attribute
{
    public string[] AllowedTenants { get; }
    public RequireTenantAttribute(params string[] tenants) => AllowedTenants = tenants;
}

// Usage:
[McpTool("tenant_tool")]
[RequireTenant("tenant-a", "tenant-b")]
[RequireRole("Admin")]
public JsonRpcMessage TenantTool(JsonRpcMessage request) { /* ... */ }
```

## Security Best Practices

### 1. Never Trust Client Input

```csharp
// ❌ BAD: Trusting client-provided role
var clientRole = request.GetParams().GetProperty("role").GetString();

// ✅ GOOD: Only trust server-validated token
var roles = httpContext.Items["UserRoles"] as List<string>;
```

### 2. Validate Token Properly

```csharp
var validationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = GetSecurityKey(),
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero  // No tolerance for expired tokens
};
```

### 3. Log Authorization Failures

```csharp
_logger.LogWarning(
    "Authorization failed for user '{UserId}' attempting '{ToolName}'. " +
    "Required: {Required}, Has: {Has}",
    userId, toolName, requiredRoles, userRoles);
```

### 4. Rate Limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Items["UserId"]?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

app.UseRateLimiter();
```

## Performance

### Method Caching

The `AuthorizationHook` caches tool methods after first scan:

```
First call:        ~10-50ms (assembly scanning)
Subsequent calls:  <1ms (dictionary lookup)
Memory:            ~200 bytes per tool
```

### Compiled Expressions

Use compiled lambda expressions for repeated attribute checks:

```
Without caching:   15-25 μs per check
With caching:      1-2 μs per check
Improvement:       ~10x faster
```

## Testing

### Unit Testing

```csharp
[Fact]
public async Task OnToolInvokingAsync_WithAdminRole_AllowsAccess()
{
    var httpContextAccessor = CreateMockAccessor(
        userId: "admin-user",
        roles: new List<string> { "Admin" });
    
    var hook = new AuthorizationHook(httpContextAccessor);
    var request = JsonRpcMessage.CreateRequest("delete_user", "1");
    
    // Should not throw
    await hook.OnToolInvokingAsync("delete_user", request);
}
```

### Integration Testing

```csharp
[Fact]
public async Task AdminTool_WithUserToken_ReturnsUnauthorized()
{
    var client = factory.CreateClient();
    var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/rpc")
    {
        Content = JsonContent.Create(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "delete_user" }
        })
    };
    httpRequest.Headers.Authorization = 
        new AuthenticationHeaderValue("Bearer", "user-token");
    
    var response = await client.SendAsync(httpRequest);
    var content = await response.Content.ReadAsStringAsync();
    
    Assert.Contains("Insufficient permissions", content);
}
```

## See Also

- [Lifecycle Hooks](/features/lifecycle-hooks/) - Hook infrastructure
- [Examples: Authorization Server](/examples/authorization/) - Complete working example
- [API Reference: Lifecycle Hooks](/api/lifecycle-hooks/) - Complete API documentation
