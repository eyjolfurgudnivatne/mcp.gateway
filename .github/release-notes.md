## [1.8.1] - 2026-01-03

**✨ Typed Returns & Auto-Output Schema**

This release introduces strongly-typed return values for tools, enabling automatic `outputSchema` generation and simplified `structuredContent` responses.

### Added
- **TypedJsonRpc<T> as Return Type**
  - Tools can now return `TypedJsonRpc<TResponse>` (or `Task<TypedJsonRpc<TResponse>>`).
  - Provides compile-time type safety for tool outputs.
  - Example: `public TypedJsonRpc<AddResponse> Add(...)`

- **Automatic Output Schema Generation**
  - If `OutputSchema` is not explicitly set, it is automatically generated from the `TypedJsonRpc<T>` return type.
  - Uses the same robust schema generator as input parameters (supports `[Description]`, `[JsonPropertyName]`, enums, etc.).
  - Ensures clients (and LLMs) know exactly what structure to expect from the tool.

- **Structured Content Support**
  - `TypedJsonRpc<T>.Success(id, result)` helper method.
  - Automatically serializes the result to `structuredContent` (JSON object) AND `content` (text JSON) for backward compatibility.
  - Simplifies tool implementation by removing manual JSON serialization boilerplate.
