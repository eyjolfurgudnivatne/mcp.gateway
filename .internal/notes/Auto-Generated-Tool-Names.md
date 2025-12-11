# 🏷️ Auto-Generated Tool Names

**Feature:** Tool names can be auto-generated from method names  
**Version:** v1.1+  
**Status:** ✅ Implemented

---

## 🎯 Overview

The `[McpTool]` attribute now supports **optional tool names**. If you don't specify a name, it will be auto-generated from your method name using snake_case conversion.

---

## ✨ Basic Usage

### Before (Explicit Name)

```csharp
[McpTool("ping")]
public JsonRpcMessage Ping(JsonRpcMessage request)
{
    return ToolResponse.Success(request.Id, new { message = "Pong" });
}
```

### Now (Auto-Generated)

```csharp
[McpTool]  // Name auto-generated: "ping"
public JsonRpcMessage Ping(JsonRpcMessage request)
{
    return ToolResponse.Success(request.Id, new { message = "Pong" });
}
```

**Result:** Same tool name (`"ping"`), less code! ✨

---

## 📐 Conversion Rules

The `ToolNameGenerator.ToSnakeCase()` method converts method names:

| Method Name | Auto-Generated Tool Name |
|-------------|--------------------------|
| `Ping` | `ping` |
| `AddNumbers` | `add_numbers` |
| `AddNumbersTool` | `add_numbers_tool` |
| `GetUserById` | `get_user_by_id` |
| `HTTPRequest` | `h_t_t_p_request` |
| `echo_message` | `echo_message` (already valid) |

**Algorithm:**
1. Insert underscore before uppercase letters (except at start)
2. Convert to lowercase

---

## 🎨 Examples

### Example 1: Simple Tool

```csharp
[McpTool]  // Auto-generates: "ping"
public JsonRpcMessage Ping(JsonRpcMessage request)
{
    return ToolResponse.Success(request.Id, new { message = "Pong" });
}
```

### Example 2: Multi-Word Name

```csharp
[McpTool]  // Auto-generates: "add_numbers"
public JsonRpcMessage AddNumbers(JsonRpcMessage request)
{
    var a = request.GetParams().GetProperty("a").GetInt32();
    var b = request.GetParams().GetProperty("b").GetInt32();
    return ToolResponse.Success(request.Id, new { result = a + b });
}
```

### Example 3: With Description

```csharp
[McpTool(Description = "Multiplies two numbers")]  // Auto-generates: "multiply_numbers"
public JsonRpcMessage MultiplyNumbers(JsonRpcMessage request)
{
    var x = request.GetParams().GetProperty("x").GetInt32();
    var y = request.GetParams().GetProperty("y").GetInt32();
    return ToolResponse.Success(request.Id, new { result = x * y });
}
```

**Note:** Description is set via the `Description` property, not from XML comments.

### Example 4: Explicit Name (Backward Compatible)

```csharp
[McpTool("get_user")]  // Explicit name takes priority
public JsonRpcMessage GetUserById(JsonRpcMessage request)
{
    var userId = request.GetParams().GetProperty("userId").GetInt32();
    return ToolResponse.Success(request.Id, new { userId, name = "John Doe" });
}
```

### Example 5: Already Valid Name

```csharp
[McpTool]  // Auto-generates: "echo_message" (method name is already valid)
public JsonRpcMessage echo_message(JsonRpcMessage request)
{
    return ToolResponse.Success(request.Id, request.Params);
}
```

---

## 🚨 Validation

Auto-generated names **must still** match the MCP tool name pattern:

**Pattern:** `^[a-zA-Z0-9_-]{1,128}$`

If auto-generation produces an invalid name, the tool will be **skipped** during discovery with a warning in Debug output.

**Examples:**

```csharp
✅ VALID - Will be registered:
[McpTool] public JsonRpcMessage AddNumbers(...) { }
// → "add_numbers"

❌ INVALID - Will be skipped:
[McpTool] public JsonRpcMessage Add.Numbers(...) { }
// → "add.numbers" (dots not allowed!)

✅ FIXED - Use explicit name:
[McpTool("add_numbers")] public JsonRpcMessage Add.Numbers(...) { }
// → "add_numbers"
```

---

## 💡 Best Practices

### 1. Use Descriptive Method Names

```csharp
✅ Good:
[McpTool]
public JsonRpcMessage AddNumbers(...) { }
// → "add_numbers" (clear and descriptive)

⚠️ Less Clear:
[McpTool]
public JsonRpcMessage Add(...) { }
// → "add" (what does it add?)
```

### 2. Consistent Naming Convention

Choose one and stick to it:

