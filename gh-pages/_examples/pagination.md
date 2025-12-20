---
layout: mcp-default
title: Pagination Server Example
description: Test pagination with 120 mock tools, prompts, and resources
breadcrumbs:
  - title: Home
    url: /
  - title: Pagination Server
    url: /examples/pagination/
prev: false
next: false
toc: true
---

# Pagination Server Example

**Version:** v1.7.0+  
**Features:** Pagination, Tools, Prompts, Resources  
**Complexity:** Beginner

## Overview

A test server for demonstrating MCP pagination with:
- ✅ **120 mock tools** - Test `tools/list` pagination
- ✅ **120 mock prompts** - Test `prompts/list` pagination
- ✅ **120 mock resources** - Test `resources/list` pagination
- ✅ **Cursor-based pagination** - Efficient pagination pattern

Perfect for:
- Testing pagination implementation
- Understanding cursor-based pagination
- Load testing with many tools/prompts/resources
- Client-side pagination testing

## Quick Start

### Run the Server

```bash
cd Examples/PaginationMcpServer
dotnet run
```

Server starts at: `http://localhost:5000`

### Test Pagination

```bash
# List first page of tools (default page size: 50)
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/list",
    "id": 1
  }'

# List second page with cursor
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/list",
    "params": {
      "cursor": "50"
    },
    "id": 2
  }'
```

## Available Collections

### Tools (120 mock tools)

- `mock_tool_001` through `mock_tool_120`
- Each tool returns its index: `{ "index": 1 }`
- Perfect for testing `tools/list` pagination

### Prompts (120 mock prompts)

- `mock_prompt_001` through `mock_prompt_120`
- Each prompt has a description and arguments
- Perfect for testing `prompts/list` pagination

### Resources (120 mock resources)

- `mock://resource/001` through `mock://resource/120`
- Each resource returns mock data
- Perfect for testing `resources/list` pagination

## Pagination Behavior

### Default Page Size

**50 items per page** (MCP protocol default)

```json
{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "id": 1
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "tools": [
      { "name": "mock_tool_001", "description": "Mock tool 001" },
      { "name": "mock_tool_002", "description": "Mock tool 002" },
      ...
      { "name": "mock_tool_050", "description": "Mock tool 050" }
    ],
    "nextCursor": "50"
  },
  "id": 1
}
```

### Second Page

```json
{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "params": {
    "cursor": "50"
  },
  "id": 2
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "tools": [
      { "name": "mock_tool_051", "description": "Mock tool 051" },
      { "name": "mock_tool_052", "description": "Mock tool 052" },
      ...
      { "name": "mock_tool_100", "description": "Mock tool 100" }
    ],
    "nextCursor": "100"
  },
  "id": 2
}
```

### Last Page

```json
{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "params": {
    "cursor": "100"
  },
  "id": 3
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "tools": [
      { "name": "mock_tool_101", "description": "Mock tool 101" },
      { "name": "mock_tool_102", "description": "Mock tool 102" },
      ...
      { "name": "mock_tool_120", "description": "Mock tool 120" }
    ],
    "nextCursor": null
  },
  "id": 3
}
```

**Note:** `nextCursor: null` indicates no more pages!

## Testing All Collections

### Test Tools Pagination

```bash
# Page 1
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'

# Page 2
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{"jsonrpc":"2.0","method":"tools/list","params":{"cursor":"50"},"id":2}'

# Page 3 (last page)
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{"jsonrpc":"2.0","method":"tools/list","params":{"cursor":"100"},"id":3}'
```

### Test Prompts Pagination

```bash
# Same pattern as tools
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{"jsonrpc":"2.0","method":"prompts/list","id":1}'
```

### Test Resources Pagination

```bash
# Same pattern as tools
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{"jsonrpc":"2.0","method":"resources/list","id":1}'
```

## JavaScript Client Example

