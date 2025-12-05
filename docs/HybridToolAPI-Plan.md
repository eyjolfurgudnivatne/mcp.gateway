# 🎨 Hybrid Tool API - Design Plan (v1.1+)

**Created:** 5. desember 2025  
**Status:** 📋 <del>Planned for v1.1 or v2.0</del> Brain-fart.

**Target:** Simplify tool creation while maintaining flexibility  
**Inspiration:** [modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk)

---

## 🎯 Goal

Support **two tool authoring styles**:

1. **Explicit (Current)** - Full control, manual InputSchema
2. **Magic (New)** - Auto-generated schema from method parameters

**Both styles should coexist** - developers choose based on needs.

---

## 📊 Comparison: Current vs. Proposed

### Current (Explicit) - v1.0

```csharp
[McpTool("add_numbers",
    Title = "Add Numbers",
    Description = "Adds two numbers and return result. Example: 5 + 3 = 8",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":{
            ""number1"":{""type"":""number"",""description"":""First number to add""},
            ""number2"":{""type"":""number"",""description"":""Second number to add""}
        },
        ""required"":[""number1"",""number2""]
    }")]
public JsonRpcMessage AddNumbersTool(JsonRpcMessage request)
{
    var args = request.GetParams<NumbersRequest>()
        ?? throw new ToolInvalidParamsException("...");

    return ToolResponse.Success(request.Id, new { result = args.Number1 + args.Number2 });
}

record NumbersRequest(double Number1, double Number2);
```

**Pros:**
- ✅ Full control over JSON Schema
- ✅ Works with ANY JSON structure
- ✅ Clear JSON-RPC contract
- ✅ Easy to debug

**Cons:**
- ❌ Verbose
- ❌ Manual schema maintenance
- ❌ Easy to make schema/code mismatch

---

### Proposed (Magic) - v1.1+

```csharp
[McpTool]  // Name inferred from method name: "add_numbers_tool"
/// <summary>Adds two numbers and return result. Example: 5 + 3 = 8</summary>
public double AddNumbersTool(double number1, double number2)
{
    return number1 + number2;
}
```

**Auto-generated:**
- Name: `"add_numbers_tool"` (from method name)
- Title: `"Add Numbers Tool"` (humanized from method name)
- Description: `"Adds two numbers and return result. Example: 5 + 3 = 8"` (from XML summary)

**Auto-generated InputSchema:**
```json
{
  "type": "object",
  "properties": {
    "number1": {"type": "number"},
    "number2": {"type": "number"}
  },
  "required": ["number1", "number2"]
}
```

**Pros:**
- ✅ Concise and clean
- ✅ Type-safe parameters
- ✅ Auto-generated schema (no mismatch)
- ✅ XML docs for description (standard C# practice)
- ✅ No extra attributes needed

**Cons:**
- ❌ Limited to simple parameter types
- ❌ Requires reflection or Source Generators
- ❌ Complex JSON structures still need explicit schema
- ❌ Parameter descriptions require custom attributes or InputSchema

---

## 🏗️ Design Decisions

### 1. Tool Name Resolution

**Rule:** Use first non-null value from:

1. `[McpTool("explicit_name")]` - Explicit name (positional parameter)
2. `Name = "..."` property in `[McpTool]`
3. Method name converted to snake_case
4. Method name as-is (if valid)

**Attribute Design:**

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class McpToolAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? InputSchema { get; set; }
}
```

**Examples:**

```csharp
// Explicit name (positional)
[McpTool("add_numbers")]
public double AddTool(...) { }
// → Tool name: "add_numbers"

// Explicit name (property)
[McpTool(Name = "add_numbers")]
public double AddTool(...) { }
// → Tool name: "add_numbers"

// Inferred from method name
[McpTool]
public double AddNumbersTool(...) { }
// → Tool name: "add_numbers_tool" (snake_case)

// Method name as-is (if valid)
[McpTool]
public double add_numbers(...) { }
// → Tool name: "add_numbers"
```

**Validation:** Tool name must still match `^[a-zA-Z0-9_-]{1,128}$`

---

### 2. Title and Description Resolution

**Title:**
1. `Title = "..."` in `[McpTool]`
2. Humanized method name (e.g., "AddNumbers" → "Add Numbers")
3. Tool name as fallback

**Description:**
1. `Description = "..."` in `[McpTool]`
2. XML documentation comment `<summary>`
3. Empty string as fallback

**Examples:**

```csharp
// Option 1: All in McpTool
[McpTool("add", 
    Title = "Add Numbers", 
    Description = "Adds two numbers")]
