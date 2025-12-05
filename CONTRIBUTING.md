# Contributing to MCP Gateway

Thank you for your interest in contributing to **MCP Gateway**! 🎉

We welcome contributions of all kinds: bug fixes, new features, documentation improvements, and more.

---

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How to Contribute](#how-to-contribute)
- [Development Setup](#development-setup)
- [Creating Custom Tools](#creating-custom-tools)
- [Code Style](#code-style)
- [Testing](#testing)
- [Pull Request Process](#pull-request-process)
- [Performance Considerations](#performance-considerations)
- [Reporting Bugs](#reporting-bugs)
- [Feature Requests](#feature-requests)

---

## 🤝 Code of Conduct

This project follows a **simple guideline**: Be respectful, constructive, and collaborative.

We're all here to build something great together! 🚀

---

## 💡 How to Contribute

### Types of Contributions

1. **Bug Fixes** - Found a bug? Fix it and submit a PR!
2. **New Features** - Have an idea? Discuss it in an issue first
3. **Documentation** - Improve README, docs, or code comments
4. **Performance** - Optimizations are always welcome
5. **Tests** - More test coverage is great
6. **Examples** - New tool examples help everyone

---

## 🛠️ Development Setup

### Prerequisites

- **.NET 10 SDK** or later
- **Visual Studio 2025** or **Visual Studio Code**
- **Git**

### Clone and Build

```bash
# Clone repository
git clone https://github.com/eyjolfurgudnivatne/MCP-Gateway.git
cd MCP-Gateway

# Restore dependencies
dotnet restore

# Build library
dotnet build Mcp.Gateway.Tools

# Run tests
dotnet test

# Run example server
dotnet run --project Mcp.Gateway.Server
```

### Project Structure

```
MCP-Gateway/
├── Mcp.Gateway.Tools/        # Core library (your focus!)
├── Mcp.Gateway.Server/       # Full-featured example server
├── Mcp.Gateway.GCCServer/    # Minimal GitHub Copilot example
├── Mcp.Gateway.Tests/        # 45+ tests
├── Mcp.Gateway.Benchmarks/   # Performance benchmarks
└── docs/                     # Documentation
```

---

## 🔧 Creating Custom Tools

### Basic Tool Example

```csharp
using Mcp.Gateway.Tools;

public class MyTools
{
    [McpTool("my_tool",
        Description = "Does something awesome",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""input"":{""type"":""string"",""description"":""Input text""}
            },
            ""required"":[""input""]
        }")]
    public async Task<JsonRpcMessage> MyTool(JsonRpcMessage request)
    {
        var input = request.GetParams().GetProperty("input").GetString();
        var result = $"Processed: {input}";
        return ToolResponse.Success(request.Id, new { result });
    }
}
```

### Tool Naming Convention

**IMPORTANT:** Tool names must follow MCP protocol specification:
- Pattern: `^[a-zA-Z0-9_-]{1,128}$`
- Use **underscores** (`_`) or **hyphens** (`-`)
- Do **NOT** use dots (`.`) - breaks GitHub Copilot compatibility

✅ **Good:** `my_tool`, `calculate_sum`, `fetch-data`  
❌ **Bad:** `my.tool`, `calculate.sum`

### Streaming Tools

For binary or text streaming, use `ToolConnector`:

```csharp
[McpTool("stream_data")]
public async Task StreamData(ToolConnector connector)
{
    var meta = new StreamMessageMeta(
        Method: "stream_data",
        Binary: true);

    using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);

    // Stream data in chunks
    var data = new byte[] { 1, 2, 3, 4, 5 };
    await handle.WriteAsync(data);
    
    await handle.CompleteAsync(new { totalBytes = data.Length });
}
```

### Dependency Injection

Tools support automatic DI:

```csharp
public class MyService
{
    public string ProcessData(string input) => input.ToUpper();
}

[McpTool("process_text")]
public async Task<JsonRpcMessage> ProcessText(
    JsonRpcMessage request,
    MyService service) // Injected automatically!
{
    var input = request.GetParams().GetProperty("text").GetString();
    var result = service.ProcessData(input);
    return ToolResponse.Success(request.Id, new { result });
}
```

**Register service in DI:**
```csharp
builder.Services.AddScoped<MyService>();
```

---

## 📝 Code Style

### General Guidelines

- **C# 14.0 / .NET 10** conventions
- **File-scoped namespaces** (`namespace Mcp.Gateway.Tools;`)
- **Primary constructors** where appropriate
- **XML documentation** for public APIs
- **Minimal comments** (code should be self-explanatory)

### Example Style

```csharp
namespace Mcp.Gateway.Tools;

/// <summary>
/// Provides arithmetic operations.
/// </summary>
public class Calculator
{
    /// <summary>
    /// Adds two numbers.
    /// </summary>
    [McpTool("add_numbers",
        Description = "Adds two numbers together",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""a"":{""type"":""number""},
                ""b"":{""type"":""number""}
            },
            ""required"":[""a"",""b""]
        }")]
    public async Task<JsonRpcMessage> Add(JsonRpcMessage request)
    {
        var a = request.GetParams().GetProperty("a").GetInt32();
        var b = request.GetParams().GetProperty("b").GetInt32();
        return ToolResponse.Success(request.Id, new { result = a + b });
    }
}
```

### What to Avoid

- ❌ Unnecessary comments
- ❌ Regions (`#region`)
- ❌ Public fields (use properties)
- ❌ Abbreviations in names
- ❌ Magic numbers (use constants)

---

## 🧪 Testing

### Test Structure

Follow the existing pattern:

```
Mcp.Gateway.Tests/
├── Endpoints/
│   ├── Http/          # HTTP RPC tests
│   ├── Ws/            # WebSocket tests
│   ├── Sse/           # SSE tests
│   └── Stdio/         # stdio tests
```

### Test Naming Convention

`FeatureName_Scenario_ExpectedResult`

**Examples:**
```csharp
[Fact]
public async Task Add_Numbers_ReturnsCorrectSum() { }

[Fact]
public async Task Add_Numbers_WithMissingParams_ReturnsInvalidParamsError() { }

[Fact]
public async Task ToolsCall_WithStreamingTool_ReturnsError() { }
```

### Running Tests

```bash
# All tests
dotnet test

# Specific category
dotnet test --filter "FullyQualifiedName~McpProtocolTests"

# With detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Test Requirements

- ✅ All tests must pass before PR
- ✅ Add tests for new features
- ✅ Cover edge cases
- ✅ Test all transports (HTTP, WS, SSE, stdio)

---

## 🚀 Pull Request Process

### 1. Fork and Branch

```bash
# Fork repository on GitHub
# Clone your fork
git clone https://github.com/YOUR_USERNAME/mcp.gateway.git

# Create feature branch
git checkout -b feature/my-awesome-feature
```

### 2. Make Changes

- Write code
- Add tests
- Update documentation
- Run tests: `dotnet test`
- Run benchmarks (if performance-related): `dotnet run -c Release --project Mcp.Gateway.Benchmarks`

### 3. Commit

```bash
# Stage changes
git add .

# Commit with clear message
git commit -m "feat: add awesome new feature

- Implemented X
- Added tests for Y
- Updated documentation"
```

**Commit message format:**
- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation only
- `perf:` - Performance improvement
- `test:` - Adding tests
- `refactor:` - Code refactoring

### 4. Push and Create PR

```bash
# Push to your fork
git push origin feature/my-awesome-feature

# Create Pull Request on GitHub
```

### 5. PR Checklist

- [ ] All tests passing (`dotnet test`)
- [ ] No build warnings
- [ ] Code follows style guidelines
- [ ] Documentation updated (if needed)
- [ ] CHANGELOG.md updated (if user-facing change)
- [ ] Benchmarks run (if performance change)

### 6. Review Process

- Maintainers will review your PR
- Address feedback if needed
- Once approved, it will be merged!

---

## ⚡ Performance Considerations

### Benchmarking

If your change affects performance:

```bash
# Run benchmarks
dotnet run -c Release --project Mcp.Gateway.Benchmarks

# Compare before/after results
```

### Performance Tips

1. **Avoid allocations in hot paths**
   - Use `ArrayPool<T>` for buffers
   - Prefer `ValueTask` for sync paths (v2.0+)
   - Use `Span<T>` and `Memory<T>` for slicing

2. **JSON Optimization**
   - Use `JsonSerializer.SerializeToUtf8Bytes` for binary output
   - Note: Source Generators blocked by `object?` types (v1.0)

3. **WebSocket Streaming**
   - ArrayPool for buffers (v1.1)
   - Batch small messages when possible

See [Performance Optimization Plan](docs/Performance-Optimization-Plan.md) for details.

---

## 🐛 Reporting Bugs

### Before Reporting

1. Check [existing issues](https://github.com/eyjolfurgudnivatne/mcp.gateway/issues)
2. Verify it's reproducible
3. Test with latest version

### Bug Report Template

```markdown
**Describe the bug**
A clear description of what the bug is.

**To Reproduce**
Steps to reproduce:
1. Setup server with...
2. Send request...
3. Observe error...

**Expected behavior**
What you expected to happen.

**Actual behavior**
What actually happened.

**Environment:**
- OS: [Windows/Linux/macOS]
- .NET Version: [10.0.x]
- MCP Gateway Version: [1.0.0]

**Logs/Errors**
```
Paste error messages or logs here
```

**Additional context**
Any other relevant information.
```

---

## 💡 Feature Requests

### Before Requesting

1. Check [existing issues](https://github.com/eyjolfurgudnivatne/mcp.gateway/issues)
2. Review [roadmap](README.md#roadmap)
3. Consider if it fits the project scope

### Feature Request Template

```markdown
**Feature Description**
Clear description of the feature.

**Use Case**
Why is this feature needed? What problem does it solve?

**Proposed Solution**
How you think it should work.

**Alternatives Considered**
Other approaches you've thought about.

**Additional Context**
Any other relevant information.
```

---

## 🎯 Development Focus Areas

### High Priority

- **Performance optimizations** (see [Performance Plan](docs/Performance-Optimization-Plan.md))
- **More example tools** (file ops, database, etc.)
- **Documentation improvements**
- **Bug fixes**

### Medium Priority

- **NuGet package** (v1.1)
- **Parameter caching** (v1.1)
- **ArrayPool implementation** (v1.1)

### Future (v2.0+)

- **MCP Resources support**
- **MCP Prompts support**
- **Hybrid JSON Source Generators**
- **Custom transport providers**

---

## 📚 Resources

### Documentation

- [README.md](README.md) - Project overview
- [MCP Protocol](docs/MCP-Protocol.md) - Protocol specification
- [Streaming Protocol](docs/StreamingProtocol.md) - Streaming details
- [Performance Plan](docs/Performance-Optimization-Plan.md) - Performance optimization

### External Resources

- [MCP Specification](https://modelcontextprotocol.io/) - Official MCP spec
- [JSON-RPC 2.0](https://www.jsonrpc.org/specification) - JSON-RPC standard
- [.NET Performance](https://learn.microsoft.com/en-us/dotnet/core/performance/) - .NET best practices

---

## 🙏 Thank You!

Every contribution, no matter how small, helps make **MCP Gateway** better for everyone.

**Happy coding!** 🚀

---

**Questions?** Open an issue or start a discussion on GitHub!

**License:** MIT - See [LICENSE](LICENSE) for details.
