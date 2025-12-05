namespace Mcp.Gateway.Benchmarks;

using BenchmarkDotNet.Attributes;
using System.Buffers;

/// <summary>
/// Benchmarks for WebSocket buffer allocation patterns
/// Measures GC pressure reduction from ArrayPool usage
/// </summary>
[MemoryDiagnoser]  // Shows Gen0/Gen1/Gen2 collections + allocations
[SimpleJob(warmupCount: 3, iterationCount: 100)]  // More iterations to see GC
public class WebSocketBufferBenchmarks
{
    private const int BufferSize = 64 * 1024;  // 64KB - same as ToolConnector
    private const int Iterations = 100;        // Simulate 100 messages
    
    /// <summary>
    /// OLD WAY: Allocate new buffer every time
    /// This puts pressure on GC - 64KB allocation per call
    /// </summary>
    [Benchmark(Baseline = true)]
    public int AllocateNewBuffer()
    {
        int totalBytes = 0;
        
        for (int i = 0; i < Iterations; i++)
        {
            var buffer = new byte[BufferSize];  // 64KB allocation!
            buffer[0] = (byte)i;                // Simulate usage
            totalBytes += buffer.Length;
        }
        
        return totalBytes;
    }
    
    /// <summary>
    /// NEW WAY: Rent from ArrayPool
    /// Reuses buffers - minimal GC pressure
    /// </summary>
    [Benchmark]
    public int UseArrayPool()
    {
        int totalBytes = 0;
        var pool = ArrayPool<byte>.Shared;
        
        for (int i = 0; i < Iterations; i++)
        {
            var buffer = pool.Rent(BufferSize);  // Rent from pool
            try
            {
                buffer[0] = (byte)i;              // Simulate usage
                totalBytes += BufferSize;
            }
            finally
            {
                pool.Return(buffer, clearArray: false);  // Return to pool
            }
        }
        
        return totalBytes;
    }
    
    /// <summary>
    /// Simulates WebSocket receive pattern (more realistic)
    /// Shows real-world benefit of ArrayPool
    /// </summary>
    [Benchmark]
    public async Task<int> SimulateWebSocketReceive_NewBuffer()
    {
        int totalReceived = 0;
        
        for (int i = 0; i < Iterations; i++)
        {
            var buffer = new byte[BufferSize];  // Allocate
            
            // Simulate WebSocket receive
            await Task.Delay(1);  // Simulate network delay
            buffer[0] = (byte)i;
            
            totalReceived += buffer.Length;
        }
        
        return totalReceived;
    }
    
    /// <summary>
    /// Simulates WebSocket receive with ArrayPool (OPTIMIZED)
    /// </summary>
    [Benchmark]
    public async Task<int> SimulateWebSocketReceive_ArrayPool()
    {
        int totalReceived = 0;
        var pool = ArrayPool<byte>.Shared;
        
        for (int i = 0; i < Iterations; i++)
        {
            var buffer = pool.Rent(BufferSize);  // Rent
            try
            {
                // Simulate WebSocket receive
                await Task.Delay(1);  // Simulate network delay
                buffer[0] = (byte)i;
                
                totalReceived += BufferSize;
            }
            finally
            {
                pool.Return(buffer, clearArray: false);  // Return
            }
        }
        
        return totalReceived;
    }
}
