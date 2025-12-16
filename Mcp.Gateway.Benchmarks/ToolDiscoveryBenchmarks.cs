namespace Mcp.Gateway.Benchmarks;

using BenchmarkDotNet.Attributes;
using Mcp.Gateway.Tools;
using Microsoft.Extensions.DependencyInjection;


/// <summary>
/// Benchmarks for tool discovery performance
/// This measures the overhead of scanning assemblies and caching tools
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ToolDiscoveryBenchmarks
{
    private ServiceProvider _serviceProvider = null!;
    private ToolService _toolService = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestTools>();
        _serviceProvider = services.BuildServiceProvider();
        
        _toolService = new ToolService(_serviceProvider);
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }
    
    [Benchmark(Baseline = true)]
    public List<ToolService.FunctionDefinition> DiscoverAllTools()
    {
        // This triggers tool scan on first call (lazy initialization)
        return _toolService.GetAllFunctionDefinitions(ToolService.FunctionTypeEnum.Tool).ToList();
    }
    
    [Benchmark]
    public int CountTools()
    {
        // Count total tools (uses cached results after first call)
        return _toolService.GetAllFunctionDefinitions(ToolService.FunctionTypeEnum.Tool).Count();
    }
}
