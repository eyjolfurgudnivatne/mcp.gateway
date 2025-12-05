# 🔬 MCP Gateway Benchmarks

Performance benchmarks for MCP Gateway using BenchmarkDotNet.

## 🚀 Quick Start

```powershell
# Run all benchmarks
dotnet run -c Release

# Run specific benchmark
dotnet run -c Release --filter *JsonSerialization*
```

## 📊 Benchmark Suites

### 1. JsonSerializationBenchmarks
Measures JSON serialization/deserialization performance:
- `DeserializeRequest` - Baseline: Deserialize JSON-RPC request
- `SerializeRequest` - Serialize to JSON string
- `SerializeToUtf8Bytes` - Serialize directly to UTF8 bytes
- `DeserializeAsync` - Async deserialization from stream

**What we measure:**
- Execution time (Mean, Median, StdDev)
- Memory allocations (Allocated bytes, Gen0/Gen1/Gen2 collections)

**Optimization targets:**
- JSON Source Generators (v1.1)
- Stream-based serialization
- Reduced allocations

---

### 2. ToolInvocationBenchmarks
Measures complete tool invocation path:
- `InvokeSimpleTool` - Baseline: Ping tool (no parameters)
- `InvokeToolWithParameters` - Add tool (with parameters)
- `ToolsListInvocation` - MCP protocol tools/list

**What we measure:**
- End-to-end request → response latency
- Memory overhead per invocation
- Parameter parsing cost

**Optimization targets:**
- ValueTask for sync paths
- Cached parameter parsing
- Reduced allocations

---

### 3. ToolDiscoveryBenchmarks
Measures tool discovery and caching:
- `DiscoverAllTools` - Baseline: Full assembly scan
- `FindToolByName` - Single tool lookup
- `CountTools` - Tool enumeration

**What we measure:**
- Discovery time (cold start)
- Lookup performance (cached)
- Memory footprint of metadata

**Optimization targets:**
- Metadata caching (v1.1)
- O(n) → O(1) lookups
- Lazy initialization

---

## 📈 Reading Results

BenchmarkDotNet produces detailed reports:

```
|              Method |     Mean |   Error |  StdDev | Allocated |
|-------------------- |---------:|--------:|--------:|----------:|
| DeserializeRequest  | 1.234 μs | 0.045 μs | 0.123 μs |   1.2 KB |
| SerializeRequest    | 2.345 μs | 0.067 μs | 0.234 μs |   2.4 KB |
```

**Key metrics:**
- **Mean**: Average execution time
- **Error**: Standard error of the mean
- **StdDev**: Standard deviation
- **Allocated**: Total memory allocated

**What's good:**
- μs (microseconds) = 0.000001 seconds
- Lower is better for time
- Lower is better for allocations

---

## 🎯 Baseline v1.0

Run benchmarks before optimizations to establish baseline:

```powershell
dotnet run -c Release > baseline-v1.0.txt
```

This creates a baseline for comparing v1.1 improvements.

---

## 🔧 Adding New Benchmarks

1. Create new class with `[MemoryDiagnoser]` attribute
2. Add `[Benchmark]` methods
3. Mark one method as `[Benchmark(Baseline = true)]`

**Example:**
```csharp
[MemoryDiagnoser]
public class MyBenchmarks
{
    [Benchmark(Baseline = true)]
    public void Original() { /* ... */ }
    
    [Benchmark]
    public void Optimized() { /* ... */ }
}
```

---

## 📚 Resources

- **BenchmarkDotNet Docs**: https://benchmarkdotnet.org/
- **Performance Best Practices**: https://learn.microsoft.com/en-us/dotnet/core/performance/

---

**Last Updated:** 4. desember 2025
