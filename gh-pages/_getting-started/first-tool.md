---
layout: mcp-default
title: Your First Tool
description: Build your first MCP tool step-by-step
breadcrumbs:
  - title: Home
    url: /
  - title: Getting Started
    url: /getting-started/index/
  - title: Your First Tool
    url: /getting-started/first-tool/
toc: true
---

# Your First Tool

Learn how to build your first MCP tool from scratch.

## What You'll Build

A simple greeting tool that:
- Accepts a name parameter
- Returns a personalized greeting
- Works with GitHub Copilot and Claude Desktop

**Time:** ~10 minutes

## Step 1: Create Project

```bash
dotnet new web -n GreetingServer
cd GreetingServer
dotnet add package Mcp.Gateway.Tools
```

## Step 2: Define Tool Parameters

Create `Models/GreetParams.cs`:

```csharp
namespace GreetingServer.Models;

public record GreetParams(string Name);
```

**Why use records?**
- Immutable by default
- Built-in equality comparison
- Clean, concise syntax

## Step 3: Create Tool Class

Create `Tools/GreetingTools.cs`:

```csharp
using Mcp.Gateway.Tools;
using GreetingServer.Models;

namespace GreetingServer.Tools;

public class GreetingTools
{
    [McpTool("greet",
        Title = "Greet User",
        Description = "Greets a user by name")]
    public JsonRpcMessage Greet(TypedJsonRpc<GreetParams> request)
    {
        // Get parameters (with null check)
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException(
                "Parameter 'Name' is required.");

        // Build response
        var greeting = $"Hello, {args.Name}! Welcome to MCP Gateway!";

        return ToolResponse.Success(
            request.Id,
            new { message = greeting });
    }
}
```

## Step 4: Configure Server

Update `Program.cs`:

```csharp
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);

// Register MCP Gateway
builder.AddToolsService();

var app = builder.Build();

// stdio mode for GitHub Copilot
if (args.Contains("--stdio"))
{
    await ToolInvoker.RunStdioModeAsync(app.Services);
    return;
}

// HTTP mode
app.UseWebSockets();
app.UseProtocolVersionValidation();
app.MapStreamableHttpEndpoint("/mcp");

app.Run();
```

**Important for GitHub Copilot:** GitHub Copilot (as of Dec 2025) expects protocol version `2025-06-18`. Set the environment variable to ensure compatibility:

```powershell
# PowerShell
$env:MCP_PROTOCOL_VERSION = "2025-06-18"
dotnet run
```

Or add to `launchSettings.json`:

```json
{
  "profiles": {
    "stdio": {
      "commandName": "Project",
      "commandLineArgs": "--stdio",
      "environmentVariables": {
        "MCP_PROTOCOL_VERSION": "2025-06-18"
      }
    }
  }
}
```

## Step 5: Test Your Tool

### Test with curl

Start the server:

```bash
dotnet run
```

Call your tool:

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "greet",
      "arguments": {
        "Name": "Alice"
      }
    },
    "id": 1
  }'
```

**Expected response:**

```json
{
  "jsonrpc": "2.0",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"message\":\"Hello, Alice! Welcome to MCP Gateway!\"}"
      }
    ]
  },
  "id": 1
}
```

### Test with GitHub Copilot

**Important:** GitHub Copilot (as of Dec 2025) expects protocol version `2025-06-18`. Make sure to set `MCP_PROTOCOL_VERSION` environment variable before running (see Step 4).

Create `.mcp.json` (in your home directory or workspace):

```json
{
  "mcpServers": {
    "greeting_server": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\path\\to\\GreetingServer",
        "--",
        "--stdio"
      ],
      "env": {
        "MCP_PROTOCOL_VERSION": "2025-06-18"
      }
    }
  }
}
```

**Important:** Use absolute path!

Then in GitHub Copilot Chat:

```
@greeting_server greet Bob
```

## Understanding the Code

### 1. Tool Attribute

```csharp
[McpTool("greet",              // Tool name (must be unique)
    Title = "Greet User",       // Human-readable title
    Description = "Greets...")]  // Description for AI
```

**Tool naming rules:**
- Lowercase with underscores: `greet_user`
- Only: a-z, 0-9, underscore, hyphen
- Max 128 characters

### 2. TypedJsonRpc<T>

```csharp
public JsonRpcMessage Greet(TypedJsonRpc<GreetParams> request)
```

Benefits:
- **Type-safe** parameters
- **Automatic** JSON deserialization
- **IntelliSense** support

### 3. GetParams()

```csharp
var args = request.GetParams()
    ?? throw new ToolInvalidParamsException("...");
```

**Why null check?**
- Client might send invalid JSON
- Parameters might be missing
- Better error messages

### 4. ToolResponse

```csharp
return ToolResponse.Success(
    request.Id,              // Match request ID
    new { message = "..." }  // Response data
);
```

**Always return:**
- Same `id` as request
- Structured data object
- Use `ToolResponse` helpers

## Common Mistakes

### ❌ Mistake 1: Forgetting Null Check

```csharp
// BAD - NullReferenceException if params are invalid
var args = request.GetParams();
var name = args.Name;  // CRASH!
```

```csharp
// GOOD - Explicit error message
var args = request.GetParams()
    ?? throw new ToolInvalidParamsException("Name required");