**Option A: PascalCase methods + auto-naming**
```csharp
[McpTool] public JsonRpcMessage AddNumbers(...) { }      // → "add_numbers"
[McpTool] public JsonRpcMessage MultiplyNumbers(...) { } // → "multiply_numbers"
[McpTool] public JsonRpcMessage GetUser(...) { }         // → "get_user"
```

**Option B: snake_case methods (already valid)**
```csharp
[McpTool] public JsonRpcMessage add_numbers(...) { }      // → "add_numbers"
[McpTool] public JsonRpcMessage multiply_numbers(...) { } // → "multiply_numbers"
[McpTool] public JsonRpcMessage get_user(...) { }         // → "get_user"
```

**Option C: Explicit names (full control)**
```csharp
[McpTool("add_numbers")] public JsonRpcMessage Add(...) { }
[McpTool("multiply_numbers")] public JsonRpcMessage Multiply(...) { }
[McpTool("get_user")] public JsonRpcMessage GetUserById(...) { }
```

### 3. When to Use Explicit Names

Use explicit names when:
- ❌ Auto-generated name would be unclear
- ❌ Method name contains invalid characters
- ❌ You want a specific name for consistency
- ❌ Migrating from existing tools with established names

```csharp
// Unclear auto-name
[McpTool("http_request")]  // Better than "h_t_t_p_request"
public JsonRpcMessage HTTPRequest(...) { }

// Established name
[McpTool("ping")]  // Keep existing name
public JsonRpcMessage SystemPing(...) { }
```

---

## 🔧 Implementation Details

### ToolNameGenerator Class

```csharp
public static class ToolNameGenerator
{
    /// <summary>
    /// Converts method name to snake_case.
    /// Examples: "AddNumbers" → "add_numbers"
    /// </summary>
    public static string ToSnakeCase(string methodName);

    /// <summary>
    /// Converts tool name to human-readable title.
    /// Examples: "add_numbers" → "Add Numbers"
    /// </summary>
    public static string ToHumanizedTitle(string toolName);
}
```

### Discovery Process

1. `ToolService` scans assemblies for `[McpTool]` methods
2. For each method:
   - Get `McpToolAttribute.Name`
   - If `null`, call `ToolNameGenerator.ToSnakeCase(methodName)`
   - Validate name with `ToolMethodNameValidator.IsValid()`
   - If invalid, skip with warning
   - If valid, register tool

### Debug Output

```
Scanning assembly: Mcp.Gateway.Server
Found tool: ping (from method: Ping) in MyTools
Found tool: add_numbers (from method: AddNumbers) in MyTools
WARNING: Skipping tool 'Add.Numbers' - Invalid tool name 'add.numbers': ...
Tool scan complete. Registered 2 tools.
```

---

## 🧪 Testing

17 unit tests verify the name generation logic:

```csharp
[Theory]
[InlineData("AddNumbers", "add_numbers")]
[InlineData("Ping", "ping")]
[InlineData("GetUserById", "get_user_by_id")]
[InlineData("HTTPRequest", "h_t_t_p_request")]
public void ToSnakeCase_ValidInput_ReturnsSnakeCase(string input, string expected)
{
    var result = ToolNameGenerator.ToSnakeCase(input);
    Assert.Equal(expected, result);
}
```

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~ToolNameGeneratorTests"
```

---

## 📚 See Also

- [McpToolAttribute API](../Mcp.Gateway.Tools/McpToolAttribute.cs)
- [ToolNameGenerator API](../Mcp.Gateway.Tools/ToolNameGenerator.cs)
- [Example: AutoNamedTools](../Mcp.Gateway.Server/Tools/Examples/AutoNamedTools.cs)
- [Tool Naming Rules](../Mcp.Gateway.Tools/README.md#tool-naming-rules)

---

## 🎯 Migration Guide

### Migrating Existing Tools

**No breaking changes!** Existing tools with explicit names continue to work:

```csharp
// v1.0 (still works in v1.1+)
[McpTool("add_numbers")]
public JsonRpcMessage AddNumbers(...) { }

// v1.1+ (optional - same result)
[McpTool]
public JsonRpcMessage AddNumbers(...) { }
```

### Gradual Migration

You can mix styles:

```csharp
public class MyTools
{
    // Keep explicit name
    [McpTool("legacy_tool")]
    public JsonRpcMessage OldTool(...) { }

    // Use auto-naming for new tools
    [McpTool]
    public JsonRpcMessage NewTool(...) { }
}
```

---

**Feature Status:** ✅ Implemented  
**Version:** v1.1  
**Backward Compatible:** Yes  
**Breaking Changes:** None

