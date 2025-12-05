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

### Example 7: Early Validation (Using StreamMessageMeta)

**Pattern:** Access parameters via `connector.StreamMessage.Meta`

```csharp
[McpTool("example_validation",
    Description = "Validates file path before streaming",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":{
            ""path"":{""type"":""string"",""description"":""File path"",""minLength"":1}
        },
        ""required"":[""path""]
    }")]
public static async Task ValidationTool(ToolConnector connector)
{
    // Extract StreamMessageMeta from connector
    if (!StreamMessageMeta.TryGetFromObject(connector.StreamMessage?.Meta, out var streamMeta) || streamMeta == null)
    {
        throw new ToolInvalidParamsException("Invalid stream metadata");
    }

    // Use Name property from meta (client can send file path here)
    var filePath = streamMeta.Name;
    
    if (string.IsNullOrEmpty(filePath))
    {
        throw new ToolInvalidParamsException("File path is required");
    }

    var meta = new StreamMessageMeta(
        Method: "result.file",
        Binary: true,
        Name: Path.GetFileName(filePath),
        Mime: "application/octet-stream");

    using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);
    
    try
    {
        if (!File.Exists(filePath))
        {
            await handle.FailAsync(new JsonRpcError(404, "File not found", new { path = filePath }));
            return;
        }

        using var fileStream = File.OpenRead(filePath);
        await fileStream.CopyToAsync(handle);
        await handle.CompleteAsync(new { 
            fileName = Path.GetFileName(filePath),
            fileSize = new FileInfo(filePath).Length,
            success = true 
        });
    }
    catch (Exception ex)
    {
        await handle.FailAsync(new JsonRpcError(-32603, "Error reading file", new { detail = ex.Message }));
    }
}
```

**How client sends file path:**
```javascript
// Client sends StreamMessage with path in Name
ws.send(JSON.stringify({
  type: "start",
  id: "stream-123",
  meta: {
    method: "example_validation",
    binary: true,
    name: "C:\\data\\myfile.bin"  // ← File path here!
  }
}));
```

**Key points:**
- ✅ `StreamMessageMeta.Name` can be used for file paths
- ✅ Access via `StreamMessageMeta.TryGetFromObject(connector.StreamMessage?.Meta, out var meta)`
- ✅ Other metadata available: `Mime`, `TotalSize`, `Compression`, etc.
- ✅ For complex parameters, use `InputSchema` validation instead

---

## Integration with Existing Stream APIs

### Example 8: IAsyncEnumerable Integration

Integrate with IAsyncEnumerable for streaming:

```csharp
[McpTool("example_asyncenumerable")]
public static async Task AsyncEnumerableTool(ToolConnector connector)
{
    var meta = new StreamMessageMeta(
        Method: "result.data",
        Binary: true);

    using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);

    // Example: Stream returned as IAsyncEnumerable
    await foreach (var chunk in GetChunksAsync())
    {
        await handle.WriteAsync(chunk);
    }

    await handle.CompleteAsync(new { status = "completed" });
}

// Simulated async stream method
private static async IAsyncEnumerable<byte[]> GetChunksAsync()
{
    for (int i = 0; i < 10; i++)
    {
        // Simulate delay
        await Task.Delay(500);
        yield return new byte[1024]; // 1KB chunk
    }
}
```

---

## Common Patterns

### Pattern 1: Basic Streaming

Basic streaming setup:

```csharp
[McpTool("example_basic_stream")]
public static async Task BasicStreamTool(ToolConnector connector)
{
    // Example: Basic echo tool
    
    var meta = new StreamMessageMeta(
        Method: "result.data",
        Binary: true);

    using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);

    // Echo back received data
    await connector.StreamMessage.Content.CopyToAsync(handle);
    await handle.CompleteAsync(new { status = "echoed" });
}
```

### Pattern 2: Chunked Transfer

Chunked transfer example:

