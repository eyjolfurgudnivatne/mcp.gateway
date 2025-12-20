---
layout: mcp-default
title: Prompt Server Example
description: Reusable prompt templates with dynamic arguments
breadcrumbs:
  - title: Home
    url: /
  - title: Examples
    url: /examples/prompt/
  - title: Prompt Server
    url: /examples/prompt/
prev: false
next: false
toc: true
---

# Prompt Server Example

**Version:** v1.4.0+  
**Features:** Prompts, System/User Messages, Dynamic Arguments  
**Complexity:** Beginner

## Overview

A simple MCP server demonstrating prompt templates with:
- ✅ **System messages** - Define AI behavior and context
- ✅ **User messages** - Templated prompts with placeholders
- ✅ **Dynamic arguments** - Inject values at runtime
- ✅ **Argument schemas** - Type-safe parameter definitions
- ✅ **Enum arguments** - Constrained values (e.g., "Good", "Naughty")

Perfect for:
- Learning prompt template patterns
- Understanding system vs user messages
- Creating reusable AI prompts
- Standardizing prompt formats

## Quick Start

### Run the Server

```bash
cd Examples/PromptMcpServer
dotnet run
```

Server starts at: `http://localhost:5000`

### Test Prompts

```bash
# List all prompts
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "prompts/list",
    "id": 1
  }'

# Get a prompt with arguments
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "prompts/get",
    "params": {
      "name": "santa_report_prompt",
      "arguments": {
        "name": "Alice",
        "behavior": "Good"
      }
    },
    "id": 2
  }'
```

## Available Prompts

### santa_report_prompt

A whimsical prompt for sending reports to Santa Claus.

**Arguments:**
- `name` (string, required) - Name of the child
- `behavior` (enum, required) - Behavior: "Good" or "Naughty"

**System message:**
> "You are a very helpful assistant for Santa Claus."

**User message template:**
> "Send a letter to Santa Claus and tell him that {{name}} has been {{behavior}}."

**Example usage:**
```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "prompts/get",
    "params": {
      "name": "santa_report_prompt",
      "arguments": {
        "name": "Bob",
        "behavior": "Naughty"
      }
    },
    "id": 1
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "name": "santa_report_prompt",
    "description": "A prompt that reports to Santa Claus",
    "messages": [
      {
        "role": "system",
        "content": "You are a very helpful assistant for Santa Claus."
      },
      {
        "role": "user",
        "content": "Send a letter to Santa Claus and tell him that Bob has been Naughty."
      }
    ],
    "arguments": {
      "name": {
        "type": "string",
        "description": "Name of the child"
      },
      "behavior": {
        "type": "string",
        "description": "Behavior of the child (e.g., Good, Naughty)",
        "enum": ["Good", "Naughty"]
      }
    }
  },
  "id": 1
}
```

## Code Implementation

### Prompt Definition

```csharp
using Mcp.Gateway.Tools;

public class SimplePrompt
{
    [McpPrompt(Description = "Report to Santa Claus")]
    public JsonRpcMessage SantaReportPrompt(JsonRpcMessage request)
    {
        return ToolResponse.Success(
            request.Id,
            new PromptResponse(
                Name: "santa_report_prompt",
                Description: "A prompt that reports to Santa Claus",
                Messages: new[]
                {
                    new PromptMessage(
                        PromptRole.System,
                        "You are a very helpful assistant for Santa Claus."),
                    new PromptMessage(
                        PromptRole.User,
                        "Send a letter to Santa Claus and tell him that {{name}} has been {{behavior}}.")
                },
                Arguments: new
                {
                    name = new
                    {
                        type = "string",
                        description = "Name of the child"
                    },
                    behavior = new
                    {
                        type = "string",
                        description = "Behavior of the child (e.g., Good, Naughty)",
                        @enum = new[] { "Good", "Naughty" }
                    }
                }
            ));
    }
}
```

### Key Components

**1. McpPrompt Attribute**
```csharp
[McpPrompt(Description = "Report to Santa Claus")]
```
- Marks the method as a prompt
- Optional description for AI clients

**2. PromptResponse Structure**
```csharp
new PromptResponse(
    Name: "santa_report_prompt",        // Prompt identifier
    Description: "...",                 // Human-readable description
    Messages: [...],                    // System and user messages
    Arguments: {...}                    // Argument schema
)
```

**3. System Message**
```csharp
new PromptMessage(
    PromptRole.System,
    "You are a very helpful assistant for Santa Claus.")
```
- Sets AI behavior and context
- Always processed first by LLMs

**4. User Message with Placeholders**
```csharp
new PromptMessage(
    PromptRole.User,
    "Send a letter to Santa Claus and tell him that {{name}} has been {{behavior}}.")
```
- Template with `{{name}}` and `{{behavior}}` placeholders
- Client replaces placeholders with actual values