```javascript
// Fetch all tools using pagination
async function fetchAllTools() {
  let cursor = null;
  let allTools = [];
  let pageNumber = 1;
  
  do {
    console.log(`Fetching page ${pageNumber}...`);
    
    const response = await fetch('http://localhost:5000/rpc', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'MCP-Protocol-Version': '2025-11-25'
      },
      body: JSON.stringify({
        jsonrpc: '2.0',
        method: 'tools/list',
        params: cursor ? { cursor } : {},
        id: pageNumber
      })
    });
    
    const result = await response.json();
    const tools = result.result.tools;
    const nextCursor = result.result.nextCursor;
    
    allTools.push(...tools);
    console.log(`  → Got ${tools.length} tools (total: ${allTools.length})`);
    
    cursor = nextCursor;
    pageNumber++;
    
  } while (cursor !== null);
  
  console.log(`\nTotal tools: ${allTools.length}`);
  return allTools;
}

// Run it
fetchAllTools().then(tools => {
  console.log('All tools fetched:', tools.length);
  console.log('First tool:', tools[0]);
  console.log('Last tool:', tools[tools.length - 1]);
});
```

**Expected output:**
```
Fetching page 1...
  → Got 50 tools (total: 50)
Fetching page 2...
  → Got 50 tools (total: 100)
Fetching page 3...
  → Got 20 tools (total: 120)

Total tools: 120
All tools fetched: 120
First tool: { name: 'mock_tool_001', description: 'Mock tool 001' }
Last tool: { name: 'mock_tool_120', description: 'Mock tool 120' }
```

## Testing Pagination Logic

The PaginationMcpServer is perfect for testing pagination edge cases:

### Test Cases

1. **First Page** - No cursor
   - Should return first 50 items
   - Should include `nextCursor: "50"`

2. **Middle Page** - Cursor: "50"
   - Should return items 51-100
   - Should include `nextCursor: "100"`

3. **Last Page** - Cursor: "100"
   - Should return items 101-120 (only 20 items)
   - Should include `nextCursor: null`

4. **Empty Page** - Cursor beyond last item
   - Should return empty array
   - Should include `nextCursor: null`

### Integration Tests

The PaginationMcpServerTests project includes comprehensive tests:

```bash
cd Examples/PaginationMcpServerTests
dotnet test
```

**Test coverage:**
- ✅ First page pagination
- ✅ Middle page pagination
- ✅ Last page pagination
- ✅ Empty page handling
- ✅ Invalid cursor handling
- ✅ All three collections (tools, prompts, resources)

## Code Examples

### Server-Side Mock Tools

```csharp
public partial class MockTools
{
    [McpTool("mock_tool_001", Description = "Mock tool 001")]
    public JsonRpcMessage Tool001(JsonRpcMessage r) 
        => ToolResponse.Success(r.Id, new { index = 1 });
    
    [McpTool("mock_tool_002", Description = "Mock tool 002")]
    public JsonRpcMessage Tool002(JsonRpcMessage r) 
        => ToolResponse.Success(r.Id, new { index = 2 });
    
    // ... 118 more tools ...
    
    [McpTool("mock_tool_120", Description = "Mock tool 120")]
    public JsonRpcMessage Tool120(JsonRpcMessage r) 
        => ToolResponse.Success(r.Id, new { index = 120 });
}
```

**Why partial class?**
- Split into 3 files for better organization
- Part1.cs: Tools 001-040
- Part2.cs: Tools 041-080
- Part3.cs: Tools 081-120

### Server-Side Mock Prompts

```csharp
public class MockPrompts
{
    [McpPrompt("mock_prompt_001",
        Description = "Mock prompt 001",
        Arguments = new[] { "arg1", "arg2" })]
    public JsonRpcMessage Prompt001(JsonRpcMessage r)
        => ToolResponse.Success(r.Id, new
        {
            description = "Mock prompt 001",
            arguments = new[] { "arg1", "arg2" }
        });
    
    // ... 119 more prompts ...
}
```

### Server-Side Mock Resources

```csharp
public class MockResources
{
    [McpResource("mock://resource/001",
        Name = "Mock Resource 001",
        Description = "Mock resource for testing",
        MimeType = "text/plain")]
    public JsonRpcMessage Resource001(JsonRpcMessage r)
        => ToolResponse.Success(r.Id, new ResourceContent(
            Uri: "mock://resource/001",
            MimeType: "text/plain",
            Text: "Mock resource 001 content"
        ));
    
    // ... 119 more resources ...
}
```