var name = args.Name;  // Safe!
```

### ❌ Mistake 2: Wrong Tool Name Format

```csharp
[McpTool("GreetUser")]      // BAD - PascalCase
[McpTool("greet user")]     // BAD - spaces not allowed
[McpTool("greet_user")]     // GOOD! ✅
[McpTool("greet-user")]     // GOOD! ✅ (hyphens allowed)
[McpTool("greet.user")]     // GOOD! ✅ (dots allowed since v1.4.0)
```

**Valid pattern (MCP 2025-11-25):** `^[a-zA-Z0-9_.-]{1,128}$`

**Allowed characters:**
- ✅ Lowercase letters: `a-z`
- ✅ Uppercase letters: `A-Z`
- ✅ Numbers: `0-9`
- ✅ Underscores: `_`
- ✅ Hyphens: `-`
- ✅ Dots: `.` (since v1.4.0 for namespacing)

**Examples:**
- ✅ `greet_user` - Underscore (traditional)
- ✅ `greet-user` - Hyphen
- ✅ `greet.user` - Dot (namespacing)
- ✅ `admin.tools.list` - Multi-level namespace
- ❌ `greet user` - Spaces NOT allowed
- ❌ `greet@user` - Special characters NOT allowed

### ❌ Mistake 3: Not Matching Request ID

```csharp
// BAD - Wrong ID
return ToolResponse.Success("wrong-id", data);

// GOOD - Use request.Id
return ToolResponse.Success(request.Id, data);
```

## Next Steps

### 1. Add More Tools

```csharp
[McpTool("greet_formal")]
public JsonRpcMessage GreetFormal(TypedJsonRpc<GreetParams> request)
{
    var args = request.GetParams()!;
    var greeting = $"Good day, {args.Name}. How may I assist you?";
    return ToolResponse.Success(request.Id, new { message = greeting });
}

[McpTool("greet_casual")]
public JsonRpcMessage GreetCasual(TypedJsonRpc<GreetParams> request)
{
    var args = request.GetParams()!;
    var greeting = $"Hey {args.Name}! What's up?";
    return ToolResponse.Success(request.Id, new { message = greeting });
}
```

### 2. Add Optional Parameters

```csharp
public record GreetParams(string Name, string? Language = "en");

[McpTool("greet_multilingual")]
public JsonRpcMessage GreetMultilingual(TypedJsonRpc<GreetParams> request)
{
    var args = request.GetParams()!;
    
    var greeting = args.Language switch
    {
        "no" => $"Hei, {args.Name}!",
        "es" => $"¡Hola, {args.Name}!",
        "fr" => $"Bonjour, {args.Name}!",
        _ => $"Hello, {args.Name}!"
    };
    
    return ToolResponse.Success(request.Id, new { message = greeting });
}
```

### 3. Add Validation

```csharp
[McpTool("greet")]
public JsonRpcMessage Greet(TypedJsonRpc<GreetParams> request)
{
    var args = request.GetParams()
        ?? throw new ToolInvalidParamsException("Name is required");

    // Validate name
    if (string.IsNullOrWhiteSpace(args.Name))
    {
        throw new ToolInvalidParamsException("Name cannot be empty");
    }

    if (args.Name.Length > 100)
    {
        throw new ToolInvalidParamsException("Name too long (max 100 characters)");
    }

    var greeting = $"Hello, {args.Name}!";
    return ToolResponse.Success(request.Id, new { message = greeting });
}
```

## Troubleshooting

### Tool Not Found

**Problem:** `Method not found` error

**Solutions:**
1. Check tool name matches exactly: `greet` (lowercase)
2. Ensure class is public
3. Ensure method is public
4. Restart server after code changes

### Parameters Not Deserializing

**Problem:** `GetParams()` returns null

**Solutions:**
1. Check parameter names match exactly (case-sensitive)
2. Ensure JSON is valid
3. Check parameter types match (string, int, etc.)

### Server Not Starting

**Problem:** Port already in use

**Solutions:**
```csharp
// Change port in Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5001");
```

## Complete Example

Full working code:

```csharp
// Program.cs
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);
builder.AddToolsService();

var app = builder.Build();

if (args.Contains("--stdio"))
{
    await ToolInvoker.RunStdioModeAsync(app.Services);
    return;
}

app.UseWebSockets();
app.UseProtocolVersionValidation();
app.MapStreamableHttpEndpoint("/mcp");
app.Run();

// Tools/GreetingTools.cs
using Mcp.Gateway.Tools;

namespace GreetingServer.Tools;

public class GreetingTools
{
    [McpTool("greet", Description = "Greets a user by name")]
    public JsonRpcMessage Greet(TypedJsonRpc<GreetParams> request)
    {
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException("Name required");

        return ToolResponse.Success(
            request.Id,
            new { message = $"Hello, {args.Name}!" });
    }
}

public record GreetParams(string Name);
```

## What's Next?

Now that you've built your first tool:

1. **[Calculator Example](/mcp.gateway/examples/calculator/)** - More complex tool example
2. **[Lifecycle Hooks](/mcp.gateway/features/lifecycle-hooks/)** - Monitor tool invocations
3. **[Tools API](/mcp.gateway/api/tools/)** - Complete API reference

## See Also

- [Getting Started Overview](/mcp.gateway/getting-started/index/) - Overview and quick start
- [Installation Guide](/mcp.gateway/getting-started/installation/) - Setup instructions
- [Calculator Example](/mcp.gateway/examples/calculator/) - More complex tool example
