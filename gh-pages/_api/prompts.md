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
    "description": "Code review prompt",
    "messages": [
      {
        "role": "user",
        "content": {
          "type": "text",
          "text": "Please review this C# code: public class Example { }"
        }
      }
    ]
  },
  "id": 2
}
```

## Defining Prompts

### Basic Prompt

```csharp
using Mcp.Gateway.Tools;

public class MyPrompts
{
    [McpPrompt("greeting")]
    public JsonRpcMessage Greeting(JsonRpcMessage request)
    {
        return PromptResponse.Success(
            request.Id,
            "A simple greeting prompt",
            new[]
            {
                new PromptMessage("user", "Hello! How can I help you today?")
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
    
    return PromptResponse.Success(
        request.Id,
        "Code review prompt",
        new[]
        {
            new PromptMessage("user", promptText)
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
    
    return PromptResponse.Success(
        request.Id,
        "Technical interview preparation",
        new[]
        {
            new PromptMessage("system", 
                "You are an experienced technical interviewer."),
            new PromptMessage("user", 
                $"Help me prepare for a {args.Role} interview at {args.Company}.")
        });
}

public record InterviewArgs(string Role, string Company);
```

## Prompt Attributes

### [McpPrompt]

Marks a method as an MCP prompt.

```csharp
[McpPrompt(
    string name,                    // Required: Prompt name
    string? Description = null)]    // Optional: Description
```

## Prompt Responses

### Simple Prompt

```csharp
return PromptResponse.Success(
    request.Id,
    "Description",
    new[]
    {
        new PromptMessage("user", "Prompt text")
    });
```

### System + User Messages

```csharp
return PromptResponse.Success(
    request.Id,
    "Chat prompt",
    new[]
    {
        new PromptMessage("system", "You are a helpful assistant."),
        new PromptMessage("user", "How do I...")
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
    
    return PromptResponse.Success(request.Id, "Code review", 
        new[] { new PromptMessage("user", prompt) });
}
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
    
    return PromptResponse.Success(request.Id, "SQL builder",
        new[] { new PromptMessage("user", prompt) });
}

public record SqlArgs(
    string Table, 
    string Operation, 
    string[] Conditions);
```

### 3. Documentation Generator

```csharp
[McpPrompt("doc_generator")]
public JsonRpcMessage DocGenerator(TypedJsonRpc<DocArgs> request)
{
    var args = request.GetParams()!;
    
    return PromptResponse.Success(
        request.Id,
        "Documentation generator",
        new[]
        {
            new PromptMessage("system", 
                "You are a technical writer creating clear documentation."),
            new PromptMessage("user", 
                $"Create documentation for:\n\n{args.Code}")
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

- [Tools API](/api/tools/) - Tool invocation reference
- [Resources API](/api/resources/) - Resource access reference
- [Examples](/examples/) - Complete server examples