```csharp
[McpTool("example_chunked_transfer")]
public static async Task ChunkedTransferTool(ToolConnector connector)
{
    var meta = new StreamMessageMeta(
        Method: "result.data",
        Binary: true);

    using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);

    // Send data in chunks
    for (int i = 0; i < 10; i++)
    {
        var chunk = Encoding.UTF8.GetBytes($"Chunk {i}\n");
        await handle.WriteAsync(chunk);
        await Task.Delay(100); // Simulate delay
    }

    await handle.CompleteAsync(new { status = "chunked" });
}
```

### Pattern 3: Large File Upload

Handling large file uploads:

```csharp
[McpTool("example_large_file")]
public static async Task LargeFileUploadTool(ToolConnector connector)
{
    var filePath = @"C:\data\largefile.bin";
    var fileInfo = new FileInfo(filePath);

    var meta = new StreamMessageMeta(
        Method: "result.file",
        Binary: true,
        Name: fileInfo.Name,
        Mime: "application/octet-stream",
        TotalSize: fileInfo.Length);

    using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);
    using var fileStream = File.OpenRead(filePath);

    // Upload in chunks
    var buffer = new byte[8192]; // 8KB chunks
    int bytesRead;
    while ((bytesRead = await fileStream.ReadAsync(buffer)) > 0)
    {
        await handle.WriteAsync(buffer.AsMemory(0, bytesRead));
    }

    await handle.CompleteAsync(new { status = "uploaded" });
}
```

### Pattern 4: Cancellation Support

Respect cancellation tokens:

```csharp
[McpTool("example_cancellation")]
public static async Task CancellableTool(ToolConnector connector)
{
    // Note: CancellationToken parameter is NOT supported for ToolConnector tools
    // Use connector.StreamMessage to check for client disconnect instead
    
    var meta = new StreamMessageMeta(Method: "result.data", Binary: true);
    using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);

    try
    {
        // Check if client is still connected via WebSocket state
        // or implement timeout logic
        
        int chunks = 0;
        const int maxChunks = 100;
        
        while (chunks < maxChunks)
        {
            // For long-running operations, check if we should continue
            // (In practice, ToolConnector manages this internally via WebSocket state)
            
            var chunk = await GetNextChunkAsync();
            await handle.WriteAsync(chunk);
            chunks++;
            
            // Optional: Add delay between chunks
            await Task.Delay(100);
        }

        await handle.CompleteAsync(new { totalChunks = chunks });
    }
    catch (WebSocketException)
    {
        // Client disconnected - cleanup and exit
        return;
    }
    catch (Exception ex)
    {
        await handle.FailAsync(new JsonRpcError(-32603, "Error", new { detail = ex.Message }));
    }
}

// Helper method (example)
private static async Task<byte[]> GetNextChunkAsync()
{
    await Task.Delay(10);
    return new byte[1024];
}
```

**Important Notes:**
- `CancellationToken` is **NOT** automatically injected for `ToolConnector` tools
- WebSocket state is managed by `ToolConnector` internally
- For cancellation, catch `WebSocketException` when client disconnects
- For timeouts, implement custom timeout logic or use `Task.WhenAny`

---

## 🎓 Best Practices

1. **Always dispose handles** - Use `using` statement
2. **Set appropriate chunk sizes** - Balance latency vs throughput
3. **Include metadata** - Name, Mime, TotalSize help clients
4. **Use summary effectively** - Include useful statistics in `CompleteAsync()`
5. **Handle errors gracefully** - Use `FailAsync()` instead of throwing
6. **Respect cancellation** - Catch `WebSocketException` for client disconnect
7. **Validate early** - Use JSON Schema in `InputSchema` for parameter validation
8. **Log appropriately** - Log start/complete/errors for debugging
9. **Follow MCP naming rules** - Use underscores: `example_tool_name` (not dots!)

---

## 🔧 Parameter Resolution Rules

### ToolConnector Tools (Streaming)

**Signature:** `public static async Task MyTool(ToolConnector connector, ...)`

