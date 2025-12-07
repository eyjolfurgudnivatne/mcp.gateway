# 🔌 MCP Client Examples

**Using Mcp.Gateway.Tools for Client Development**

`Mcp.Gateway.Tools` provides all the models and helpers you need to build MCP clients. These examples show how to use the library for client-side communication.

---

## 📚 Table of Contents

1. [HTTP Client Examples](#http-client-examples)
2. [WebSocket Client Examples](#websocket-client-examples)
3. [Streaming Client Examples](#streaming-client-examples)
4. [Helper Methods Reference](#helper-methods-reference)

---

## HTTP Client Examples

### Example 1: Simple Tool Call (HTTP)

```csharp
using Mcp.Gateway.Tools;
using System.Net.Http.Json;
using System.Text.Json;

// Create HTTP client
using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

// 1. Create request using helper
var request = JsonRpcMessage.CreateRequest(
    Method: "tools/call",
    Id: 1,
    Params: new
    {
        name = "add_numbers",
        arguments = new { number1 = 5, number2 = 3 }
    }
);

// 2. Send request
var response = await httpClient.PostAsJsonAsync("/rpc", request, JsonOptions.Default);
response.EnsureSuccessStatusCode();

// 3. Parse response
var jsonResponse = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(JsonOptions.Default);

// 4. Extract result using helper
var result = jsonResponse!.GetResult<dynamic>();
Console.WriteLine($"Result: {result}");
// Output: Result: { content = [ { type = "text", text = "{"result":8}" } ] }
```

### Example 2: List Tools (HTTP)

```csharp
using Mcp.Gateway.Tools;
using System.Net.Http.Json;

using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

// 1. Create tools/list request
var request = JsonRpcMessage.CreateRequest(
    Method: "tools/list",
    Id: 1
);

// 2. Send and receive
var response = await httpClient.PostAsJsonAsync("/rpc", request, JsonOptions.Default);
var jsonResponse = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(JsonOptions.Default);

// 3. Extract tools list
var toolsResult = jsonResponse!.GetResult<dynamic>();
Console.WriteLine($"Available tools: {toolsResult.tools}");
```

### Example 3: Initialize Protocol (HTTP)

```csharp
using Mcp.Gateway.Tools;

// Create initialize request
var request = JsonRpcMessage.CreateRequest(
    Method: "initialize",
    Id: 1,
    Params: new
    {
        protocolVersion = "2025-06-18",
        clientInfo = new
        {
            name = "my-client",
            version = "1.0.0"
        }
    }
);

// Send request
var response = await httpClient.PostAsJsonAsync("/rpc", request, JsonOptions.Default);
var jsonResponse = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(JsonOptions.Default);

// Get server info
var serverInfo = jsonResponse!.GetResult<dynamic>();
Console.WriteLine($"Server: {serverInfo.serverInfo.name} v{serverInfo.serverInfo.version}");
```

---

## WebSocket Client Examples

### Example 4: WebSocket Tool Call

```csharp
using Mcp.Gateway.Tools;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

// 1. Connect WebSocket
using var ws = new ClientWebSocket();
await ws.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);

// 2. Create request
var request = JsonRpcMessage.CreateRequest(
    Method: "tools/call",
    Id: 1,
    Params: new
    {
        name = "add_numbers",
        arguments = new { number1 = 10, number2 = 5 }
    }
);

// 3. Serialize and send
var requestJson = JsonSerializer.SerializeToUtf8Bytes(request, JsonOptions.Default);
await ws.SendAsync(requestJson, WebSocketMessageType.Text, true, CancellationToken.None);

// 4. Receive response
var buffer = new byte[4096];
var result = await ws.ReceiveAsync(buffer, CancellationToken.None);

// 5. Parse response
var responseJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
var response = JsonSerializer.Deserialize<JsonRpcMessage>(responseJson, JsonOptions.Default);

// 6. Extract result
var toolResult = response!.GetResult<dynamic>();
Console.WriteLine($"Result: {toolResult}");

// 7. Close WebSocket
await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
```

### Example 5: WebSocket Initialize + List Tools

```csharp
using Mcp.Gateway.Tools;
using System.Net.WebSockets;
using System.Text.Json;

using var ws = new ClientWebSocket();
await ws.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);

// Helper: Send JSON-RPC message
async Task SendMessage(JsonRpcMessage msg)
{
    var json = JsonSerializer.SerializeToUtf8Bytes(msg, JsonOptions.Default);
    await ws.SendAsync(json, WebSocketMessageType.Text, true, CancellationToken.None);
}

// Helper: Receive JSON-RPC message
async Task<JsonRpcMessage?> ReceiveMessage()
{
    var buffer = new byte[4096];
    var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
    return JsonSerializer.Deserialize<JsonRpcMessage>(json, JsonOptions.Default);
}

// 1. Initialize
await SendMessage(JsonRpcMessage.CreateRequest(
    Method: "initialize",
    Id: 1,
    Params: new { protocolVersion = "2025-06-18", clientInfo = new { name = "test-client" } }
));
var initResponse = await ReceiveMessage();
Console.WriteLine($"Initialized: {initResponse!.GetResult<dynamic>().serverInfo.name}");

// 2. List tools
await SendMessage(JsonRpcMessage.CreateRequest(Method: "tools/list", Id: 2));
var toolsResponse = await ReceiveMessage();
var tools = toolsResponse!.GetResult<dynamic>().tools;
Console.WriteLine($"Tools: {tools}");

await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
```

---

## Streaming Client Examples

### Example 6: Initiate Binary Stream (Client → Server)

```csharp
using Mcp.Gateway.Tools;
using System.Net.WebSockets;
using System.Text.Json;

using var ws = new ClientWebSocket();
await ws.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);

// 1. Create start message using helper
var meta = new StreamMessageMeta(
    Method: "system_binary_streams_in",
    Binary: true,
    Name: "myfile.bin",
    Mime: "application/octet-stream",
    TotalSize: 1024
);

var startMessage = StreamMessage.CreateStartMessage(meta);

// 2. Send start message
var startJson = JsonSerializer.SerializeToUtf8Bytes(startMessage, JsonOptions.Default);
await ws.SendAsync(startJson, WebSocketMessageType.Text, true, CancellationToken.None);

// 3. Send binary chunks
var streamId = startMessage.Id!;
for (int i = 0; i < 10; i++)
{
    // Create binary header (GUID + index)
    var header = StreamMessage.CreateBinaryHeader(streamId, i);
    
    // Create payload
    var payload = new byte[100];
    Array.Fill(payload, (byte)i);
    
    // Combine header + payload
    var frame = new byte[header.Length + payload.Length];
    header.CopyTo(frame, 0);
    payload.CopyTo(frame, header.Length);
    
    // Send binary frame
    await ws.SendAsync(frame, WebSocketMessageType.Binary, true, CancellationToken.None);
}

// 4. Send done message
var doneMessage = StreamMessage.CreateDoneMessage(streamId, new { totalChunks = 10 });
var doneJson = JsonSerializer.SerializeToUtf8Bytes(doneMessage, JsonOptions.Default);
await ws.SendAsync(doneJson, WebSocketMessageType.Text, true, CancellationToken.None);

// 5. Receive response
var buffer = new byte[4096];
var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
var responseJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
Console.WriteLine($"Server response: {responseJson}");

await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
```

### Example 7: Receive Binary Stream (Server → Client)

```csharp
using Mcp.Gateway.Tools;
using System.Net.WebSockets;
using System.Text.Json;

using var ws = new ClientWebSocket();
await ws.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);

// 1. Create start message to request stream
var meta = new StreamMessageMeta(
    Method: "system_binary_streams_out",
    Binary: true
);

var startMessage = StreamMessage.CreateStartMessage(meta);
var startJson = JsonSerializer.SerializeToUtf8Bytes(startMessage, JsonOptions.Default);
await ws.SendAsync(startJson, WebSocketMessageType.Text, true, CancellationToken.None);

// 2. Receive stream
var buffer = new byte[64 * 1024]; // 64KB buffer
var totalBytes = 0L;

while (ws.State == WebSocketState.Open)
{
    var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
    
    if (result.MessageType == WebSocketMessageType.Close)
    {
        break;
    }
    
    if (result.MessageType == WebSocketMessageType.Binary)
    {
        // Parse binary header
        if (StreamMessage.TryParseBinaryHeader(buffer, out var streamId, out var index))
        {
            var payloadSize = result.Count - StreamMessage.BinaryHeaderSize;
            totalBytes += payloadSize;
            
            Console.WriteLine($"Received chunk {index}: {payloadSize} bytes");
        }
    }
    else if (result.MessageType == WebSocketMessageType.Text)
    {
        // Parse JSON message (start/done/error)
        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        
        if (StreamMessage.TryGetFromJsonElement(
            JsonDocument.Parse(json).RootElement,
            out var msg) && msg != null)
        {
            if (msg.IsDone)
            {
                Console.WriteLine($"Stream complete. Summary: {msg.Summary}");
                break;
            }
            else if (msg.IsError)
            {
                Console.WriteLine($"Stream error: {msg.Error?.Message}");
                break;
            }
        }
    }
}

Console.WriteLine($"Total bytes received: {totalBytes}");

await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
```

### Example 8: Text Stream (JSON Chunks)

```csharp
using Mcp.Gateway.Tools;
using System.Net.WebSockets;
using System.Text.Json;

using var ws = new ClientWebSocket();
await ws.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);

// 1. Request text stream
var meta = new StreamMessageMeta(
    Method: "system_text_streams_out",
    Binary: false,
    Encoding: "utf-8"
);

var startMessage = StreamMessage.CreateStartMessage(meta);
var startJson = JsonSerializer.SerializeToUtf8Bytes(startMessage, JsonOptions.Default);
await ws.SendAsync(startJson, WebSocketMessageType.Text, true, CancellationToken.None);

// 2. Receive text chunks
var buffer = new byte[4096];
var chunkCount = 0;

while (ws.State == WebSocketState.Open)
{
    var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
    
    if (result.MessageType == WebSocketMessageType.Close)
        break;
    
    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
    
    if (StreamMessage.TryGetFromJsonElement(
        JsonDocument.Parse(json).RootElement,
        out var msg) && msg != null)
    {
        if (msg.IsChunk)
        {
            Console.WriteLine($"Chunk {msg.Index}: {msg.Data}");
            chunkCount++;
        }
        else if (msg.IsDone)
        {
            Console.WriteLine($"Done! Summary: {msg.Summary}");
            break;
        }
        else if (msg.IsError)
        {
            Console.WriteLine($"Error: {msg.Error?.Message}");
            break;
        }
    }
}

Console.WriteLine($"Received {chunkCount} chunks");

await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
```

---

## Helper Methods Reference

### JsonRpcMessage Helpers

```csharp
// Create request
var request = JsonRpcMessage.CreateRequest(
    Method: "method_name",
    Id: 1,
    Params: new { /* params */ }
);

// Create notification (no response expected)
var notification = JsonRpcMessage.CreateNotification(
    Method: "method_name",
    Params: new { /* params */ }
);

// Create success response (server-side)
var success = JsonRpcMessage.CreateSuccess(
    Id: 1,
    Result: new { /* result */ }
);

// Create error response (server-side)
var error = JsonRpcMessage.CreateError(
    Id: 1,
    Error: new JsonRpcError(
        Code: -32600,
        Message: "Invalid Request",
        Data: null
    )
);

// Extract result
var result = response.GetResult<MyType>();

// Extract params
var params = request.GetParams<MyParamsType>();

// Check message type
if (response.IsSuccessResponse) { /* ... */ }
if (response.IsErrorResponse) { /* ... */ }
if (request.IsRequest) { /* ... */ }
if (request.IsNotification) { /* ... */ }
```

### StreamMessage Helpers

```csharp
// Create start message
var meta = new StreamMessageMeta(
    Method: "tool_name",
    Binary: true,
    Name: "filename.bin",
    Mime: "application/octet-stream",
    TotalSize: 1024
);
var start = StreamMessage.CreateStartMessage(meta);

// Create chunk message (text)
var chunk = StreamMessage.CreateChunkMessage(
    Id: streamId,
    Index: 0,
    Data: new { key = "value" }
);

// Create done message
var done = StreamMessage.CreateDoneMessage(
    Id: streamId,
    Summary: new { totalChunks = 10 }
);

// Create error message
var error = StreamMessage.CreateErrorMessage(
    Id: streamId,
    Error: new JsonRpcError(-32603, "Internal error", null)
);

// Parse binary header
if (StreamMessage.TryParseBinaryHeader(frame, out var streamId, out var index))
{
    // Extract payload
    var payload = frame[StreamMessage.BinaryHeaderSize..];
}

// Create binary header
var header = StreamMessage.CreateBinaryHeader(streamId, index);

// Check message type
if (msg.IsStart) { /* ... */ }
if (msg.IsChunk) { /* ... */ }
if (msg.IsDone) { /* ... */ }
if (msg.IsError) { /* ... */ }
```

### StreamMessageMeta Helpers

```csharp
// Parse from object (e.g., from StreamMessage.Meta)
if (StreamMessageMeta.TryGetFromObject(connector.StreamMessage?.Meta, out var meta) && meta != null)
{
    var method = meta.Method;
    var isBinary = meta.Binary;
    var fileName = meta.Name;
    var mimeType = meta.Mime;
    var totalSize = meta.TotalSize;
}
```

---

## 🎓 Best Practices

1. **Use helpers** - `CreateRequest()`, `CreateStartMessage()`, etc.
2. **Type-safe extraction** - `GetResult<T>()`, `GetParams<T>()`
3. **Check message types** - `IsSuccessResponse`, `IsStart`, `IsChunk`, etc.
4. **Reuse JsonOptions.Default** - For consistent serialization
5. **Handle errors** - Check `IsErrorResponse` before accessing `Result`
6. **Close WebSockets cleanly** - Use `CloseAsync()` with proper status
7. **Buffer size** - Use 64KB buffers for optimal performance

---

## 📚 See Also

- [MCP Protocol Documentation](../MCP-Protocol.md)
- [Streaming Protocol v1.0](../StreamingProtocol.md)
- [ToolConnector Usage (Server-side)](toolconnector-usage.md)
- [Tool Creation Guide](../../README.md)

---

**Built with ❤️ using Mcp.Gateway.Tools**
