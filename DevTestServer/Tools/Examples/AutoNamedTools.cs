namespace DevTestServer.Tools.Examples;

using Mcp.Gateway.Tools;

/// <summary>
/// Example tools demonstrating auto-generated tool names.
/// </summary>
public class AutoNamedTools
{
    /// <summary>
    /// Example 1: Name auto-generated from method name
    /// Tool name: "ping" (generated from "Ping")
    /// </summary>
    [McpTool]  // Name will be auto-generated: "ping"
    public JsonRpcMessage Ping(JsonRpcMessage request)
    {
        return ToolResponse.Success(request.Id, new { message = "Pong" });
    }

    /// <summary>
    /// Example 2: Name auto-generated with snake_case conversion
    /// Tool name: "add_numbers_tool" (generated from "AddNumbersTool")
    /// </summary>
    [McpTool(Title = "Add Numbers", Description = "Adds two numbers")]
    public JsonRpcMessage AddNumbersTool(JsonRpcMessage request)
    {
        var a = request.GetParams().GetProperty("a").GetInt32();
        var b = request.GetParams().GetProperty("b").GetInt32();
        return ToolResponse.Success(request.Id, new { result = a + b });
    }

    /// <summary>
    /// Example 3: Explicit name still works (backward compatible)
    /// Tool name: "get_user" (explicitly specified)
    /// </summary>
    [McpTool("get_user", Description = "Gets user by ID")]
    public JsonRpcMessage GetUserById(JsonRpcMessage request)
    {
        var userId = request.GetParams().GetProperty("userId").GetInt32();
        return ToolResponse.Success(request.Id, new { userId, name = "John Doe" });
    }

    /// <summary>
    /// Example 4: Method already in snake_case
    /// Tool name: "echo_message" (method name is valid as-is)
    /// </summary>
    [McpTool]  // Name will be: "echo_message"
    public JsonRpcMessage echo_message(JsonRpcMessage request)
    {
        return ToolResponse.Success(request.Id, request.Params);
    }
}