public double Add(...) { }

// Option 2: Positional + properties
[McpTool("add", 
    Description = "Adds two numbers")]  // Title inferred: "Add"
public double Add(...) { }

// Option 3: XML docs for description
[McpTool("add", Title = "Add Numbers")]
/// <summary>Adds two numbers</summary>
public double Add(...) { }

// Option 4: Everything inferred
[McpTool]
/// <summary>Adds two numbers</summary>
public double AddNumbersTool(...) { }
// → name: "add_numbers_tool", title: "Add Numbers Tool", description: "Adds two numbers"
```

**Note:** We do NOT add separate `[Description]` or `[Title]` attributes - everything is in `[McpTool]` for simplicity!

---

### 3. InputSchema Generation

**Strategy:** Generate JSON Schema from method parameters

#### Type Mapping

| C# Type | JSON Type | Required |
|---------|-----------|----------|
| `int`, `long` | `"integer"` | ✅ Yes |
| `double`, `float`, `decimal` | `"number"` | ✅ Yes |
| `string` | `"string"` | ✅ Yes |
| `bool` | `"boolean"` | ✅ Yes |
| `int?`, `double?`, etc. | Same + nullable | ❌ No |
| `string?` | `"string"` + nullable | ❌ No |
| `DateTime`, `DateTimeOffset` | `"string"` (format: "date-time") | ✅ Yes |
| `Guid` | `"string"` (format: "uuid") | ✅ Yes |
| `enum` | `"string"` (enum values) | ✅ Yes |
| `array`, `List<T>` | `"array"` | ✅ Yes |
| `object`, custom class | `"object"` | ✅ Yes |

#### Parameter Attributes

```csharp
[Description("Parameter description")]  // Adds description
[Range(1, 100)]                         // Adds min/max for numbers
[MinLength(3)]                          // Adds minLength for strings
[MaxLength(50)]                         // Adds maxLength for strings
[Pattern(@"^[A-Z]+$")]                  // Adds pattern for strings
```

**Example:**

```csharp
[McpTool]
public double Divide(
    [Description("Numerator")] double numerator,
    [Description("Denominator (cannot be zero)")] [Range(0.001, double.MaxValue)] double denominator
)
```

**Generated Schema:**
```json
{
  "type": "object",
  "properties": {
    "numerator": {
      "type": "number",
      "description": "Numerator"
    },
    "denominator": {
      "type": "number",
      "description": "Denominator (cannot be zero)",
      "minimum": 0.001,
      "maximum": 1.7976931348623157e+308
    }
  },
  "required": ["numerator", "denominator"]
}
```

---

### 4. Return Type Handling

**Strategy:** Wrap return value in JSON-RPC response automatically

| Return Type | Behavior |
|-------------|----------|
| `JsonRpcMessage` | Return as-is (explicit control) |
| `Task<JsonRpcMessage>` | Await and return |
| Any other type `T` | Wrap in `ToolResponse.Success(id, value)` |
| `Task<T>` | Await and wrap |
| `void` | Return success with `null` result |
| `Task` | Await and return success with `null` |

**Examples:**

```csharp
// Explicit (current) - full control
[McpTool("add")]
public JsonRpcMessage Add(JsonRpcMessage request)
{
    return ToolResponse.Success(request.Id, result);
}

// Magic - auto-wrap
[McpTool("add")]
public double Add(double a, double b)
{
    return a + b;  // Auto-wrapped in JsonRpcMessage
}

// Magic async
[McpTool("add")]
public async Task<double> Add(double a, double b)
{
    await Task.Delay(10);
    return a + b;  // Auto-wrapped
}
```

---

### 5. Dependency Injection Support

**Rule:** Mix parameters and DI services

**Examples:**

```csharp
// Only parameters (new style)
[McpTool]
public double Add(double a, double b)
{
    return a + b;
}

