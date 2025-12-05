# ToolConnector Usage Examples

**Version:** v2 (Phase 2 - Write Only)  
**Date:** 4. desember 2025

---

## 📚 Table of Contents

1. [Binary Streaming (Server → Client)](#binary-streaming-server--client)
2. [Text Streaming (Server → Client)](#text-streaming-server--client)
3. [Error Handling](#error-handling)
4. [Integration with Existing Stream APIs](#integration-with-existing-stream-apis)
5. [Common Patterns](#common-patterns)

---

## Binary Streaming (Server → Client)

### Example 1: Simple Binary Upload

Stream static binary data to client:

```csharp
[McpTool("example_binary_simple")]
public static async Task SimpleBinaryTool(ToolConnector connector)
{
    var meta = new StreamMessageMeta(
        Method: "result.data",
        Binary: true,
        Mime: "application/octet-stream");

    using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);

    var data = Encoding.UTF8.GetBytes("Hello, World!");
    await handle.WriteAsync(data);
    
    await handle.CompleteAsync(new { totalBytes = data.Length });
}
```

### Example 2: File Upload

Stream file content to client:

```csharp
[McpTool("example_binary_file")]
public static async Task FileUploadTool(ToolConnector connector)
{
    var filePath = @"C:\data\myfile.bin";
    var fileInfo = new FileInfo(filePath);

    var meta = new StreamMessageMeta(
        Method: "result.file",
        Binary: true,
        Name: fileInfo.Name,
        Mime: "application/octet-stream",
        TotalSize: fileInfo.Length);

    using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);
    using var fileStream = File.OpenRead(filePath);

    var buffer = new byte[64 * 1024]; // 64KB chunks
    int bytesRead;

    while ((bytesRead = await fileStream.ReadAsync(buffer)) > 0)
    {
        await handle.WriteAsync(buffer.AsMemory(0, bytesRead));
    }

    await handle.CompleteAsync(new 
    { 
        fileName = fileInfo.Name,
        totalBytes = fileInfo.Length,
        chunks = (int)Math.Ceiling((double)fileInfo.Length / buffer.Length)
    });
}
```

### Example 3: Generated Data Stream

Stream dynamically generated binary data:

```csharp
[McpTool("example_binary_generated")]
public static async Task GeneratedDataTool(ToolConnector connector)
{
    var meta = new StreamMessageMeta(
        Method: "result.generated",
        Binary: true,
        Mime: "application/octet-stream");

    using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);

    // Generate random data
    var random = new Random();
    var totalBytes = 0L;

    for (int i = 0; i < 100; i++)
    {
        var chunk = new byte[1024];
        random.NextBytes(chunk);
        
        await handle.WriteAsync(chunk);
        totalBytes += chunk.Length;
    }

    await handle.CompleteAsync(new { totalBytes, chunks = 100 });
}
```

---

## Text Streaming (Server → Client)

### Example 4: JSON Line Stream

Stream JSON objects line-by-line:

```csharp
[McpTool("example_text_jsonlines")]
public static async Task JsonLinesTool(ToolConnector connector)
{
    var meta = new StreamMessageMeta(
        Method: "result.logs",
        Binary: false,
        Encoding: "utf-8");

    var handle = (ToolConnector.TextStreamHandle)connector.OpenWrite(meta);

    var logs = new[]
    {
        new { timestamp = DateTime.UtcNow, level = "INFO", message = "Starting process" },
        new { timestamp = DateTime.UtcNow, level = "INFO", message = "Processing data" },
        new { timestamp = DateTime.UtcNow, level = "WARN", message = "Low memory" },
        new { timestamp = DateTime.UtcNow, level = "INFO", message = "Process complete" }
    };

    for (int i = 0; i < logs.Length; i++)
    {
        await handle.WriteChunkAsync(logs[i]);
    }

    await handle.CompleteAsync(new { totalLines = logs.Length });
}
```

### Example 5: Streaming Database Results

Stream database query results:

```csharp
[McpTool("example_text_database")]
public static async Task DatabaseStreamTool(ToolConnector connector)
{
    var meta = new StreamMessageMeta(
        Method: "result.query",
        Binary: false);

    var handle = (ToolConnector.TextStreamHandle)connector.OpenWrite(meta);

    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    using var command = new SqlCommand("SELECT * FROM Users", connection);
    using var reader = await command.ExecuteReaderAsync();

    var count = 0;
    while (await reader.ReadAsync())
    {
        var row = new
        {
            id = reader.GetInt32(0),
            name = reader.GetString(1),
            email = reader.GetString(2)
        };

        await handle.WriteChunkAsync(row);
        count++;
    }

    await handle.CompleteAsync(new { totalRows = count });
}
```

---

## Error Handling

### Example 6: Graceful Error Handling

Handle errors and send error message:

```csharp
[McpTool("example_error_handling")]
public static async Task ErrorHandlingTool(ToolConnector connector)
{
    var meta = new StreamMessageMeta(
        Method: "result.data",
        Binary: true);

    using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);

    try
    {
        // Simulate error condition
        var filePath = @"C:\nonexistent\file.bin";
        
        if (!File.Exists(filePath))
        {
            await handle.FailAsync(new JsonRpcError(
                code: 404,
                message: "File not found",
                data: new { path = filePath }));
            return;
        }

        // Normal processing...
        using var fileStream = File.OpenRead(filePath);
        await fileStream.CopyToAsync(handle);
        await handle.CompleteAsync(new { status = "success" });
    }
    catch (Exception ex)
    {
        await handle.FailAsync(new JsonRpcError(
            code: -32603,
            message: "Internal error",
            data: new { detail = ex.Message }));
    }
}
```

### Example 7: Validation Before Streaming

Validate input before starting stream:

```csharp
[McpTool("example_validation")]
public static async Task ValidationTool(ToolConnector connector, JsonRpcMessage request)
{
    // Validate request params before opening stream
    if (request.Params is null)
    {
        throw new ToolInvalidParamsException("Missing params");
    }

    var filePath = request.Params.GetProperty("path").GetString();
    
    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
    {
        throw new ToolInvalidParamsException($"Invalid file path: {filePath}");
    }

    // Validation passed, start streaming
    var meta = new StreamMessageMeta(
        Method: "result.file",
        Binary: true,
        Name: Path.GetFileName(filePath));

    using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);
    using var fileStream = File.OpenRead(filePath);
    
    await fileStream.CopyToAsync(handle);
    await handle.CompleteAsync(new { success = true });
}
```

---

## Integration with Existing Stream APIs

### Example 8: Copy from Stream

Use `Stream.CopyToAsync()` for efficient streaming:

```csharp
[McpTool("example_stream_copy")]
public static async Task StreamCopyTool(ToolConnector connector)
{
    var meta = new StreamMessageMeta(
        Method: "result.data",
        Binary: true);

    using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);

    // BinaryStreamHandle implements Stream, so CopyToAsync works!
    using var sourceStream = GetDataStream(); // Your data source
    await sourceStream.CopyToAsync(handle);
    
    await handle.CompleteAsync(new { status = "copied" });
}
```

### Example 9: Compression

Stream compressed data:

```csharp
[McpTool("example_compression")]
public static async Task CompressionTool(ToolConnector connector)
{
    var meta = new StreamMessageMeta(
        Method: "result.compressed",
        Binary: true,
        Compression: "gzip",
        Mime: "application/gzip");

    using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);
    using var gzipStream = new GZipStream(handle, CompressionLevel.Optimal, leaveOpen: true);

    var data = Encoding.UTF8.GetBytes("Large text data to compress...");
    await gzipStream.WriteAsync(data);
    await gzipStream.FlushAsync();

    await handle.CompleteAsync(new { compressed = true });
}
```

---

## Common Patterns

### Pattern 1: Progress Reporting via Summary

Report progress in the done message:

```csharp
var totalBytes = 0L;
var chunks = 0;

while (hasMoreData)
{
    await handle.WriteAsync(chunk);
    totalBytes += chunk.Length;
    chunks++;
}

await handle.CompleteAsync(new 
{ 
    totalBytes, 
    chunks,
    duration = stopwatch.Elapsed.TotalSeconds
});
```

### Pattern 2: Metadata-rich Streams

Include metadata in `StreamMessageMeta`:

```csharp
var meta = new StreamMessageMeta(
    Method: "result.image",
    Binary: true,
    Name: "photo.jpg",
    Mime: "image/jpeg",
    TotalSize: 1024 * 1024, // 1MB
    CorrelationId: Guid.NewGuid().ToString());
```

### Pattern 3: Chunking Strategy

Use appropriate chunk sizes:

```csharp
// Small chunks for low-latency (e.g., real-time logs)
var buffer = new byte[1024]; // 1KB

// Large chunks for throughput (e.g., file transfer)
var buffer = new byte[64 * 1024]; // 64KB

// Adaptive chunking based on network conditions
var buffer = new byte[GetOptimalChunkSize()];
```

### Pattern 4: Cancellation Support

Respect cancellation tokens:

```csharp
[McpTool("example_cancellation")]
public static async Task CancellableTool(ToolConnector connector, CancellationToken ct)
{
    var meta = new StreamMessageMeta(Method: "result.data", Binary: true);
    using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);

    try
    {
        while (!ct.IsCancellationRequested)
        {
            var chunk = await GetNextChunkAsync(ct);
            await handle.WriteAsync(chunk, ct);
        }

        await handle.CompleteAsync(new { cancelled = true }, ct);
    }
    catch (OperationCanceledException)
    {
        await handle.FailAsync(new JsonRpcError(499, "Cancelled", null));
    }
}
```

---

## 🎓 Best Practices

1. **Always dispose handles** - Use `using` statement
2. **Set appropriate chunk sizes** - Balance latency vs throughput
3. **Include metadata** - Name, Mime, TotalSize help clients
4. **Use summary effectively** - Include useful statistics in `CompleteAsync()`
5. **Handle errors gracefully** - Use `FailAsync()` instead of throwing
6. **Respect cancellation** - Check `CancellationToken` in loops
7. **Validate early** - Check inputs before opening stream
8. **Log appropriately** - Log start/complete/errors for debugging
9. **Follow MCP naming rules** - Use underscores: `example_tool_name` (not dots!)

---

## 📚 See Also

- [Phase 2 Implementation](phase-2-implementation.md)
- [Architecture Decision Record 001](adr-001-websocket-ownership.md)
- [ToolConnector API Reference](../api/ToolConnector.md) (TODO)
