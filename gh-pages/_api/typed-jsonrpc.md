---
layout: mcp-default
title: TypedJsonRpc<T> and Schema Generation
description: Automatic JSON Schema generation from C# types
breadcrumbs:
  - title: Home
    url: /
  - title: API Reference
  - title: TypedJsonRpc<T>
    url: /api/typed-jsonrpc/
toc: true
---

# TypedJsonRpc<T> and Schema Generation

**Added in:** v1.0.0  
**Auto-Schema:** v1.8.0+  
**Status:** Production-ready

## Overview

`TypedJsonRpc<T>` provides type-safe parameter handling with **automatic JSON Schema generation** from your C# types.

**Benefits:**
- ✅ **Type-safe** - Compile-time validation
- ✅ **Auto-schema** - No manual JSON Schema needed
- ✅ **IntelliSense** - Full IDE support
- ✅ **Nullable** - C# nullable reference types respected
- ✅ **Descriptions** - `[Description]` attributes included
- ✅ **Enums** - Automatic enum value lists
- ✅ **Formats** - DateTime, Guid, etc. auto-detected

## Quick Example

### Before (Manual Schema)

```csharp
[McpTool("greet",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":{
            ""name"":{""type"":""string"",""description"":""User name""}
        },
        ""required"":[""name""]
    }")]
public JsonRpcMessage Greet(JsonRpcMessage request)
{
    var name = request.GetParams().GetProperty("name").GetString();
    return ToolResponse.Success(request.Id, new { message = $"Hello, {name}!" });
}
```

### After (Auto-Schema)

```csharp
[McpTool("greet")]
public JsonRpcMessage Greet(TypedJsonRpc<GreetParams> request)
{
    var args = request.GetParams()!;
    return ToolResponse.Success(request.Id, 
        new { message = $"Hello, {args.Name}!" });
}

public sealed record GreetParams(
    [property: Description("User name")] string Name);
```

**Result:** Same JSON Schema, less code! ✨

## How It Works

### 1. Type Mapping

`ToolSchemaGenerator` automatically maps C# types to JSON Schema types:

| C# Type | JSON Type | Format |
|---------|-----------|--------|
| `string` | `"string"` | - |
| `int`, `long`, `short`, `byte` | `"integer"` | - |
| `float`, `double`, `decimal` | `"number"` | - |
| `bool` | `"boolean"` | - |
| `Guid` | `"string"` | `"uuid"` |
| `DateTime`, `DateTimeOffset` | `"string"` | `"date-time"` |
| `enum` | `"string"` | - (with `enum` values) |
| `T[]`, `List<T>` | `"array"` | - |
| Complex types | `"object"` | - |

### 2. Nullable Detection

C# nullable reference types are respected:

```csharp
public sealed record MyParams(
    string Name,        // Required (non-nullable)
    string? Email);     // Optional (nullable)
```

**Generated schema:**
```json
{
  "type": "object",
  "properties": {
    "name": {"type": "string"},
    "email": {"type": "string"}
  },
  "required": ["name"]
}
```

### 3. Description Attributes

Use `[Description]` from `System.ComponentModel`:

```csharp
using System.ComponentModel;

public sealed record UserParams(
    [property: Description("User's full name")] 
    string Name,
    [property: Description("User's email address")] 
    string? Email);
```

**Generated schema:**
```json
{
  "type": "object",
  "properties": {
    "name": {
      "type": "string",
      "description": "User's full name"
    },
    "email": {
      "type": "string",
      "description": "User's email address"
    }
  },
  "required": ["name"]
}
```

### 4. Enum Support

Enums automatically generate allowed values:

```csharp
public enum Priority { Low, Medium, High }

public sealed record TaskParams(
    [property: Description("Task priority level")] 
    Priority Priority);
```

**Generated schema:**
```json
{
  "type": "object",
  "properties": {
    "priority": {
      "type": "string",
      "enum": ["Low", "Medium", "High"],
      "description": "Task priority level"
    }
  },
  "required": ["priority"]
}
```

### 5. JsonPropertyName

Control JSON property names:

```csharp
using System.Text.Json.Serialization;

public sealed record UserParams(
    [property: JsonPropertyName("full_name")] 
    string FullName);
```

**Generated schema:**
```json
{
  "type": "object",
  "properties": {
    "full_name": {"type": "string"}
  },
  "required": ["full_name"]
}
```

**Default:** Property names are automatically converted to camelCase:
- `FullName` → `"fullName"`
- `EmailAddress` → `"emailAddress"`

## Supported Features

### ✅ Automatic Features

| Feature | Support | Example |
|---------|---------|---------|
| **Type mapping** | ✅ Full | `string`, `int`, `double`, etc. |
| **Nullable** | ✅ Full | `string?` → optional |
| **Description** | ✅ Full | `[Description("...")]` |
| **Enum** | ✅ Full | `enum` → `"enum": [...]` |
| **Formats** | ✅ Partial | `Guid`, `DateTime` |
| **Arrays** | ✅ Basic | `string[]`, `List<T>` |
| **Nested objects** | ✅ Basic | `type: "object"` |
| **Default values** | ❌ Not yet | Use `InputSchema` |

