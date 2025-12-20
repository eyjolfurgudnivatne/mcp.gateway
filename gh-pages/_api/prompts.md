---
layout: mcp-default
title: Prompts API Reference
description: Complete reference for the Prompts API
breadcrumbs:
  - title: Home
    url: /
  - title: API Reference
    url: /api/
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
    "name": "code_review",
    "description": "Code review prompt",
    "messages": [
      {
        "role": "user",
        "content": "Please review this C# code: public class Example { }"
      }
    ],
    "arguments": {
      "code": {
        "type": "string",
        "description": "The code to review"
      },
      "language": {
        "type": "string",
        "description": "Programming language"
      }
    }
  },
  "id": 2
}
```

**Response fields:**
- `name` (string) - Prompt name
- `description` (string) - Prompt description
- `messages` (array) - Prompt messages with `role` and `content`
- `arguments` (object) - Argument schema (JSON Schema format)

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
            new PromptResponse(
                Name: "greeting",
                Description: "A simple greeting prompt",
                Messages: new[]
                {
                    new PromptMessage(PromptRole.User, "Hello! How can I help you today?")
                },
                Arguments: new { }
            ));
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
        new PromptResponse(
            Name: "code_review",
            Description: "Code review prompt",
            Messages: new[]
            {
                new PromptMessage(PromptRole.User, promptText)
            },
            Arguments: new
            {
                code = new
                {
                    type = "string",
                    description = "The code to review"
                },
                language = new
                {
                    type = "string",
                    description = "Programming language (e.g., csharp, javascript)"
                }
            }
        ));
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
        new PromptResponse(
            Name: "interview_prep",
            Description: "Technical interview preparation",
            Messages: new[]
            {
                new PromptMessage(PromptRole.System, 
                    "You are an experienced technical interviewer."),
                new PromptMessage(PromptRole.User, 
                    $"Help me prepare for a {args.Role} interview at {args.Company}.")
            },
            Arguments: new
            {
                role = new
                {
                    type = "string",
                    description = "Job role (e.g., 'Senior Developer', 'DevOps Engineer')"
                },
                company = new
                {
                    type = "string",
                    description = "Company name"
                }
            }
        ));
}

public record InterviewArgs(string Role, string Company);
```

## Prompt Attributes

### [McpPrompt]

Marks a method as an MCP prompt.

```csharp
[McpPrompt(
    string? name = null,            // Optional: Prompt name (auto-generated if null)
    string? Description = null,     // Optional: Description
    string? Icon = null)]           // Optional: Icon URL (v1.6.5+)
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

## Prompt Responses

### PromptResponse Structure

All prompts must return a `PromptResponse` with these fields:

```csharp
public sealed record PromptResponse(
    string Name,                              // Prompt name
    string Description,                       // Prompt description
    IReadOnlyList<PromptMessage> Messages,   // Prompt messages
    object Arguments);                        // Argument schema
```

### PromptMessage Structure

Messages use `PromptRole` enum:

```csharp
public sealed record PromptMessage(
    PromptRole Role,    // System, User, Assistant, or Tool
    string Content);    // Message content

public enum PromptRole
{
    System,
    User,
    Assistant,
    Tool
}
```

### Simple Prompt

```csharp
return ToolResponse.Success(
    request.Id,
    new PromptResponse(
        Name: "greeting",
        Description: "A greeting prompt",
        Messages: new[]
        {
            new PromptMessage(PromptRole.User, "Hello!")
        },
        Arguments: new { }
    ));
```

### System + User Messages

```csharp
return ToolResponse.Success(
    request.Id,
    new PromptResponse(
        Name: "chat_helper",
        Description: "Chat prompt",
        Messages: new[]
        {
            new PromptMessage(PromptRole.System, "You are a helpful assistant."),
            new PromptMessage(PromptRole.User, "How do I...")
        },
        Arguments: new { }
    ));
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
        new PromptResponse(
            Name: "code_review",
            Description: "Code review prompt",
            Messages: new[]
            {
                new PromptMessage(PromptRole.User, prompt)
            },
            Arguments: new
            {
                code = new { type = "string", description = "The code to review" },
                language = new { type = "string", description = "Programming language" }
            }
        ));
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
        new PromptResponse(
            Name: "sql_builder",
            Description: "SQL query builder",
            Messages: new[]
            {
                new PromptMessage(PromptRole.User, prompt)
            },
            Arguments: new
            {
                table = new { type = "string", description = "Database table name" },
                operation = new { type = "string", description = "SQL operation (SELECT, INSERT, UPDATE, DELETE)" },
                conditions = new { type = "array", description = "Query conditions" }
            }
        ));
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
        new PromptResponse(
            Name: "doc_generator",
            Description: "Documentation generator",
            Messages: new[]
            {
                new PromptMessage(PromptRole.System, 
                    "You are a technical writer creating clear documentation."),
                new PromptMessage(PromptRole.User, 
                    $"Create documentation for:\n\n{args.Code}")
            },
            Arguments: new
            {
                code = new { type = "string", description = "Code to document" }
            }
        ));
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
return PromptResponse.Success(
    request.Id,
    description,
    new[]
    {
        new PromptMessage("system", "You are an expert..."),
        new PromptMessage("user", userPrompt)
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

## Testing

### Unit Test

```csharp
[Fact]
public async Task CodeReview_ValidCode_ReturnsPrompt()
{
    // Arrange
    var prompts = new MyPrompts();
    var request = new TypedJsonRpc<CodeReviewArgs>
    {
        Id = "1",
        Params = new CodeReviewArgs("class Test { }", "csharp")
    };
    
    // Act
    var response = prompts.CodeReview(request);
    
    // Assert
    Assert.NotNull(response.Result);
}
```

### Integration Test

```csharp
[Fact]
public async Task PromptsGet_ValidPrompt_ReturnsMessages()
{
    // Arrange
    using var server = new McpGatewayFixture();
    var client = server.CreateClient();
    
    var request = new
    {
        jsonrpc = "2.0",
        method = "prompts/get",
        @params = new
        {
            name = "code_review",
            arguments = new { Code = "test", Language = "csharp" }
        },
        id = 1
    };
    
    // Act
    var response = await client.PostAsJsonAsync("/mcp", request);
    var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
    
    // Assert
    Assert.True(result.RootElement.TryGetProperty("result", out var resultProp));
    Assert.True(resultProp.TryGetProperty("messages", out _));
}
```

## See Also

- [Tools API](/mcp.gateway/api/tools/) - Tool invocation reference
- [Resources API](/mcp.gateway/api/resources/) - Resource access reference
- [Prompt Example](/mcp.gateway/examples/prompt/) - Complete prompt server example (Coming Soon)
