namespace Mcp.Gateway.Benchmarks;

using BenchmarkDotNet.Attributes;
using Mcp.Gateway.Tools;
using Microsoft.Extensions.DependencyInjection;


/// <summary>
/// Benchmarks for tool invocation performance
/// This measures the complete path for tool registration and discovery
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ToolInvocationBenchmarks
{
    private ServiceProvider _serviceProvider = null!;
    private ToolService _toolService = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ToolService>();
        
        // Add test tools
        services.AddSingleton<TestTools>();
        
        _serviceProvider = services.BuildServiceProvider();
        _toolService = _serviceProvider.GetRequiredService<ToolService>();
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }
    
    [Benchmark(Baseline = true)]
    public JsonRpcMessage InvokeSimpleTool()
    {
        // Create and invoke a simple tool
        var tools = new TestTools();
        var request = JsonRpcMessage.CreateRequest("test_ping", Id: 1);
        return tools.Ping(request).Result;
    }
    
    [Benchmark]
    public JsonRpcMessage InvokeToolWithParameters()
    {
        // Create and invoke tool with parameters
        var tools = new TestTools();
        var request = JsonRpcMessage.CreateRequest(
            Method: "test_add",
            Id: 2,
            Params: new { a = 5, b = 3 }
        );
        return tools.Add(request).Result;
    }
}

/// <summary>
/// Simple test tools for benchmarking
/// </summary>
public class TestTools
{
    [McpTool("test_ping", Description = "Simple ping tool")]
    public Task<JsonRpcMessage> Ping(JsonRpcMessage request)
    {
        return Task.FromResult(ToolResponse.Success(request.Id, new { message = "pong" }));
    }
    
    [McpTool("test_add", 
        Description = "Add two numbers",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":
            {
                ""a"":{""type"":""number""},
                ""b"":{""type"":""number""}
            }
        }")]
    public Task<JsonRpcMessage> Add(JsonRpcMessage request)
    {
        var a = request.GetParams().GetProperty("a").GetInt32();
        var b = request.GetParams().GetProperty("b").GetInt32();
        return Task.FromResult(ToolResponse.Success(request.Id, new { result = a + b }));
    }
}