**5. Argument Schema**
```csharp
Arguments: new
{
    name = new
    {
        type = "string",
        description = "Name of the child"
    },
    behavior = new
    {
        type = "string",
        description = "Behavior of the child (e.g., Good, Naughty)",
        @enum = new[] { "Good", "Naughty" }
    }
}
```
- JSON Schema format
- Describes expected arguments
- `@enum` restricts to specific values

## Prompt + Tool Combination

The example also includes a tool in the same class:

```csharp
public record LetterToSantaRequest(
    [property: Description("Name of the child")] 
    string Name,
    [property: Description("Behavior of the child")] 
    BehaviorEnum Behavior,
    [property: Description("Email address to Santa Claus")] 
    string? SantaEmailAddress);

public enum BehaviorEnum
{
    Good,
    Naughty
}

[McpTool(Description = "Send letter to Santa Claus")]
public JsonRpcMessage LetterToSanta(TypedJsonRpc<LetterToSantaRequest> request)
{
    var args = request.GetParams()
        ?? throw new ToolInvalidParamsException(
            "Parameters 'name' and 'behavior' are required.");

    // Send letter logic here...
    return ToolResponse.Success(request.Id, new { sent = true });
}
```

**Pattern:** Prompts and tools can coexist in the same class!
- Prompt: Generates AI instructions
- Tool: Executes actual operations

## Testing Prompts

### Test prompts/list

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "prompts/list",
    "id": 1
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "prompts": [
      {
        "name": "santa_report_prompt",
        "description": "Report to Santa Claus",
        "arguments": [
          {
            "name": "name",
            "description": "Name of the child",
            "required": true
          },
          {
            "name": "behavior",
            "description": "Behavior of the child (e.g., Good, Naughty)",
            "required": true
          }
        ]
      }
    ]
  },
  "id": 1
}
```

### Test prompts/get

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "prompts/get",
    "params": {
      "name": "santa_report_prompt",
      "arguments": {
        "name": "Charlie",
        "behavior": "Good"
      }
    },
    "id": 2
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "name": "santa_report_prompt",
    "description": "A prompt that reports to Santa Claus",
    "messages": [
      {
        "role": "system",
        "content": "You are a very helpful assistant for Santa Claus."
      },
      {
        "role": "user",
        "content": "Send a letter to Santa Claus and tell him that Charlie has been Good."
      }
    ],
    "arguments": {
      "name": {
        "type": "string",
        "description": "Name of the child"
      },
      "behavior": {
        "type": "string",
        "description": "Behavior of the child (e.g., Good, Naughty)",
        "enum": ["Good", "Naughty"]
      }
    }
  },
  "id": 2
}
```

## JavaScript Client Example

```javascript
// 1. List all prompts
async function listPrompts() {
  const response = await fetch('http://localhost:5000/mcp', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'MCP-Protocol-Version': '2025-11-25'
    },
    body: JSON.stringify({
      jsonrpc: '2.0',
      method: 'prompts/list',
      id: 1
    })
  });
  
  const result = await response.json();
  console.log('Available prompts:', result.result.prompts);
  return result.result.prompts;
}

// 2. Get prompt with arguments
async function getPrompt(name, args) {
  const response = await fetch('http://localhost:5000/mcp', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'MCP-Protocol-Version': '2025-11-25'
    },
    body: JSON.stringify({
      jsonrpc: '2.0',
      method: 'prompts/get',
      params: { name, arguments: args },
      id: 2
    })
  });
  
  const result = await response.json();
  return result.result;
}

// 3. Use the prompt
const prompt = await getPrompt('santa_report_prompt', {
  name: 'Alice',
  behavior: 'Good'
});

console.log('System message:', prompt.messages[0].content);
console.log('User message:', prompt.messages[1].content);

// 4. Send to LLM (e.g., OpenAI)
const completion = await openai.chat.completions.create({
  model: 'gpt-4',
  messages: prompt.messages.map(m => ({
    role: m.role,
    content: m.content
  }))
});

console.log('AI response:', completion.choices[0].message.content);
```

## Integration Tests

The PromptMcpServerTests project includes comprehensive tests:

```bash
cd Examples/PromptMcpServerTests
dotnet test
```

**Test coverage:**
- ✅ prompts/list returns all prompts
- ✅ prompts/get with valid arguments
- ✅ prompts/get with invalid arguments
- ✅ Argument schema validation
- ✅ System and user messages structure
- ✅ Enum values in schema

## Common Prompt Patterns

### 1. Code Review Prompt