## Performance

### Pagination Overhead

**Negligible performance impact:**

```
Without pagination:  ~5ms (all 120 tools)
With pagination:     ~2ms per page (50 tools)
```

**Memory:**
- Each page: ~50 KB
- Full dataset: ~150 KB
- 3 pages total: Same memory as full dataset

### Load Testing

```bash
# Test 100 concurrent requests
for i in {1..100}; do
  curl -X POST http://localhost:5000/rpc \
    -H "Content-Type: application/json" \
    -H "MCP-Protocol-Version: 2025-11-25" \
    -d '{"jsonrpc":"2.0","method":"tools/list","id":'$i'}' &
done
wait
```

## Best Practices

### 1. Always Check nextCursor

```javascript
// ✅ GOOD
if (result.nextCursor !== null) {
  // More pages available
  fetchNextPage(result.nextCursor);
}

// ❌ BAD
if (result.nextCursor) {
  // This fails when nextCursor is "" or 0
}
```

### 2. Track Progress

```javascript
let totalFetched = 0;
let pageNumber = 1;

do {
  const result = await fetchPage(cursor);
  totalFetched += result.tools.length;
  
  console.log(`Page ${pageNumber}: ${result.tools.length} items (total: ${totalFetched})`);
  
  cursor = result.nextCursor;
  pageNumber++;
} while (cursor !== null);
```

### 3. Handle Errors

```javascript
try {
  const result = await fetchPage(cursor);
  return result;
} catch (error) {
  if (error.code === -32602) {
    // Invalid cursor - start from beginning
    return fetchPage(null);
  }
  throw error;
}
```

## Common Use Cases

### 1. Fetch All Items

```javascript
async function fetchAll(method) {
  let cursor = null;
  let items = [];
  
  do {
    const result = await fetch('http://localhost:5000/rpc', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'MCP-Protocol-Version': '2025-11-25'
      },
      body: JSON.stringify({
        jsonrpc: '2.0',
        method,
        params: cursor ? { cursor } : {},
        id: Date.now()
      })
    });
    
    const data = await result.json();
    const key = method.split('/')[0]; // 'tools', 'prompts', 'resources'
    
    items.push(...data.result[key]);
    cursor = data.result.nextCursor;
    
  } while (cursor !== null);
  
  return items;
}

// Usage
const tools = await fetchAll('tools/list');
const prompts = await fetchAll('prompts/list');
const resources = await fetchAll('resources/list');
```

### 2. Lazy Loading

```javascript
class PaginatedList {
  constructor(method) {
    this.method = method;
    this.items = [];
    this.cursor = null;
    this.hasMore = true;
  }
  
  async loadMore() {
    if (!this.hasMore) return;
    
    const result = await fetchPage(this.method, this.cursor);
    this.items.push(...result.items);
    this.cursor = result.nextCursor;
    this.hasMore = result.nextCursor !== null;
    
    return this.items;
  }
}

// Usage
const toolsList = new PaginatedList('tools/list');
await toolsList.loadMore(); // Load page 1
await toolsList.loadMore(); // Load page 2
```

### 3. Search with Pagination

```javascript
async function searchTools(query) {
  let cursor = null;
  let matches = [];
  
  do {
    const result = await fetchPage('tools/list', cursor);
    
    // Filter locally (server-side filtering is better!)
    const pageMatches = result.tools.filter(tool =>
      tool.name.includes(query) || 
      tool.description.includes(query)
    );
    
    matches.push(...pageMatches);
    cursor = result.nextCursor;
    
  } while (cursor !== null);
  
  return matches;
}

// Usage
const toolsWithMock = await searchTools('mock');
console.log(`Found ${toolsWithMock.length} tools matching 'mock'`);
```

## See Also

- [Pagination Feature](/mcp.gateway/features/pagination/) - Complete pagination guide
- [Tools API](/mcp.gateway/api/tools/) - Tools API reference
- [Resources API](/mcp.gateway/api/resources/) - Resources API reference