### ⚠️ Limitations

Auto-schema generation does **NOT** support:

| Feature | Workaround |
|---------|------------|
| `anyOf`, `oneOf`, `allOf` | Use manual `InputSchema` |
| `if/then/else` | Use manual `InputSchema` |
| Array item schemas | Use manual `InputSchema` |
| Min/Max constraints | Use manual `InputSchema` |
| Pattern (regex) | Use manual `InputSchema` |
| Custom formats | Use manual `InputSchema` |
| Recursive types | Use manual `InputSchema` |

**For advanced schemas:** Use explicit `InputSchema` parameter!

## Complete Examples

### Example 1: Simple Parameters

```csharp
[McpTool("create_user")]
public JsonRpcMessage CreateUser(TypedJsonRpc<CreateUserParams> request)
{
    var args = request.GetParams()!;
    // args.Name, args.Email, args.Age are strongly typed
    return ToolResponse.Success(request.Id, new { userId = 123 });
}

public sealed record CreateUserParams(
    [property: Description("User's full name")] 
    string Name,
    [property: Description("User's email address")] 
    string Email,
    [property: Description("User's age in years")] 
    int? Age);
```

**Auto-generated schema:**
```json
{
  "type": "object",
  "properties": {
    "name": {
      "type": "string",
      "description": "User's full name"
    },
    "email": {
      "type": "string",
      "description": "User's email address"
    },
    "age": {
      "type": "integer",
      "description": "User's age in years"
    }
  },
  "required": ["name", "email"]
}
```

### Example 2: Enum and DateTime

```csharp
[McpTool("create_task")]
public JsonRpcMessage CreateTask(TypedJsonRpc<CreateTaskParams> request)
{
    var args = request.GetParams()!;
    return ToolResponse.Success(request.Id, new { taskId = 456 });
}

public enum TaskStatus { Pending, InProgress, Completed }

public sealed record CreateTaskParams(
    [property: Description("Task title")] 
    string Title,
    [property: Description("Task status")] 
    TaskStatus Status,
    [property: Description("Due date (ISO 8601)")] 
    DateTime? DueDate);
```

**Auto-generated schema:**
```json
{
  "type": "object",
  "properties": {
    "title": {
      "type": "string",
      "description": "Task title"
    },
    "status": {
      "type": "string",
      "enum": ["Pending", "InProgress", "Completed"],
      "description": "Task status"
    },
    "dueDate": {
      "type": "string",
      "format": "date-time",
      "description": "Due date (ISO 8601)"
    }
  },
  "required": ["title", "status"]
}
```

### Example 3: Arrays

```csharp
[McpTool("send_email")]
public JsonRpcMessage SendEmail(TypedJsonRpc<SendEmailParams> request)
{
    var args = request.GetParams()!;
    // args.Recipients is string[]
    return ToolResponse.Success(request.Id, new { sent = true });
}

public sealed record SendEmailParams(
    [property: Description("Email subject")] 
    string Subject,
    [property: Description("Email body")] 
    string Body,
    [property: Description("List of recipient email addresses")] 
    string[] Recipients);
```

**Auto-generated schema:**
```json
{
  "type": "object",
  "properties": {
    "subject": {
      "type": "string",
      "description": "Email subject"
    },
    "body": {
      "type": "string",
      "description": "Email body"
    },
    "recipients": {
      "type": "array",
      "description": "List of recipient email addresses"
    }
  },
  "required": ["subject", "body", "recipients"]
}
```

### Example 4: Guid and Custom Names

```csharp
[McpTool("update_resource")]
public JsonRpcMessage UpdateResource(TypedJsonRpc<UpdateResourceParams> request)
{
    var args = request.GetParams()!;
    return ToolResponse.Success(request.Id, new { updated = true });
}

public sealed record UpdateResourceParams(
    [property: JsonPropertyName("resource_id")]
    [property: Description("Unique resource identifier")] 
    Guid ResourceId,
    [property: JsonPropertyName("resource_name")]
    [property: Description("Resource name")] 
    string ResourceName);
```

**Auto-generated schema:**
```json
{
  "type": "object",
  "properties": {
    "resource_id": {
      "type": "string",
      "format": "uuid",
      "description": "Unique resource identifier"
    },
    "resource_name": {
      "type": "string",
      "description": "Resource name"
    }
  },
  "required": ["resource_id", "resource_name"]
}
```

## When to Use InputSchema

Use explicit `InputSchema` when you need:

### 1. Union Types (anyOf, oneOf)

```csharp
[McpTool("process_input",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":{
            ""value"":{
                ""oneOf"":[
                    {""type"":""string""},
                    {""type"":""number""}
                ]
            }
        }
    }")]
public JsonRpcMessage ProcessInput(JsonRpcMessage request)
{
    var value = request.GetParams().GetProperty("value");
    // Manual handling of union type
}
```

