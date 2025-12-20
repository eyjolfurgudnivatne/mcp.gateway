---
layout: mcp-default
title: Pagination
description: Handle large result sets with cursor-based pagination
breadcrumbs:
  - title: Home
    url: /
  - title: Pagination
    url: /features/pagination/
toc: true
---

# Pagination

Handle large result sets efficiently with cursor-based pagination.

## Overview

**Added in:** v1.7.0  
**Protocol:** MCP 2025-11-25  
**Methods:** `tools/list`, `prompts/list`, `resources/list`

Pagination allows servers to split large result sets into manageable chunks:
- ✅ **Cursor-based** - Efficient for large datasets
- ✅ **Stateless** - No server-side session required
- ✅ **Flexible** - Server controls page size

## Quick Example

### Server-Side

```csharp
[McpTool("list_users")]
public JsonRpcMessage ListUsers(TypedJsonRpc<ListUsersParams> request)
{
    var args = request.GetParams()!;
    var cursor = args.Cursor;
    var pageSize = 10;
    
    // Parse cursor (simple offset in this example)
    var offset = string.IsNullOrEmpty(cursor) ? 0 : int.Parse(cursor);
    
    // Get page of results
    var users = _database.Users
        .Skip(offset)
        .Take(pageSize + 1)  // Take one extra to check if more exist
        .ToList();
    
    // Check if more results exist
    var hasMore = users.Count > pageSize;
    if (hasMore)
    {
        users = users.Take(pageSize).ToList();
    }
    
    // Generate next cursor
    var nextCursor = hasMore ? (offset + pageSize).ToString() : null;
    
    return ToolResponse.Success(
        request.Id,
        new
        {
            users,
            nextCursor
        });
}

public record ListUsersParams(string? Cursor = null);
```

### Client-Side

```javascript
async function fetchAllUsers() {
  let cursor = null;
  let allUsers = [];
  
  do {
    const response = await fetch('/mcp', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        jsonrpc: '2.0',
        method: 'tools/call',
        params: {
          name: 'list_users',
          arguments: { Cursor: cursor }
        },
        id: Date.now()
      })
    });
    
    const result = await response.json();
    const data = JSON.parse(result.result.content[0].text);
    
    allUsers.push(...data.users);
    cursor = data.nextCursor;
    
  } while (cursor !== null);
  
  return allUsers;
}
```

## Pagination Patterns

### 1. Offset-Based (Simple)

```csharp
var cursor = args.Cursor ?? "0";
var offset = int.Parse(cursor);

var results = _data
    .Skip(offset)
    .Take(pageSize)
    .ToList();

var nextCursor = results.Count == pageSize 
    ? (offset + pageSize).ToString() 
    : null;
```

### 2. Keyset-Based (Efficient)

```csharp
var cursor = args.Cursor;  // Last seen ID
var query = _dbContext.Users.AsQueryable();

if (!string.IsNullOrEmpty(cursor))
{
    query = query.Where(u => u.Id > cursor);
}

var results = query
    .OrderBy(u => u.Id)
    .Take(pageSize + 1)
    .ToList();

var hasMore = results.Count > pageSize;
var nextCursor = hasMore ? results[pageSize - 1].Id : null;
```

### 3. Token-Based (Secure)

```csharp
// Encode cursor state
var cursorData = new CursorData
{
    Offset = 100,
    Timestamp = DateTime.UtcNow
};

var json = JsonSerializer.Serialize(cursorData);
var bytes = Encoding.UTF8.GetBytes(json);
var cursor = Convert.ToBase64String(bytes);

// Decode cursor
var bytes = Convert.FromBase64String(args.Cursor);
var json = Encoding.UTF8.GetString(bytes);
var cursorData = JsonSerializer.Deserialize<CursorData>(json);
```

## Built-in Pagination

MCP Gateway automatically paginates:

### tools/list

```json
{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "params": {
    "cursor": "optional-cursor"
  },
  "id": 1
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "tools": [...],
    "nextCursor": "next-page-cursor"
  },
  "id": 1
}
```

### resources/list

```json
{
  "jsonrpc": "2.0",
  "method": "resources/list",
  "params": {
    "cursor": "optional-cursor"
  },
  "id": 2
}
```

## Best Practices

### 1. Consistent Page Size

```csharp
private const int DefaultPageSize = 10;
private const int MaxPageSize = 100;

var pageSize = Math.Min(args.PageSize ?? DefaultPageSize, MaxPageSize);
```

### 2. Validate Cursors

```csharp
if (!string.IsNullOrEmpty(cursor))
{
    if (!int.TryParse(cursor, out var offset) || offset < 0)
    {
        throw new ToolInvalidParamsException("Invalid cursor");
    }
}
```

### 3. Include Total Count (Optional)

```csharp
return ToolResponse.Success(
    request.Id,
    new
    {
        users,
        nextCursor,
        totalCount = _database.Users.Count()  // Optional
    });
```

### 4. Handle Edge Cases

```csharp
// Empty results
if (!results.Any())
{
    return ToolResponse.Success(request.Id, new
    {
        users = Array.Empty<User>(),
        nextCursor = (string?)null
    });
}

// Last page
var hasMore = results.Count > pageSize;
if (!hasMore)
{
    return ToolResponse.Success(request.Id, new
    {
        users = results,
        nextCursor = (string?)null
    });
}
```

## Testing

```csharp
[Fact]
public async Task ListUsers_FirstPage_ReturnsResults()
{
    // Arrange
    var request = new TypedJsonRpc<ListUsersParams>
    {
        Id = "1",
        Params = new ListUsersParams(Cursor: null)
    };
    
    // Act
    var response = await _tools.ListUsers(request);
    
    // Assert
    var result = JsonSerializer.Deserialize<ListUsersResult>(
        response.Result!.ToString()!);
    
    Assert.Equal(10, result.Users.Count);
    Assert.NotNull(result.NextCursor);
}

[Fact]
public async Task ListUsers_LastPage_ReturnsNullCursor()
{
    // Arrange - cursor pointing to last page
    var request = new TypedJsonRpc<ListUsersParams>
    {
        Id = "1",
        Params = new ListUsersParams(Cursor: "90")
    };
    
    // Act
    var response = await _tools.ListUsers(request);
    
    // Assert
    var result = JsonSerializer.Deserialize<ListUsersResult>(
        response.Result!.ToString()!);
    
    Assert.True(result.Users.Count <= 10);
    Assert.Null(result.NextCursor);
}
```

## See Also

- [Tools API](/mcp.gateway/api/tools/) - Complete Tools API reference
- [Resources API](/mcp.gateway/api/resources/) - Resources pagination
- [Pagination Example](/mcp.gateway/examples/pagination/) - Complete example
