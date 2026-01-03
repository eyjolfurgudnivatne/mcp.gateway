---
layout: mcp-default
title: Prompts API Reference
description: Complete reference for the Prompts API
breadcrumbs:
  - title: Home
    url: /
  - title: API Reference
    url: /api/prompts/
  - title: Prompts API
    url: /api/prompts/
toc: true
---

# Prompts API Reference

Complete reference for MCP Gateway Prompts API.

## Overview

Prompts allow servers to provide reusable prompt templates that AI assistants can use. They're perfect for:
- Common query patterns
- Standardized instructions
- Context-aware prompts
- Dynamic prompt generation

## Quick Reference

| Method | Description |
|--------|-------------|
| `prompts/list` | List all available prompts |
| `prompts/get` | Get a specific prompt with arguments |

## prompts/list

List all prompts available on the server.

### Request

```json
{
  "jsonrpc": "2.0",
  "method": "prompts/list",
  "params": {
    "cursor": "optional-pagination-cursor"
  },
  "id": 1
}
```

### Response

```json
{
  "jsonrpc": "2.0",
  "result": {
    "prompts": [
      {
        "name": "code_review",
        "description": "Review code for best practices",
        "arguments": [
          {
            "name": "code",
            "description": "The code to review",
            "required": true
          },
          {
            "name": "language",
            "description": "Programming language",
            "required": false
          }
        ]
      }
    ],
    "nextCursor": "optional-cursor"
  },
  "id": 1
}
```

## prompts/get

Get a specific prompt with provided arguments.

### Request

```json
{
  "jsonrpc": "2.0",
  "method": "prompts/get",
  "params": {
    "name": "code_review",
    "arguments": {
      "code": "public class Example { }",
      "language": "csharp"
    }
  },
  "id": 2
}
```

### Response

```json
{
  "jsonrpc": "2.0",
  "result": {
    "description": "Code review prompt",
    "messages": [
      {
        "role": "user",
        "content": {
            "type": "text",
            "text": "Please review this csharp code: public class Example { }"
        }
      }
    ]
  },
  "id": 2
}
```

**Response fields:**
- `description` (string) - Prompt description
- `messages` (array) - Prompt messages with `role` and `content`

## Defining Prompts

### Basic Prompt

```csharp
using Mcp.Gateway.Tools;

public class MyPrompts
{
    [McpPrompt("greeting")]
    public JsonRpcMessage Greeting(JsonRpcMessage request)
    {
        return ToolResponse.Success(
            request.Id,
            new PromptResponse
            {
                Description = "A simple greeting prompt",
                Messages = [
                    new PromptMessage(
                        PromptRole.User, 
                        new TextContent {
                            Text = "Hello! How can I help you today?"
                        })
                ]
            });
    }
}
```

### Prompt with Arguments

```csharp
[McpPrompt("code_review",
    Description = "Review code for best practices")]
public JsonRpcMessage CodeReview(TypedJsonRpc<CodeReviewArgs> request)
{
    var args = request.GetParams()!;
    
    var promptText = $"Please review this {args.Language} code:\n\n{args.Code}";
    
    return ToolResponse.Success(
        request.Id,
        new PromptResponse
        {
            Description = "Code review prompt",
            Messages = [
                new PromptMessage(
                    PromptRole.User, 
                    new TextContent {
                        Text = promptText
                    })
            ]
        });
}

public record CodeReviewArgs(string Code, string? Language = "text");
```

### Multi-Message Prompt

```csharp
[McpPrompt("interview_prep")]
public JsonRpcMessage InterviewPrep(TypedJsonRpc<InterviewArgs> request)
{
    var args = request.GetParams()!;
    
    return ToolResponse.Success(
        request.Id,
        new PromptResponse
        {
            Description = "Technical interview preparation",
            Messages = [
                new(
                    PromptRole.System,
                    new TextContent {
                        Text = "You are an experienced technical interviewer."
                    }),
                new (
                    PromptRole.User,
                    new TextContent {
                        Text = $"Help me prepare for a {args.Role} interview at {args.Company}."
                    })
            ]
        });
}

public record InterviewArgs(string Role, string Company);
```

### Content blocks

```csharp
new TextContent {...}
new ImageContent {...}
new AudioContent {...}
new ResourceLink {...}
new EmbeddedResource {...}
```

## Prompt Attributes

### [McpPrompt]

Marks a method as an MCP prompt.

```csharp
[McpPrompt(
    string? name = null,            // Optional: Prompt name (auto-generated if null)
    string? Description = null,     // Optional: Description
    string? Icon = null)]           // Optional: Icon URL (v1.6.5+) (use attributes instead. See below)
```

**Attribute properties:**

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `name` | string | No | Prompt identifier (auto-generated from method name if not provided) |
| `Description` | string | No | Prompt description for AI |
| `Icon` | string | No | Icon URL (v1.6.5+) |

**Example with icon:**
```csharp
[McpPrompt("code_review",
    Description = "Review code for best practices",
    Icon = "https://example.com/icons/code-review.png")]
```

**Icons with attributes:**

```csharp
[McpPrompt(...)]
[McpIcon("icon.png")]
[McpIcon("icon2.png", "image/png", Sizes = new[] { "16x16", "32x32", "48x48", "any" })]
[McpIcon("icon-light.png", "image/png", McpIconTheme.Light)]
[McpIcon("icon-dark.png", "image/png", McpIconTheme.Dark)]
```

