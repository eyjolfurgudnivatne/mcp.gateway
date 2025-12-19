namespace AuthorizationMcpServer.Tools;

using AuthorizationMcpServer.Authorization;
using Mcp.Gateway.Tools;

/// <summary>
/// Example tools demonstrating role-based authorization (v1.8.0).
/// </summary>
public class AdminTools
{
    [McpTool("delete_user", Description = "Delete a user (Admin only)")]
    [RequireRole("Admin")]
    public JsonRpcMessage DeleteUser(JsonRpcMessage request)
    {
        var userId = request.GetParams().GetProperty("userId").GetString();
        
        // Simulate deletion
        return ToolResponse.Success(request.Id, new
        {
            deleted = true,
            userId,
            message = $"User '{userId}' has been deleted"
        });
    }
    
    [McpTool("create_user", Description = "Create a new user (Admin or Manager)")]
    [RequireRole("Admin")]
    [RequireRole("Manager")]  // Admin OR Manager can call this
    public JsonRpcMessage CreateUser(JsonRpcMessage request)
    {
        var username = request.GetParams().GetProperty("username").GetString();
        
        return ToolResponse.Success(request.Id, new
        {
            created = true,
            username,
            message = $"User '{username}' has been created"
        });
    }
    
    [McpTool("get_user_list", Description = "Get list of users (Admin, Manager, or User)")]
    [RequireRole("Admin")]
    [RequireRole("Manager")]
    [RequireRole("User")]
    public JsonRpcMessage GetUserList(JsonRpcMessage request)
    {
        return ToolResponse.Success(request.Id, new
        {
            users = new[]
            {
                new { id = "1", name = "Alice" },
                new { id = "2", name = "Bob" },
                new { id = "3", name = "Charlie" }
            }
        });
    }
    
    [McpTool("get_public_info", Description = "Get public information (no auth required)")]
    [AllowAnonymous]
    public JsonRpcMessage GetPublicInfo(JsonRpcMessage request)
    {
        return ToolResponse.Success(request.Id, new
        {
            info = "This is public information accessible to everyone",
            timestamp = DateTime.UtcNow
        });
    }
    
    [McpTool("update_settings", Description = "Update system settings (Admin only)")]
    [RequireRole("Admin")]
    public JsonRpcMessage UpdateSettings(JsonRpcMessage request)
    {
        var settingName = request.GetParams().GetProperty("name").GetString();
        var settingValue = request.GetParams().GetProperty("value").GetString();
        
        return ToolResponse.Success(request.Id, new
        {
            updated = true,
            setting = settingName,
            value = settingValue
        });
    }
}
