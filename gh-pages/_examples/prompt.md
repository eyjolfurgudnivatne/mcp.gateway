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
> $"Send a letter to Santa Claus and tell him that {args.Name} has behaved {args.Behavior}."

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
    "description": "A prompt that reports to Santa Claus",
    "messages": [
      {
        "role": "system",
        "content": {
          "type": "text",
          "text": "You are a very helpful assistant for Santa Claus."
        }
      },
      {
        "role": "user",
        "content": {
          "type": "text",
          "text": "Send a letter to Santa Claus and tell him that Bob has behaved Naughty."
        }
      }
    ]
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
    public record SantaReportPromptRequest(
        [property: JsonPropertyName("name")]
        [property: DisplayName("Child's Name")] // title
        [property: Description("Name of the child")] string Name,

        [property: JsonPropertyName("behavior")]
        [property: DisplayName("Child's Behavior")] // title
        [property: Description("Behavior of the child (e.g., Good, Naughty)")] BehaviorEnum Behavior);

    public enum BehaviorEnum
    {
        Good,
        Naughty
    }

    [McpPrompt(Description = "Report to Santa Claus")]
    public JsonRpcMessage SantaReportPrompt(TypedJsonRpc<SantaReportPromptRequest> request)
    {
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'name' and 'behavior' are required and must be strings.");

        return ToolResponse.Success(
            request.Id,
            new PromptResponse
            {
                Description = "A prompt that reports to Santa Claus",
                Messages = [
                    new(
                        PromptRole.System,
                        new TextContent {
                            Text = "You are a very helpful assistant for Santa Claus."
                        }),
                    new (
                        PromptRole.User,
                        new TextContent {
                            Text = $"Send a letter to Santa Claus and tell him that {args.Name} has behaved {args.Behavior}."
                        })
                ]
            }
        );
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
new PromptResponse
{
    Description = "...",                 // Human-readable description
    Messages = [...],                    // System and user messages
}
```

**3. System Message**
```csharp
new PromptMessage(
    PromptRole.System,
    new TextContent {
      Text = "You are a very helpful assistant for Santa Claus."
    })
```
- Sets AI behavior and context
- Always processed first by LLMs

**4. User Message with Placeholders**
```csharp
new PromptMessage(
    PromptRole.User,
    new TextContent {
      Text = $"Send a letter to Santa Claus and tell him that {args.Name} has behaved {args.Behavior}."
    })
```
- Template with `{args.Name}` and `{args.Behavior}` placeholders

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
    "description": "A prompt that reports to Santa Claus",
    "messages": [
      {
        "role": "system",
        "content": {
          "type": "text",
          "text": "You are a very helpful assistant for Santa Claus."
        }
      },
      {
        "role": "user",
        "content": {
          "type": "text",
          "text": "Send a letter to Santa Claus and tell him that Charlie has behaved Good."
        }
      }
    ]
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

## Best Practices

### 1. Clear System Messages

```csharp
// ✅ GOOD - Specific role and behavior
new PromptMessage(
  PromptRole.System, 
  new TextContent {
    Text = "You are an experienced Python developer. Focus on PEP 8 style and performance."
  })

// ❌ BAD - Too vague
new PromptMessage(
  PromptRole.System, 
  new TextContent {
    Text = "You are helpful."
  })
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
