# AuthorizationMcpServer - Role-Based Authorization Example (v1.8.0)

This example demonstrates **role-based authorization** for MCP tools using:
- Custom `[RequireRole]` attribute for fine-grained control
- Lifecycle hooks (`AuthorizationHook`) for authorization enforcement
- ASP.NET Core middleware for authentication
- Simple token-based auth (demo - use JWT in production!)

## Features

- ✅ **Role-based authorization** - Tools can require specific roles
- ✅ **Multiple roles** - Use `[RequireRole]` multiple times for OR logic
- ✅ **Anonymous tools** - Use `[AllowAnonymous]` to skip auth
- ✅ **Lifecycle hook integration** - Leverages v1.8.0 hooks
- ✅ **Clear error messages** - JSON-RPC compliant auth errors
- ✅ **Production-ready pattern** - Easy to swap token validation

## Authorization Flow

```
1. Client → HTTP Request with "Authorization: Bearer <token>"
2. Middleware → Validates token, stores user info in HttpContext.Items
3. Tool Invocation → AuthorizationHook checks [RequireRole] attributes
4. Tool Execution → If authorized, tool runs normally
```

## Demo Tokens

For testing purposes (replace with JWT in production):

| Token | User ID | Roles |
|-------|---------|-------|
| `admin-token-123` | admin-user | Admin |
| `user-token-456` | regular-user | User |
| `manager-token-789` | manager-user | Manager, User |
| `public-token-000` | public-user | (none) |

## Example Tools

### Admin-only Tool

```csharp
[McpTool("delete_user", Description = "Delete a user (Admin only)")]
[RequireRole("Admin")]
public JsonRpcMessage DeleteUser(JsonRpcMessage request)
{
    var userId = request.GetParams().GetProperty("userId").GetString();
    // Delete user logic...
    return ToolResponse.Success(request.Id, new { deleted = true });
}
```

### Multi-role Tool (OR logic)

```csharp
[McpTool("create_user", Description = "Create a new user (Admin or Manager)")]
[RequireRole("Admin")]
[RequireRole("Manager")]  // Admin OR Manager can call this
public JsonRpcMessage CreateUser(JsonRpcMessage request)
{
    // Create user logic...
    return ToolResponse.Success(request.Id, new { created = true });
}
```

### Anonymous Tool

```csharp
[McpTool("get_public_info", Description = "Get public information (no auth required)")]
[AllowAnonymous]
public JsonRpcMessage GetPublicInfo(JsonRpcMessage request)
{
    return ToolResponse.Success(request.Id, new { info = "Public data" });
}
```

## Usage

### 1. Start the server

```bash
cd Examples/AuthorizationMcpServer
dotnet run
```

Server starts on http://localhost:5000

### 2. Call admin tool (succeeds)

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer admin-token-123" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "id": "1",
    "params": {
      "name": "delete_user",
      "arguments": {"userId": "user-123"}
    }
  }'
```

### 3. Call admin tool with user token (fails)

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer user-token-456" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "id": "1",
    "params": {
      "name": "delete_user",
      "arguments": {"userId": "user-123"}
    }
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "error": {
    "code": -32603,
    "message": "Internal error",
    "data": {
      "detail": "User does not have required role(s) to invoke 'delete_user'. Required: Admin. User has: User."
    }
  },
  "id": "1"
}
```

### 4. Call anonymous tool (always works with valid token)

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer public-token-000" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "id": "1",
    "params": {
      "name": "get_public_info",
      "arguments": {}
    }
  }'
```

## Authorization Errors

### Missing Token (401 Unauthorized)

```json
{
  "jsonrpc": "2.0",
  "error": {
    "code": -32000,
    "message": "Unauthorized",
    "data": {
      "detail": "Missing Authorization header",
      "hint": "Include 'Authorization: Bearer <token>' header"
    }
  },
  "id": null
}
```

### Invalid Token (403 Forbidden)

```json
{
  "jsonrpc": "2.0",
  "error": {
    "code": -32002,
    "message": "Forbidden",
    "data": {
      "detail": "Invalid or expired token",
      "hint": "Request a new token or check your credentials"
    }
  },
  "id": null
}
```

### Insufficient Permissions (403 via hook)

```json
{
  "jsonrpc": "2.0",
  "error": {
    "code": -32603,
    "message": "Internal error",
    "data": {
      "detail": "User does not have required role(s) to invoke 'delete_user'. Required: Admin. User has: User."
    }
  },
  "id": "1"
}
```

## Production Deployment

### Swap to JWT Authentication

Replace `SimpleTokenValidationService` with proper JWT validation:

```csharp
// In Program.cs
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://your-identity-server.com";
        options.Audience = "mcp-gateway-api";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

// Update middleware to use ASP.NET Core auth
app.UseAuthentication();
app.UseAuthorization();

// Update AuthorizationHook to read from ClaimsPrincipal
var userRoles = httpContext.User.FindAll(ClaimTypes.Role)
    .Select(c => c.Value)
    .ToList();
```

### Add Audit Logging

Extend `AuthorizationHook` to log all authorization decisions:

```csharp
public Task OnToolCompletedAsync(string toolName, JsonRpcMessage response, TimeSpan duration)
{
    var userId = _httpContextAccessor.HttpContext?.Items["UserId"] as string;
    _logger.LogInformation(
        "User '{UserId}' successfully invoked tool '{ToolName}'",
        userId, toolName);
    
    return Task.CompletedTask;
}

public Task OnToolFailedAsync(string toolName, Exception error, TimeSpan duration)
{
    if (error is UnauthorizedAccessException)
    {
        var userId = _httpContextAccessor.HttpContext?.Items["UserId"] as string;
        _logger.LogWarning(
            "User '{UserId}' was denied access to tool '{ToolName}': {Error}",
            userId, toolName, error.Message);
    }
    
    return Task.CompletedTask;
}
```

## Testing

Run the test suite:

```bash
cd Examples/AuthorizationMcpServerTests
dotnet test
```

**Test coverage:**
- ✅ Missing authorization header → 401
- ✅ Invalid token → 403
- ✅ Admin tool with admin token → Success
- ✅ Admin tool with user token → Unauthorized
- ✅ Multi-role tool with manager token → Success
- ✅ Anonymous tool with any valid token → Success
- ✅ Health check without auth → Success

## Architecture

```
┌─────────────────────────────────────────────┐
│           HTTP Request                       │
│   Authorization: Bearer <token>             │
└──────────────┬──────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────┐
│      Middleware (Program.cs)                │
│  - Validate token                            │
│  - Store user info in HttpContext.Items     │
│  - Return 401/403 if invalid                │
└──────────────┬──────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────┐
│      ToolInvoker                            │
│  - Invoke AuthorizationHook                 │
└──────────────┬──────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────┐
│   AuthorizationHook (Lifecycle Hook)        │
│  - Read [RequireRole] attributes            │
│  - Check user roles from HttpContext        │
│  - Throw UnauthorizedAccessException        │
└──────────────┬──────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────┐
│         Tool Method                         │
│  [RequireRole("Admin")]                     │
│  public JsonRpcMessage DeleteUser(...)      │
└─────────────────────────────────────────────┘
```

## See Also

- [Tool Lifecycle Hooks](../../docs/LifecycleHooks.md) - v1.8.0 feature
- [v1.8.0 Release Notes](../../.internal/notes/v1.8.0/v1.8.0-enhancements.md)
- [ASP.NET Core Authorization](https://learn.microsoft.com/aspnet/core/security/authorization/)
