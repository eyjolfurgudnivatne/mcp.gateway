# MCP Gateway v1.4.0 – MCP Prompts Support

## Summary

v1.4.0 introduces first-class **MCP Prompts** support alongside existing tools, including:

- Attribute-based prompt registration via `McpPromptAttribute`
- A dedicated prompt model and response types in `Mcp.Gateway.Tools`
- A new example server `PromptMcpServer` + tests
- Groundwork for future MCP concepts (Resources, extended server features)

---

## Highlights

### New: MCP Prompts

- Added `[McpPrompt]` attribute in `Mcp.Gateway.Tools`:
  - `McpPromptAttribute(string? name = null)` with:
    - `Name` – optional; auto-generated from method name when null (same snake_case logic as tools).
    - `Title` – optional, human-friendly title (falls back to humanized name when omitted).
    - `Description` – optional, shown to MCP clients in `prompts/list`.
  - Used to mark methods as **prompts**, separate from `[McpTool]`.
- Introduced dedicated prompt response models in `Mcp.Gateway.Tools`:
  - `PromptResponse` – wraps the MCP prompt result:
    - `name` – name of this prompt.
    - `description` – description of this prompt.
    - `messages` – list of messages for the LLM to handle.
    - `arguments` – list of arguments for the LLM to use with messages.
  - `PromptMessage` – a single prompt message:
    - `role` – e.g. `system`, `user`, `assistant`.
    - `content` – prompt text.
  - These models match the MCP `prompts/get` result shape and can be reused by all prompt implementations.
  - MCP prompt methods implemented:
    - `prompts/list`
    - `prompts/get`
    - `initialize` prompts capability flag

> Note: Prompts are **not** streamed in v1.4.0; they are returned as regular JSON-RPC responses with a fixed
> `messages` array.

### New: Prompt Example Server

- Added `Examples/PromptMcpServer` showcasing prompt usage:
  - `Prompts/SimplePrompt.cs` – minimal prompt example using `JsonRpcMessage`:

    ```csharp
    [McpPrompt(Description = "Report to Santa Claus")]
    public JsonRpcMessage SantaReportPrompt(JsonRpcMessage request)
    {
        return ToolResponse.Success(
            request.Id,
            new PromptResponse(
                Name: "santa_report_prompt",
                Description: "A prompt that reports to Santa Claus",
                Messages: [
                    new(
                        PromptRole.System,
                        "You are a very helpful assistant for Santa Claus."),
                    new (
                        PromptRole.User,
                        "Send a letter to Santa Claus and tell him that {{name}} has been {{behavior}}.")
                ],
                Arguments: new {
                    name = new {
                        type = "string",
                        description = "Name of the child"
                    },
                    behavior = new {
                        type = "string",
                        description = "Behavior of the child (e.g., Good, Naughty)",
                        @enum = new[] { "Good", "Naughty" }
                    }
                }
            ));
    }
    ```

  - Demonstrates how prompts and `JsonRpcMessage` integrate cleanly with existing infrastructure.

- Added `Examples/PromptMcpServerTests`:
  - `Prompts/SimplePromptTests.cs` verifies that the prompt responds with the expected `name`, `description`, `messages` and `arguments` structure and uses
    the `PromptResponse`/`PromptMessage` models correctly.

> The example server is intended as a reference implementation for MCP Prompts, similar to how
> `CalculatorMcpServer` demonstrates tools.

---

## Behaviour & Compatibility

- Prompts are a **new** MCP surface area and do **not** change existing behaviour:
  - Tools (`[McpTool]`, `tools/list`, `tools/call`) remain unchanged.
  - Wire format for tools and streaming is unchanged.
- Prompt types live in `Mcp.Gateway.Tools` and reuse existing JSON-RPC infrastructure:
  - Prompt methods still return `JsonRpcMessage` via `ToolResponse.Success(...)`.
  - `PromptResponse` and `PromptMessage` are regular record types serialized by the existing `JsonOptions`.
- Prompt roles are represented as strings on the wire (e.g. `"system"`, `"user"`, `"assistant"`), keeping
  compatibility with MCP/LLM clients.
- `initialize` now includes a `prompts` capability flag when the server has registered prompts (mirroring how tools capabilities are surfaced). MCP clients can use this to detect prompt support.

> v1.4.0 delivers a complete first version of MCP Prompts: attribute, response model, auto-discovery, prompts/list and prompts/get wired into ToolInvoker, and initialize-capabilities when prompts are present. 

---

## Testing

- New example tests in `Examples/PromptMcpServerTests`:
  - Validate that prompt endpoints:
    - Accept both raw `JsonRpcMessage` and `TypedJsonRpc<T>` request models, depending on the example.
    - Produce `PromptResponse` objects with correct `messages` shape.
- Existing `Mcp.Gateway.Tests` and example tests continue to pass:
  - No regressions to tools, transports (HTTP/WS/SSE/stdio), or streaming behaviour.

---

## Upgrade Notes

- v1.4.0 is backward compatible with v1.3.0 and v1.2.0.
- No changes are required for existing tool implementations.
- To start using prompts:
  1. Add a new class in your server with methods annotated by `[McpPrompt]`.
  2. Use `TypedJsonRpc<TRequest>` if you want a strongly-typed request model.
  3. Return `ToolResponse.Success(request.Id, PromptResponse)` with a `messages` array that MCP clients can send to
     their LLM.
- Prompt roles are free-form strings at this stage; stick to `system`, `user`, and `assistant` to align with common
  LLM conventions.
