# 📦 NuGet Publishing Guide for MCP Gateway

**Created:** 5. desember 2025  
**Status:** Ready to publish  
**Package:** Mcp.Gateway.Tools v1.0.1

---

## 🎯 Overview

**What:** Publish `Mcp.Gateway.Tools` to NuGet.org  
**Why:** Make library available for .NET developers worldwide  
**Time:** 30-60 minutes (first time), 10 minutes (subsequent)  
**Cost:** FREE!

---

## ✅ Prerequisites

### 1. NuGet.org Account
- [x] Create account at https://www.nuget.org/users/account/LogOn
- [ ] Verify email
- [ ] Generate API key

### 2. API Key Setup
1. Go to: https://www.nuget.org/account/apikeys
2. Click **"Create"**
3. Settings:
   - **Key Name:** `MCP.Gateway`
   - **Expiration:** 365 days (or longer)
   - **Glob Pattern:** `Mcp.Gateway.*`
   - **Select Scopes:** "Push new packages and package versions"
4. **Copy API key** (you won't see it again!)
5. Save securely (environment variable or password manager)

---

## 🏗️ Step-by-Step Publishing

### Step 1: Build the Package

```bash
# Clean previous builds
dotnet clean

# Build in Release mode
dotnet build -c Release

# Create NuGet package
dotnet pack Mcp.Gateway.Tools -c Release -o nupkgs
```

**Result:**
- Creates `nupkgs/Mcp.Gateway.Tools.1.0.1.nupkg`
- Creates `nupkgs/Mcp.Gateway.Tools.1.0.1.snupkg` (symbols)

---

### Step 2: Test Package Locally (IMPORTANT!)

```bash
# Inspect package contents
dotnet nuget push nupkgs/Mcp.Gateway.Tools.1.0.1.nupkg --source local --skip-duplicate

# Or use NuGet Package Explorer (Windows)
# Download: https://github.com/NuGetPackageExplorer/NuGetPackageExplorer
```

**Verify:**
- [ ] README.md included
- [ ] LICENSE included
- [ ] DLLs present
- [ ] Metadata correct (version, author, description)

---

### Step 3: Publish to NuGet.org

#### Option A: Using dotnet CLI (Recommended)

```bash
# Set API key (one-time setup)
$env:NUGET_API_KEY = "your-api-key-here"

# Or use dotnet nuget setapikey
dotnet nuget push nupkgs/Mcp.Gateway.Tools.1.0.1.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

#### Option B: Using Web UI

1. Go to: https://www.nuget.org/packages/manage/upload
2. Click **"Choose File"**
3. Select `nupkgs/Mcp.Gateway.Tools.1.0.1.nupkg`
4. Click **"Upload"**
5. Verify metadata
6. Click **"Submit"**

---

### Step 4: Wait for Validation

**Timeline:**
- Upload: Instant
- Validation: 5-15 minutes
- Indexing: 15-30 minutes
- Searchable: ~1 hour

**Check status:**
- https://www.nuget.org/packages/Mcp.Gateway.Tools

---

## 📋 What's in the Package?

### Metadata (from .csproj)
```xml
<PackageId>Mcp.Gateway.Tools</PackageId>
<Version>1.0.1</Version>
<Authors>ARKo AS - AHelse Development Team</Authors>
<Description>Production-ready Model Context Protocol (MCP) Gateway library for .NET 10...</Description>
<PackageProjectUrl>https://github.com/eyjolfurgudnivatne/mcp.gateway</PackageProjectUrl>
<PackageTags>mcp;model-context-protocol;json-rpc;websocket;github-copilot;claude;ai</PackageTags>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
```

### Contents
```
Mcp.Gateway.Tools.1.0.1.nupkg/
├── lib/
│   └── net10.0/
│       ├── Mcp.Gateway.Tools.dll
│       └── Mcp.Gateway.Tools.xml (documentation)
├── README.md
└── [metadata]
```

---

## 🔍 Verification Steps

### After Publishing

1. **Check Package Page:**
   - https://www.nuget.org/packages/Mcp.Gateway.Tools
   - Verify version shows v1.0.1
   - Check download count starts at 0

2. **Test Installation:**
```bash
# Create test project
mkdir test-install
cd test-install
dotnet new console

# Add package
dotnet add package Mcp.Gateway.Tools --version 1.0.1

# Verify it works
dotnet build
```

3. **Check on nuget.org:**
   - README displays correctly
   - Links work
   - Dependencies listed
   - Release notes visible

---

## 🚨 Common Issues & Solutions

### Issue 1: "Package already exists"
**Solution:** Increment version number (e.g., 1.0.2)

### Issue 2: "Invalid API key"
**Solution:** 
- Regenerate API key
- Check expiration date
- Verify scope includes "Push"

### Issue 3: "Invalid package metadata"
**Solution:**
- Check .csproj has all required fields
- Ensure version format is correct (X.Y.Z)
- Validate license expression

### Issue 4: "Missing dependencies"
**Solution:**
- Add `<FrameworkReference>` to .csproj
- Our package already includes `Microsoft.AspNetCore.App`

### Issue 5: XML Documentation Warnings
**Current Status:** 118 warnings about missing XML comments

**Options:**
1. **Ignore** (works fine, just warnings)
2. **Suppress** (add `<NoWarn>1591</NoWarn>` to .csproj)
3. **Fix** (add XML comments to all public members)

**Recommendation:** Suppress for v1.0.1, fix in v1.1

```xml
<PropertyGroup>
  <NoWarn>1591</NoWarn> <!-- Missing XML comment warnings -->
</PropertyGroup>
```

---

## 🎯 Post-Publishing Tasks

### Immediate
- [ ] Verify package appears on nuget.org
- [ ] Test installation in fresh project
- [ ] Update README.md with installation instructions
- [ ] Tweet/announce release

### Within 24 hours
- [ ] Monitor download stats
- [ ] Check for issues on GitHub
- [ ] Respond to questions

### Optional
- [ ] Add package badge to README
- [ ] Create sample project using NuGet package
- [ ] Write blog post about release

---

## 📊 Package Metrics

After publishing, you can track:
- **Downloads:** https://www.nuget.org/packages/Mcp.Gateway.Tools
- **Stats:** https://www.nuget.org/stats/packages/Mcp.Gateway.Tools?groupby=Version
- **Dependencies:** Which packages depend on yours

---

## 🔐 Security Best Practices

### API Key Management
```bash
# Store in environment variable
$env:NUGET_API_KEY = "your-key"

# Or use dotnet user-secrets (for CI/CD)
dotnet user-secrets set "NuGetApiKey" "your-key"
```

### GitHub Actions (Optional for v1.1)
```yaml
# .github/workflows/publish.yml
name: Publish to NuGet

on:
  release:
    types: [published]

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'
    - name: Pack
      run: dotnet pack -c Release -o nupkgs
    - name: Push to NuGet
      run: dotnet nuget push nupkgs/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

---

## 📝 Version Management

### Semantic Versioning (SemVer)
```
MAJOR.MINOR.PATCH
  |     |     |
  |     |     +-- Bug fixes (1.0.1 → 1.0.2)
  |     +-------- New features, backward compatible (1.0.1 → 1.1.0)
  +-------------- Breaking changes (1.0.1 → 2.0.0)
```

**Our versions:**
- v1.0.1 - Current (performance improvements)
- v1.1.0 - Next (new features, no breaking changes)
- v2.0.0 - Future (Hybrid Tool API, breaking changes OK)

---

## ✅ Publishing Checklist

### Before Publishing
- [x] Version number updated in .csproj
- [x] CHANGELOG.md updated
- [x] All tests passing (45/45)
- [x] No build errors
- [ ] XML documentation warnings handled
- [x] README.md complete
- [x] LICENSE file present
- [ ] NuGet API key ready

### Publishing Steps
- [ ] Clean build (`dotnet clean`)
- [ ] Release build (`dotnet build -c Release`)
- [ ] Create package (`dotnet pack -c Release`)
- [ ] Inspect package contents
- [ ] Push to NuGet.org
- [ ] Wait for validation
- [ ] Verify on nuget.org
- [ ] Test installation

### After Publishing
- [ ] Update README.md installation instructions
- [ ] Add package badge
- [ ] Announce on social media
- [ ] Monitor for issues

---

## 🎨 Package Badge (for README)

After publishing, add this to README.md:

```markdown
[![NuGet](https://img.shields.io/nuget/v/Mcp.Gateway.Tools.svg)](https://www.nuget.org/packages/Mcp.Gateway.Tools/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Mcp.Gateway.Tools.svg)](https://www.nuget.org/packages/Mcp.Gateway.Tools/)
```

---

## 🚀 Ready to Publish?

**Run these commands:**

```bash
# 1. Suppress XML warnings (optional)
# Edit Mcp.Gateway.Tools/Mcp.Gateway.Tools.csproj and add:
# <NoWarn>1591</NoWarn>

# 2. Clean and build
dotnet clean
dotnet build -c Release

# 3. Create package
dotnet pack Mcp.Gateway.Tools -c Release -o nupkgs

# 4. Set API key
$env:NUGET_API_KEY = "paste-your-api-key-here"

# 5. Publish!
dotnet nuget push nupkgs/Mcp.Gateway.Tools.1.0.1.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# 6. Wait 15-30 minutes, then check:
# https://www.nuget.org/packages/Mcp.Gateway.Tools
```

---

## 📞 Support

**Issues?**
- NuGet Support: https://www.nuget.org/policies/Contact
- Documentation: https://learn.microsoft.com/en-us/nuget/

**Questions?**
- Open GitHub Issue
- Check NuGet.org status: https://status.nuget.org/

---

**Created by:** NuGet publishing guide  
**Date:** 5. desember 2025  
**Package:** Mcp.Gateway.Tools v1.0.1  
**Status:** Ready to publish!
