namespace Mcp.Gateway.Benchmarks;

using BenchmarkDotNet.Attributes;
using Mcp.Gateway.Tools;
using System.Text.Json;


/// <summary>
/// Benchmarks for JSON serialization/deserialization performance
/// This measures the overhead of System.Text.Json operations
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class JsonSerializationBenchmarks
{
    private JsonRpcMessage _sampleRequest = null!;
    private string _sampleJson = null!;
    private JsonSerializerOptions _options = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _options = JsonOptions.Default;
        
        _sampleRequest = JsonRpcMessage.CreateRequest(
            Method: "tools/call",
            Id: 1,
            Params: new
            {
                name = "add_numbers",
                arguments = new { number1 = 5, number2 = 3 }
            }
        );
        
        _sampleJson = JsonSerializer.Serialize(_sampleRequest, _options);
    }
    
    [Benchmark(Baseline = true)]
    public JsonRpcMessage? DeserializeRequest()
    {
        return JsonSerializer.Deserialize<JsonRpcMessage>(_sampleJson, _options);
    }
    
    [Benchmark]
    public string SerializeRequest()
    {
        return JsonSerializer.Serialize(_sampleRequest, _options);
    }
    
    [Benchmark]
    public byte[] SerializeToUtf8Bytes()
    {
        return JsonSerializer.SerializeToUtf8Bytes(_sampleRequest, _options);
    }
    
    [Benchmark]
    public async Task<JsonRpcMessage?> DeserializeAsync()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_sampleJson));
        return await JsonSerializer.DeserializeAsync<JsonRpcMessage>(stream, _options);
    }
}