**Parameter resolution:**
1. **First parameter MUST be `ToolConnector`** - Automatically injected
2. **Additional parameters** - Resolved from Dependency Injection (DI)
3. **Special parameters NOT supported:**
   - ❌ `JsonRpcMessage request` - Not available (use JSON Schema validation)
   - ❌ `CancellationToken ct` - Not automatically injected (use WebSocket state)
   - ❌ Request parameters - Not directly accessible (passed via `StreamMessage.Meta`)

**Valid examples:**
```csharp
// ✅ Just ToolConnector
public static async Task MyTool(ToolConnector connector)

// ✅ ToolConnector + DI service
public static async Task MyTool(ToolConnector connector, ILogger logger)

// ✅ ToolConnector + multiple DI services
public static async Task MyTool(
    ToolConnector connector,
    IMyService service,
    ILogger<MyTool> logger)
```

**Invalid examples:**
```csharp
// ❌ JsonRpcMessage not supported with ToolConnector
public static async Task MyTool(ToolConnector connector, JsonRpcMessage request)

// ❌ CancellationToken not automatically injected
public static async Task MyTool(ToolConnector connector, CancellationToken ct)

// ❌ Custom parameters not supported (use DI or StreamMessage.Meta)
public static async Task MyTool(ToolConnector connector, string filePath)
```

### Non-Streaming Tools (JSON-RPC)

**Signature:** `public async Task<JsonRpcMessage> MyTool(JsonRpcMessage request, ...)`

**Parameter resolution:**
1. **First parameter MUST be `JsonRpcMessage`** - The request
2. **Additional parameters** - Resolved from Dependency Injection (DI)

**Valid examples:**
```csharp
// ✅ Just JsonRpcMessage
public async Task<JsonRpcMessage> MyTool(JsonRpcMessage request)

// ✅ JsonRpcMessage + DI service
public async Task<JsonRpcMessage> MyTool(JsonRpcMessage request, ILogger logger)

// ✅ JsonRpcMessage + multiple DI services
public async Task<JsonRpcMessage> MyTool(
    JsonRpcMessage request,
    IMyService service,
    ILogger<MyTool> logger)
```

---

## 🚨 Common Mistakes

### Mistake 1: Mixing ToolConnector with JsonRpcMessage
```csharp
// ❌ WRONG - This will fail at runtime!
[McpTool("bad_example")]
public static async Task BadExample(ToolConnector connector, JsonRpcMessage request)
{
    // DI will try to resolve JsonRpcMessage from container (fails!)
}
```

**Fix:** Choose one or the other
```csharp
// ✅ CORRECT - Streaming tool
[McpTool("good_streaming")]
public static async Task GoodStreaming(ToolConnector connector)

// ✅ CORRECT - Regular tool with request access
[McpTool("good_regular")]
public async Task<JsonRpcMessage> GoodRegular(JsonRpcMessage request)
```

### Mistake 2: Expecting CancellationToken
```csharp
// ❌ WRONG - CancellationToken not automatically injected!
[McpTool("bad_cancellation")]
public static async Task BadCancellation(ToolConnector connector, CancellationToken ct)
{
    // DI will try to resolve CancellationToken (may fail!)
}
```

**Fix:** Use WebSocket exception handling
```csharp
// ✅ CORRECT - Detect cancellation via WebSocket
[McpTool("good_cancellation")]
public static async Task GoodCancellation(ToolConnector connector)
{
    try
    {
        // ... streaming logic ...
    }
    catch (WebSocketException)
    {
        // Client disconnected
        return;
    }
}
```

### Mistake 3: Trying to access request parameters in streaming tool
```csharp
// ❌ WRONG - Cannot access request params directly!
[McpTool("bad_params")]
public static async Task BadParams(ToolConnector connector)
{
    // How do I get the file path from request? ← Can't!
}
```

**Fix:** Use JSON Schema validation or StreamMessage.Meta
```csharp
// ✅ CORRECT - Use JSON Schema for validation
[McpTool("good_params",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":{
            ""path"":{""type"":""string"",""minLength"":1}
        },
        ""required"":[""path""]
    }")]
public static async Task GoodParams(ToolConnector connector)
{
    // Parameters validated before tool invocation
    // Access via connector.StreamMessage.Meta if needed
}
```

---