```csharp
[McpPrompt("code_review")]
public JsonRpcMessage CodeReview(JsonRpcMessage request)
{
    return ToolResponse.Success(
        request.Id,
        new PromptResponse(
            Name: "code_review",
            Description: "Review code for best practices",
            Messages: new[]
            {
                new PromptMessage(PromptRole.System, 
                    "You are an experienced code reviewer."),
                new PromptMessage(PromptRole.User, 
                    "Review this {{language}} code:\n\n{{code}}\n\nFocus on: {{focus}}")
            },
            Arguments: new
            {
                code = new { type = "string", description = "Code to review" },
                language = new { type = "string", description = "Programming language" },
                focus = new { type = "string", description = "Review focus area" }
            }
        ));
}
```

### 2. SQL Query Builder

```csharp
[McpPrompt("sql_builder")]
public JsonRpcMessage SqlBuilder(JsonRpcMessage request)
{
    return ToolResponse.Success(
        request.Id,
        new PromptResponse(
            Name: "sql_builder",
            Description: "Generate SQL queries",
            Messages: new[]
            {
                new PromptMessage(PromptRole.System, 
                    "You are a SQL expert. Generate efficient, safe queries."),
                new PromptMessage(PromptRole.User, 
                    "Create a {{operation}} query for table '{{table}}' with conditions: {{conditions}}")
            },
            Arguments: new
            {
                operation = new 
                { 
                    type = "string", 
                    @enum = new[] { "SELECT", "INSERT", "UPDATE", "DELETE" }
                },
                table = new { type = "string" },
                conditions = new { type = "string" }
            }
        ));
}
```

### 3. Documentation Generator

```csharp
[McpPrompt("doc_generator")]
public JsonRpcMessage DocGenerator(JsonRpcMessage request)
{
    return ToolResponse.Success(
        request.Id,
        new PromptResponse(
            Name: "doc_generator",
            Description: "Generate documentation from code",
            Messages: new[]
            {
                new PromptMessage(PromptRole.System, 
                    "You are a technical writer. Create clear, comprehensive documentation."),
                new PromptMessage(PromptRole.User, 
                    "Document this {{type}}:\n\n{{code}}\n\nInclude: {{sections}}")
            },
            Arguments: new
            {
                type = new 
                { 
                    type = "string", 
                    @enum = new[] { "function", "class", "module", "API" }
                },
                code = new { type = "string" },
                sections = new { type = "string", description = "Comma-separated sections to include" }
            }
        ));
}
```

## Best Practices

### 1. Clear System Messages

```csharp
// ✅ GOOD - Specific role and behavior
new PromptMessage(PromptRole.System, 
    "You are an experienced Python developer. Focus on PEP 8 style and performance.")

// ❌ BAD - Too vague
new PromptMessage(PromptRole.System, "You are helpful.")
```

### 2. Descriptive Placeholders

```csharp
// ✅ GOOD - Clear what to insert
"Analyze this {{error_message}} and suggest {{fix_type}} fixes."

// ❌ BAD - Unclear
"Analyze this {{text}} and do {{thing}}."
```

### 3. Argument Descriptions

```csharp
// ✅ GOOD - Clear description with example
new
{
    type = "string",
    description = "Error message from stack trace (e.g., 'NullReferenceException')"
}

// ❌ BAD - No context
new { type = "string", description = "Error" }
```

### 4. Use Enums for Constrained Values

```csharp
// ✅ GOOD - Limited to valid values
behavior = new
{
    type = "string",
    @enum = new[] { "Good", "Naughty" }
}

// ⚠️ OK - But allows any string
behavior = new
{
    type = "string",
    description = "Either 'Good' or 'Naughty'"
}
```

### 5. Multi-Message Structure

```csharp
// ✅ GOOD - System + User context
Messages: new[]
{
    new PromptMessage(PromptRole.System, "You are a SQL expert."),
    new PromptMessage(PromptRole.User, "Generate query for: {{request}}")
}

// ⚠️ OK - But less context
Messages: new[]
{
    new PromptMessage(PromptRole.User, "Generate SQL query for: {{request}}")
}
```

## Use Cases

### 1. Standardized AI Instructions

Create consistent prompts across your organization:
- Code reviews with company standards
- Documentation following style guide
- SQL queries with security policies

### 2. Template Library

Build reusable prompt templates:
- Marketing copy generation
- Technical support responses
- Data analysis instructions

### 3. Multi-Language Support

Define prompts once, use in multiple languages:
```csharp
"Translate '{{text}}' from {{source_lang}} to {{target_lang}}"
```

### 4. Context-Aware Prompts

Inject runtime context:
```csharp
"Using project context {{project_name}}, review this {{file_type}} file: {{content}}"
```

## See Also

- [Prompts API](/mcp.gateway/api/prompts/) - Complete Prompts API reference
- [Tools API](/mcp.gateway/api/tools/) - Combine prompts with tools
- [Getting Started](/mcp.gateway/getting-started/index/) - Quick start guide