// Parameters + DI (hybrid)
[McpTool]
public async Task<User> GetUser(
    int userId,                    // From JSON params
    IUserRepository repo,          // From DI
    ILogger<MyTool> logger         // From DI
)
{
    logger.LogInformation("Fetching user {UserId}", userId);
    return await repo.GetByIdAsync(userId);
}

// DI only (current style with JsonRpcMessage)
[McpTool("ping")]
public JsonRpcMessage Ping(
    JsonRpcMessage request,
    ILogger<MyTool> logger
)
{
    logger.LogInformation("Ping received");
    return ToolResponse.Success(request.Id, new { message = "Pong" });
}
```

**Detection Logic:**

1. Check if parameter type is registered in DI container
2. If yes → inject from DI
3. If no → expect from JSON params

**Special parameters (never from JSON):**
- `JsonRpcMessage` - The request itself
- `ToolConnector` - For streaming tools
- `CancellationToken` - Cancellation support
- Any type registered in DI

---

## 🛠️ Implementation Plan

### Phase 1: Attribute Infrastructure (Week 1)

**Tasks:**
1. Update `[McpTool]` to make `name` parameter optional (nullable)
2. Keep all properties (`Name`, `Title`, `Description`, `InputSchema`) as optional
3. Add XML documentation parsing support
4. Add parameter validation attributes (`[Range]`, `[MinLength]`, etc.) - OPTIONAL

**Files to modify:**
- `Mcp.Gateway.Tools/McpToolAttribute.cs` (modify)

**Updated Attribute:**
```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class McpToolAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? InputSchema { get; set; }
}
```

**Note:** No new attributes needed! Everything is in `[McpTool]` or XML docs.

---

### Phase 2: Schema Generation (Week 2)

**Tasks:**
1. Create `JsonSchemaGenerator` class
2. Implement type → JSON Schema mapping
3. Handle parameter attributes
4. Generate required/optional fields
5. Unit tests for schema generation

**Files to create:**
- `Mcp.Gateway.Tools/JsonSchemaGenerator.cs` (new)
- `Mcp.Gateway.Tests/JsonSchemaGeneratorTests.cs` (new)

**API:**
```csharp
public static class JsonSchemaGenerator
{
    public static string GenerateFromMethod(MethodInfo method);
    public static string GenerateFromParameters(ParameterInfo[] parameters);
}
```

**Example usage:**
```csharp
var method = typeof(Calculator).GetMethod("Add");
var schema = JsonSchemaGenerator.GenerateFromMethod(method);
// Returns: {"type":"object","properties":{...},"required":[...]}
```

---

### Phase 3: Parameter Binding (Week 3)

**Tasks:**
1. Create parameter binder
2. Extract parameters from `JsonRpcMessage.Params`
3. Inject DI services
4. Handle nullable parameters
5. Validate parameter values
6. Unit tests for binding

**Files to create/modify:**
- `Mcp.Gateway.Tools/ParameterBinder.cs` (new)
- `Mcp.Gateway.Tools/ToolInvoker.cs` (modify)

**API:**
```csharp
public class ParameterBinder
{
    public object?[] BindParameters(
        MethodInfo method,
        JsonRpcMessage request,
        IServiceProvider services);
}
```

**Logic:**
```csharp
foreach (var param in method.GetParameters())
{
    if (IsSpecialParameter(param))
    {
        // JsonRpcMessage, ToolConnector, CancellationToken
        args[i] = GetSpecialParameter(param, request, connector, ct);
    }
    else if (services.GetService(param.ParameterType) is object service)
    {
        // DI service
        args[i] = service;
    }
    else
    {
        // JSON parameter
        args[i] = GetFromJson(request.Params, param);
    }
}
```

---

### Phase 4: Return Value Wrapping (Week 3)

**Tasks:**
1. Detect return type
2. Auto-wrap non-JsonRpcMessage returns
3. Handle async returns
4. Unit tests

**Files to modify:**
- `Mcp.Gateway.Tools/ToolInvoker.cs`

**Logic:**
```csharp
var result = method.Invoke(instance, args);

// Handle async
if (result is Task task)
{
    await task;
    result = GetTaskResult(task);
}