### 2. Conditional Schemas (if/then/else)

```csharp
[McpTool("conditional_tool",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":{
            ""type"":{""type"":""string"",""enum"":[""A"",""B""]},
            ""value"":{""type"":""string""}
        },
        ""if"":{""properties"":{""type"":{""const"":""A""}}},
        ""then"":{""required"":[""value""]}
    }")]
```

### 3. Array Item Schemas

```csharp
[McpTool("process_items",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":{
            ""items"":{
                ""type"":""array"",
                ""items"":{
                    ""type"":""object"",
                    ""properties"":{
                        ""id"":{""type"":""integer""},
                        ""name"":{""type"":""string""}
                    }
                }
            }
        }
    }")]
```

### 4. Min/Max Constraints

```csharp
[McpTool("create_user",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":{
            ""age"":{
                ""type"":""integer"",
                ""minimum"":18,
                ""maximum"":100
            },
            ""name"":{
                ""type"":""string"",
                ""minLength"":1,
                ""maxLength"":50
            }
        }
    }")]
```

### 5. Pattern Matching

```csharp
[McpTool("validate_email",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":{
            ""email"":{
                ""type"":""string"",
                ""pattern"":""^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$""
            }
        }
    }")]
```

## Hybrid Approach

Combine auto-schema with validation:

```csharp
[McpTool("create_user")]
public JsonRpcMessage CreateUser(TypedJsonRpc<CreateUserParams> request)
{
    var args = request.GetParams()!;
    
    // Manual validation for complex rules
    if (args.Age.HasValue && (args.Age < 18 || args.Age > 100))
    {
        throw new ToolInvalidParamsException("Age must be between 18 and 100");
    }
    
    if (args.Name.Length > 50)
    {
        throw new ToolInvalidParamsException("Name too long (max 50 chars)");
    }
    
    return ToolResponse.Success(request.Id, new { userId = 123 });
}

public sealed record CreateUserParams(
    [property: Description("User's name (max 50 characters)")] 
    string Name,
    [property: Description("User's age (18-100)")] 
    int? Age);
```

**Benefits:**
- ✅ Auto-schema for basic validation
- ✅ Manual validation for complex rules
- ✅ Type-safe parameters
- ✅ Clear error messages

## Best Practices

### 1. Always Use Sealed Records

```csharp
// ✅ GOOD - Sealed record
public sealed record MyParams(string Name);

// ❌ BAD - Class (mutable)
public class MyParams
{
    public string Name { get; set; }
}
```

### 2. Add Descriptions

```csharp
// ✅ GOOD - Documented
public sealed record CreateUserParams(
    [property: Description("User's full name")] string Name);

// ⚠️ OK - But less helpful
public sealed record CreateUserParams(string Name);
```

### 3. Use Nullable Appropriately

```csharp
// ✅ GOOD - Clear intent
public sealed record SearchParams(
    string Query,      // Required
    int? Limit,        // Optional
    string? Sort);     // Optional

// ❌ BAD - Everything nullable
public sealed record SearchParams(
    string? Query,     // Should be required!
    int? Limit,
    string? Sort);
```

### 4. JsonPropertyName for API Consistency

```csharp
// ✅ GOOD - Explicit naming
public sealed record UserParams(
    [property: JsonPropertyName("user_id")] Guid UserId,
    [property: JsonPropertyName("user_name")] string UserName);

// ⚠️ OK - Auto camelCase
public sealed record UserParams(Guid UserId, string UserName);
// → {"userId": "...", "userName": "..."}
```

## Performance

Auto-schema generation:
- **When:** Once during tool discovery (startup)
- **Cost:** ~5-10ms per tool (one-time)
- **Memory:** Cached in `ToolService`
- **Runtime:** Zero overhead (pre-generated)

**No performance impact during tool invocations!**

## Troubleshooting

### Schema Not Generated

**Problem:** Tool uses `TypedJsonRpc<T>` but no schema appears

**Solutions:**
1. Check if `InputSchema` is explicitly set (it takes priority)
2. Ensure `T` is a public type
3. Verify properties are public
4. Check logs for schema generation errors

### Wrong Property Names

**Problem:** JSON property names don't match C# names

**Solution:** Use `[JsonPropertyName]`:
```csharp
public sealed record MyParams(
    [property: JsonPropertyName("my_field")] string MyField);
```

### Enum Not Working

**Problem:** Enum values not appearing in schema

**Solution:** Ensure enum is public and non-nested:
```csharp
// ✅ GOOD
public enum Status { Active, Inactive }

// ❌ BAD - Nested enum
public class MyClass
{
    private enum Status { ... }  // Won't work!
}
```

## See Also

- [Tools API](/mcp.gateway/api/tools/) - Complete Tools API reference
- [Getting Started](/mcp.gateway/getting-started/first-tool/) - Build your first tool
- [DateTime Example](/mcp.gateway/examples/datetime/) - Real-world TypedJsonRpc usage
