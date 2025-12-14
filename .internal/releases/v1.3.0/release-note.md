# MCP Gateway v1.3.0 – TypedJsonRpc and Schema Generation

## Summary

- New `TypedJsonRpc<T>` helper for strongly-typed tool implementations
- Optional JSON Schema auto-generation for `TypedJsonRpc<T>` tools
- Extended MCP `tools/list` to surface generated schemas
- Enum and description support based on C# attributes

---

## Highlights

### New

- `TypedJsonRpc<T>` – thin wrapper over `JsonRpcMessage` for tools
  - Provides `Id`, `IdAsString`, `Method`, and `Inner` (`JsonRpcMessage`)
  - Adds `GetParams()` returning `T` without needing `GetParams<T>()` at call-site
  - Used only as a **convenience API** for tool authors; protocol wire format is unchanged.
- Tool discovery extended with parameter-type awareness
  - `ToolService` now records the first parameter type (e.g. `TypedJsonRpc<AddNumbersRequestTyped>`) for each tool.
  - `ToolInvoker` uses this metadata to construct `TypedJsonRpc<T>` instances at runtime when invoking tools.

### Schema Generator (opt-in)

- New internal `ToolSchemaGenerator` for tools using `TypedJsonRpc<T>` with no explicit `InputSchema`:
  - Only runs when **both** of these are true:
    - First parameter is `TypedJsonRpc<TParams>`
    - `[McpTool]` has `InputSchema == null` or whitespace.
  - Generates a minimal, valid MCP tool schema from `TParams`:
    - Root: `{ "type": "object", "properties": { ... }, "required": [ ... ] }`
    - `properties` from public instance properties on `TParams`.
    - `required` contains all non-nullable properties (nullable → optional).
- JSON type mapping from C#:
  - `string` → `"type": "string"`
  - `Guid` → `"type": "string", "format": "uuid"`
  - `DateTime` / `DateTimeOffset` → `"type": "string", "format": "date-time"`
  - `bool` → `"type": "boolean"`
  - `int`, `long`, `short`, `byte` → `"type": "integer"`
  - `float`, `double`, `decimal` → `"type": "number"`
  - arrays / `IEnumerable<T>` (except `string`) → `"type": "array"`
  - other types / nested records → `"type": "object"` (no recursion in v1.3.0).
- Enum support:
  - C# enums are represented as string-based enums in JSON Schema:
    - `enum Status { Active, Disabled }` → `{ "type": "string", "enum": ["Active", "Disabled"] }`.
- Description support:
  - `[Description("...")]` on record properties (with `[property: Description]`) is mapped to `"description"` in JSON Schema:
    - `[property: Description("First number to add")]` → `"description": "First number to add"`.
  - `JsonPropertyName` is respected for property names in the generated schema.

---

## Behaviour & Compatibility

- `InputSchema` remains the single source of truth when provided:
  - If a tool has `InputSchema` set on `[McpTool]`, that schema is used as-is.
  - The schema generator is **never** applied in that case.
- For tools using `TypedJsonRpc<TParams>` and **no** `InputSchema`:
  - `tools/list` will now include an auto-generated `inputSchema` based on `TParams`.
- For all existing tools using `JsonRpcMessage` and explicit `InputSchema`:
  - Behaviour is unchanged.
  - Wire format, tool names, and schemas are fully backward compatible.
- `ToolInvoker` and `ToolService` maintain the same JSON-RPC and MCP semantics:
  - No changes to `JsonRpcMessage` layout.
  - No changes to MCP protocol methods (`initialize`, `tools/list`, `tools/call`).

---

## Examples

### Example: TypedJsonRpc tool with explicit schema (unchanged)

```csharp
[McpTool("add_numbers_typed",
    Title = "Add Numbers (typed)",
    Description = "Adds two numbers using TypedJsonRpc proof-of-concept. Uses same schema as add_numbers.",
    InputSchema = @"{
        \"type\":\"object\",
        \"properties\":{
            \"number1\":{\"type\":\"number\",\"description\":\"First number to add\"},
            \"number2\":{\"type\":\"number\",\"description\":\"Second number to add\"}
        },
        \"required\":[\"number1\",\"number2\"]
    }")]
public JsonRpcMessage AddNumbersToolTyped(TypedJsonRpc<AddNumbersRequestTyped> request)
{
    var args = request.GetParams()
        ?? throw new ToolInvalidParamsException(
            "Parameters 'number1' and 'number2' are required and must be numbers.");

    return ToolResponse.Success(
        request.Id,
        new AddNumbersResponse(args.Number1 + args.Number2));
}
```

- Behaviour: identical to pre-v1.3.0. Schema comes from `InputSchema`, not from generator.

### Example: TypedJsonRpc tool **without** schema (auto-generated)

```csharp
public sealed record AddNumbersRequestTyped(
    [property: JsonPropertyName("number1")]
    [property: Description("First number to add")] double Number1,

    [property: JsonPropertyName("number2")]
    [property: Description("Second number to add")] double Number2);

[McpTool("add_numbers_typed_ii",
    Title = "Add Numbers (typed)",
    Description = "Adds two numbers using TypedJsonRpc with auto-generated schema.")]
public JsonRpcMessage AddNumbersToolTypedII(TypedJsonRpc<AddNumbersRequestTyped> request)
{
    var args = request.GetParams()
        ?? throw new ToolInvalidParamsException(
            "Parameters 'number1' and 'number2' are required and must be numbers.");

    return ToolResponse.Success(
        request.Id,
        new AddNumbersResponse(args.Number1 + args.Number2));
}
```

- Generated schema (simplified):

```json
{
  "type": "object",
  "properties": {
    "number1": {
      "type": "number",
      "description": "First number to add"
    },
    "number2": {
      "type": "number",
      "description": "Second number to add"
    }
  },
  "required": ["number1", "number2"]
}
```

---

## Testing

- New tests in `Examples/CalculatorMcpServerTests`:
  - `ToolsList_ReturnsAllTools` – verifies explicit schema for `add_numbers_typed`.
  - `ToolsListII_ReturnsAllTools` – verifies auto-generated schema for `add_numbers_typed_ii`:
    - Confirms `type: "object"`.
    - Confirms `properties.number1/number2` with `type: "number"`.
    - Confirms `description` values come from `[Description]` attributes.
    - Confirms both properties are listed in `required`.
- Existing MCP Gateway tests remain unchanged and pass with the new behaviour.

---

## Upgrade Notes

- No breaking changes from v1.2.0.
- Existing tools with explicit `InputSchema` are unaffected.
- To opt into schema generation for a new tool:
  1. Use `TypedJsonRpc<TParams>` as the first parameter.
  2. Omit `InputSchema` on `[McpTool]`.
  3. Define a record `TParams` with `JsonPropertyName` and optional `[property: Description]` attributes.
- For tooling that parses `tools/list`, no changes are required; generated schemas follow the same shape as hand-authored ones.
