namespace MetricsMcpServer.Tools;

using Mcp.Gateway.Tools;

/// <summary>
/// Simple calculator tools for testing metrics (v1.8.0).
/// </summary>
public class CalculatorTools
{
    [McpTool("add", Description = "Add two numbers")]
    public JsonRpcMessage Add(JsonRpcMessage request)
    {
        var args = request.GetParams();
        var a = args.GetProperty("a").GetDouble();
        var b = args.GetProperty("b").GetDouble();
        
        return ToolResponse.Success(request.Id, new { result = a + b });
    }
    
    [McpTool("divide", Description = "Divide two numbers (throws on divide by zero)")]
    public JsonRpcMessage Divide(JsonRpcMessage request)
    {
        var args = request.GetParams();
        var a = args.GetProperty("a").GetDouble();
        var b = args.GetProperty("b").GetDouble();
        
        if (b == 0)
        {
            throw new ToolInvalidParamsException("Cannot divide by zero", "divide");
        }
        
        return ToolResponse.Success(request.Id, new { result = a / b });
    }
    
    [McpTool("slow_operation", Description = "Simulates a slow operation (for testing duration metrics)")]
    public async Task<JsonRpcMessage> SlowOperation(JsonRpcMessage request)
    {
        var args = request.GetParams();
        var delayMs = args.TryGetProperty("delayMs", out var delay) 
            ? delay.GetInt32() 
            : 100;
        
        await Task.Delay(delayMs);
        
        return ToolResponse.Success(request.Id, new { message = $"Operation completed after {delayMs}ms" });
    }
}