// Wrap if needed
if (result is JsonRpcMessage jsonRpc)
{
    return jsonRpc;  // Already wrapped
}
else
{
    return ToolResponse.Success(request.Id, result);  // Auto-wrap
}
```

---

### Phase 5: Tool Discovery Update (Week 4)

**Tasks:**
1. Update `ToolService` to detect magic tools
2. Generate schema for magic tools
3. Infer tool name from method name
4. Extract title/description from attributes
5. Update tool metadata
6. Integration tests

**Files to modify:**
- `Mcp.Gateway.Tools/ToolService.cs`

**Logic:**
```csharp
var toolAttr = method.GetCustomAttribute<McpToolAttribute>();

// Infer name
var name = toolAttr.Name 
    ?? ConvertToSnakeCase(method.Name)
    ?? throw new Exception("Invalid tool name");

// Infer description
var description = toolAttr.Description
    ?? method.GetCustomAttribute<DescriptionAttribute>()?.Description
    ?? GetXmlDocSummary(method)
    ?? string.Empty;

// Generate or use explicit schema
var schema = toolAttr.InputSchema
    ?? JsonSchemaGenerator.GenerateFromMethod(method);
```

---

### Phase 6: Testing & Documentation (Week 5)

**Tasks:**
1. Unit tests for each component
2. Integration tests (HTTP, WS, stdio)
3. Update README with examples
4. Migration guide
5. Performance benchmarks
6. Update CHANGELOG

**Test coverage:**
- ✅ Simple magic tools (no params)
- ✅ Magic tools with parameters
- ✅ Magic tools with DI
- ✅ Magic tools with nullable parameters
- ✅ Hybrid tools (params + DI)
- ✅ Explicit tools (current style)
- ✅ Return type handling (all variants)
- ✅ Schema generation (all types)

---

## 📚 Examples

### Example 1: Simple Magic Tool

```csharp
[McpTool]
/// <summary>Adds two numbers</summary>
public double Add(double a, double b)
{
    return a + b;
}
```

**Auto-generated:**
- Name: `"add"`
- Title: `"Add"`
- Description: `"Adds two numbers"` (from XML summary)
- InputSchema: `{"type":"object","properties":{"a":{...},"b":{...}},"required":["a","b"]}`

---

### Example 2: Magic Tool with Optional Parameters

```csharp
[McpTool("greet")]
/// <summary>Greets a user by name</summary>
public string Greet(string name, string? prefix = null)
{
    return prefix != null ? $"{prefix} {name}!" : $"Hello, {name}!";
}
```

**Auto-generated:**
- Required: `["name"]`
- Optional: `["prefix"]`

---

### Example 3: Magic Tool with DI

```csharp
[McpTool]
/// <summary>Fetches user by ID</summary>
public async Task<User> GetUser(
    int userId,
    IUserRepository repo,           // From DI
    ILogger<MyTool> logger          // From DI
)
{
    logger.LogInformation("Fetching user {UserId}", userId);
    return await repo.GetByIdAsync(userId);
}
```

**Auto-generated schema:**
- Only `userId` in schema (DI services excluded)

---

### Example 4: Hybrid (Explicit + Magic)

```csharp
[McpTool("complex_query",
    Description = "Executes a complex database query",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":[
            {
                ""filters"":{""type"":""array"",...}
            },
            {
                ""options"":{""type"":""object"",...}
            }
        ]
    }")]
public async Task<QueryResult> ComplexQuery(
    JsonRpcMessage request,
    IDatabase db                    // From DI
)
{
    var args = request.GetParams<ComplexQueryRequest>();
    return await db.ExecuteAsync(args.Filters, args.Options);
}
```

**Uses explicit schema** (too complex for auto-generation)

---

### Example 5: Everything Explicit

```csharp
[McpTool("add_numbers",
    Title = "Add Numbers",
    Description = "Adds two numbers and return result. Example: 5 + 3 = 8",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":
        {
            ""number1"":{""type"":""number"",""description"":""First number""},
            ""number2"":{""type"":""number"",""description"":""Second number""}
        },
        ""required"":[""number1"",""number2""]
    }")]
public JsonRpcMessage AddTool(JsonRpcMessage request)
{
    var args = request.GetParams<NumbersRequest>();
    return ToolResponse.Success(request.Id, new { result = args.Number1 + args.Number2 });
}
```

**Current style** - Full control, still supported!