## Prompt Responses

### PromptResponse Structure

All prompts must return a `PromptResponse` with these fields:

```csharp
public class PromptResponse
{
    /// <summary>
    /// An optional description for the prompt.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Messages returned.
    /// </summary>
    public List<PromptMessage> Messages { get; set; } = [];

    /// <summary>
    /// The _meta property/parameter is reserved by MCP to allow clients and servers to attach additional metadata to their interactions.
    /// </summary>
    public Dictionary<string, object>? Meta { get; set; }
}
```

### PromptMessage Structure

Messages use `PromptRole` enum:

```csharp
public sealed record PromptMessage(
    PromptRole Role,            // System, User, Assistant, or Tool
    IContentBlock Content);     // Message content

public enum PromptRole
{
    System,
    User,
    Assistant,
    Tool
}

// Content blocks:

new TextContent {...}
new ImageContent {...}
new AudioContent {...}
new ResourceLink {...}
new EmbeddedResource {...}
```

### Simple Prompt

```csharp
return ToolResponse.Success(
    request.Id,
    new PromptResponse
    {
        Description = "A greeting prompt",
        Messages = [
            new PromptMessage(
                PromptRole.User, 
                new TextContent
                {
                    Text = "Hello!"
                })
        ]
    });
```

### System + User Messages

```csharp
return ToolResponse.Success(
    request.Id,
    new PromptResponse
    {
        Description = "Chat prompt",
        Messages = [
            new PromptMessage(PromptRole.System, new TextContent { Text = "You are a helpful assistant." }),
            new PromptMessage(PromptRole.User, new TextContent { Text = "How do I..." })
        ]
    });
```

## Use Cases

### 1. Code Review Prompt

```csharp
[McpPrompt("code_review")]
public JsonRpcMessage CodeReview(TypedJsonRpc<CodeReviewArgs> request)
{
    var args = request.GetParams()!;
    
    var prompt = $@"Please review this {args.Language} code for:
- Best practices
- Potential bugs
- Performance issues
- Security concerns

Code:
```{args.Language}
{args.Code}
```";
    
    return ToolResponse.Success(
        request.Id,
        new PromptResponse
        {
            Description = "Code review prompt",
            Messages = [
                new PromptMessage(PromptRole.User, new TextContent { Text = prompt })
            ]
        });
}

public record CodeReviewArgs(string Code, string Language);
```

### 2. SQL Query Builder

```csharp
[McpPrompt("sql_builder")]
public JsonRpcMessage SqlBuilder(TypedJsonRpc<SqlArgs> request)
{
    var args = request.GetParams()!;
    
    var prompt = $@"Generate a SQL query for:
Table: {args.Table}
Operation: {args.Operation}
Conditions: {string.Join(", ", args.Conditions)}

Please provide:
1. The SQL query
2. Explanation of the query
3. Any potential performance considerations";
    
    return ToolResponse.Success(
        request.Id,
        new PromptResponse
        {
            Description = "SQL query builder",
            Messages = [
                new PromptMessage(PromptRole.User, new TextContent { Text = prompt })
            ]
        });
}

public record SqlArgs(string Table, string Operation, string[] Conditions);
```

### 3. Documentation Generator

```csharp
[McpPrompt("doc_generator")]
public JsonRpcMessage DocGenerator(TypedJsonRpc<DocArgs> request)
{
    var args = request.GetParams()!;
    
    return ToolResponse.Success(
        request.Id,
        new PromptResponse
        {
            Description = "Documentation generator",
            Messages = [
                new PromptMessage(
                    PromptRole.System, 
                    new TextContent { 
                        Text = "You are a technical writer creating clear documentation."
                    }),

                new PromptMessage(
                    PromptRole.User, 
                    new TextContent { 
                        Text = $"Create documentation for:\n\n{args.Code}"
                    })
            ]
        });
}

public record DocArgs(string Code);
```

## Best Practices

### 1. Clear Descriptions

```csharp
[McpPrompt("analyze_logs",
    Description = "Analyzes log files for errors and patterns")]
```

### 2. Validate Arguments

```csharp
var args = request.GetParams()
    ?? throw new ToolInvalidParamsException("Arguments required");

if (string.IsNullOrWhiteSpace(args.Code))
{
    throw new ToolInvalidParamsException("Code cannot be empty");
}
```

### 3. Use System Messages

```csharp
return ToolResponse.Success(
    request.Id,
    new PromptResponse
    {
        Description = description,
        Messages = [
            new PromptMessage(
                PromptRole.System, 
                new TextContent { 
                    Text = "You are an expert..."
                }),

            new PromptMessage(
                PromptRole.User, 
                new TextContent { 
                    Text = userPrompt
                })
        ]
    });
```

### 4. Structure Complex Prompts

```csharp
var sections = new[]
{
    "## Context",
    args.Context,
    "",
    "## Task",
    args.Task,
    "",
    "## Requirements",
    string.Join("\n", args.Requirements.Select(r => $"- {r}"))
};

var prompt = string.Join("\n", sections);
```


## See Also

- [Tools API](/mcp.gateway/api/tools/) - Tool invocation reference
- [Resources API](/mcp.gateway/api/resources/) - Resource access reference
- [Prompt Example](/mcp.gateway/examples/prompt/) - Complete prompt server example
