# Release Notes - v1.1.0

## 🎯 What's New

### Auto-Generated Tool Names ✨

Tool names can now be auto-generated from method names!

```csharp
// Before (v1.0)
[McpTool("ping")]
public JsonRpcMessage Ping(JsonRpcMessage request) { }

// Now (v1.1)
[McpTool]  // Name auto-generated: "ping"
public JsonRpcMessage Ping(JsonRpcMessage request) { }
```

**Features:**
- ✅ Optional tool naming - specify explicitly or auto-generate
- ✅ Smart conversion: `AddNumbersTool` → `add_numbers_tool`
- ✅ Backward compatible - existing explicit names still work
- ✅ Validation - auto-generated names must match MCP naming rules

### GitHub Actions Automation 🚀

**Automated Testing:**
- ✅ Tests run on every push/PR to main branch
- ✅ 62+ comprehensive tests covering all features
- ✅ Test status badge in README

**Automated Releases:**
- ✅ Create release by pushing a version tag: `git push origin v1.1.0`
- ✅ Automatic build, test, and package creation
- ✅ GitHub Release created with artifacts
- ✅ NuGet publishing via Trusted Publishing (secure, keyless)

### Documentation 📚

**New Guides:**
- Auto-Generated Tool Names - Complete feature guide
- GitHub Actions Testing - CI/CD setup and usage
- GitHub Release Automation - Automated release workflow
- NuGet Trusted Publishing - Secure package publishing
- Client Examples - How to build MCP clients using Mcp.Gateway.Tools

---

## 📦 Installation

### NuGet Package

```bash
dotnet add package Mcp.Gateway.Tools --version 1.1.0
```

**Or via Package Manager Console:**
```powershell
Install-Package Mcp.Gateway.Tools -Version 1.1.0
```

---

## 🔄 Upgrade from v1.0

**No breaking changes!** v1.1.0 is fully backward compatible.

**Optional migration to auto-naming:**

```csharp
// v1.0 (still works in v1.1)
[McpTool("ping")]
public JsonRpcMessage Ping(JsonRpcMessage request) { }

// v1.1 (optional - cleaner)
[McpTool]
public JsonRpcMessage Ping(JsonRpcMessage request) { }
```

---

## 🧪 What's Tested

- ✅ **62+ comprehensive tests** (all passing)
- ✅ Auto-naming with 17 new unit tests
- ✅ All transports (HTTP, WebSocket, SSE, stdio)
- ✅ MCP protocol compliance
- ✅ Binary/text streaming
- ✅ GitHub Copilot integration

---

## 📖 Documentation

- [Auto-Generated Tool Names Guide](https://github.com/eyjolfurgudnivatne/mcp.gateway/blob/main/docs/Auto-Generated-Tool-Names.md)
- [GitHub Actions Automation](https://github.com/eyjolfurgudnivatne/mcp.gateway/blob/main/docs/GitHub-Release-Automation.md)
- [Client Examples](https://github.com/eyjolfurgudnivatne/mcp.gateway/blob/main/Mcp.Gateway.Tools/docs/examples/client-examples.md)
- [Full Changelog](https://github.com/eyjolfurgudnivatne/mcp.gateway/blob/main/CHANGELOG.md)

---

## 🙏 Acknowledgments

**Built by:** ARKo AS - AHelse Development Team

**Special thanks to:**
- **Anthropic** - For MCP specification
- **GitHub** - For Actions and Copilot support
- **Microsoft** - For .NET 10

---

**Full Changelog**: https://github.com/eyjolfurgudnivatne/mcp.gateway/blob/main/CHANGELOG.md

**License:** MIT
