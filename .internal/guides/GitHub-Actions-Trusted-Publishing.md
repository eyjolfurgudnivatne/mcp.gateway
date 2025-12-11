# 🔐 GitHub Actions + Trusted Publishing Setup

**Created:** 5. desember 2025  
**Status:** Ready to implement  
**For:** MCP Gateway v1.1+  

---

## 🎯 What is Trusted Publishing?

**Trusted Publishing** (via OpenID Connect) allows GitHub Actions to publish NuGet packages **without API keys**.

**Benefits:**
- ✅ No API keys to manage or leak
- ✅ Automatic publish on GitHub release
- ✅ Enhanced security (OIDC tokens)
- ✅ Full audit trail
- ✅ Simplified CI/CD

---

## 📋 Prerequisites

### 1. NuGet.org Account
- [x] Already have account (used for v1.0.1)
- [x] Email verified

### 2. GitHub Repository
- [x] Repository: https://github.com/eyjolfurgudnivatne/mcp.gateway
- [x] Admin access required

---

## 🏗️ Step-by-Step Setup

### Step 1: Configure Trusted Publisher on NuGet.org

1. **Go to NuGet.org:**
   - Navigate to: https://www.nuget.org/account/Packages
   - Find: `Mcp.Gateway.Tools`
   - Click **"Manage Package"**

2. **Add Trusted Publisher:**
   - Click **"Publishing"** tab
   - Click **"Add Trusted Publisher"**
   - Fill in:
     ```
     Provider: GitHub Actions
     Repository Owner: eyjolfurgudnivatne
     Repository Name: mcp.gateway
     Workflow File: .github/workflows/publish.yml
     Environment: (leave empty or use "production")
     ```
   - Click **"Add"**

3. **Verify:**
   - You should see the trusted publisher listed
   - Status: "Active"

---

### Step 2: Create GitHub Actions Workflow

**File:** `.github/workflows/publish.yml`

```yaml
name: Publish to NuGet

on:
  release:
    types: [published]
  workflow_dispatch:  # Allow manual trigger

permissions:
  contents: read
  id-token: write  # Required for OIDC authentication

jobs:
  publish:
    name: Publish NuGet Package
    runs-on: ubuntu-latest
    
    steps:
      # 1. Checkout code
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Full history for versioning
      
      # 2. Setup .NET 10
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      # 3. Restore dependencies
      - name: Restore dependencies
        run: dotnet restore
      
      # 4. Build in Release mode
      - name: Build
        run: dotnet build -c Release --no-restore
      
      # 5. Run tests
      - name: Test
        run: dotnet test -c Release --no-build --verbosity normal
      
      # 6. Pack NuGet package
      - name: Pack
        run: dotnet pack Mcp.Gateway.Tools -c Release --no-build -o nupkgs
      
      # 7. Publish to NuGet.org (using Trusted Publishing - no API key needed!)
      - name: Push to NuGet
        run: dotnet nuget push nupkgs/*.nupkg --source https://api.nuget.org/v3/index.json --skip-duplicate
        env:
          DOTNET_CLI_TELEMETRY_OPTOUT: 1
```

---

### Step 3: Create GitHub Environment (Optional but Recommended)

**Why?** Extra security layer - requires approval before publishing.

1. **Go to GitHub:**
   - Navigate to: https://github.com/eyjolfurgudnivatne/mcp.gateway/settings/environments
   - Click **"New environment"**

2. **Configure:**
   - Name: `production`
   - Protection rules:
     - ✅ Required reviewers (select yourself)
     - ✅ Wait timer: 0 minutes (or 5 min for safety)
   - Click **"Save protection rules"**

3. **Update workflow:**
```yaml
jobs:
  publish:
    name: Publish NuGet Package
    runs-on: ubuntu-latest
    environment: production  # <-- Add this line
```

---

### Step 4: Test the Workflow

#### Option A: Test with Pre-release

1. **Create a pre-release:**
```bash
# Update version to 1.0.2-preview.1
# Edit Mcp.Gateway.Tools/Mcp.Gateway.Tools.csproj:
<Version>1.0.2-preview.1</Version>

# Commit and tag
git add .
git commit -m "test: prepare for automated release"
git tag -a v1.0.2-preview.1 -m "Test release automation"
git push origin main --tags
```

2. **Create GitHub Release:**
   - Go to: https://github.com/eyjolfurgudnivatne/mcp.gateway/releases/new
   - Choose tag: `v1.0.2-preview.1`
   - Check: ✅ "Set as a pre-release"
   - Click **"Publish release"**

3. **Watch GitHub Actions:**
   - Go to: https://github.com/eyjolfurgudnivatne/mcp.gateway/actions
   - Watch workflow run
   - Verify package published to NuGet.org

#### Option B: Manual Trigger (for testing)

1. **Trigger workflow manually:**
   - Go to: https://github.com/eyjolfurgudnivatne/mcp.gateway/actions
   - Select "Publish to NuGet" workflow
   - Click **"Run workflow"**
   - Select branch: `main`
   - Click **"Run workflow"**

---

## 📊 Comparison: Manual vs. Automated

| Aspect | Manual (v1.0.1) | Automated (v1.1+) |
|--------|-----------------|-------------------|
| **API Key** | Required | Not needed! |
| **Security** | Key can leak | OIDC tokens (auto-expire) |
| **Steps** | 5 manual steps | 1 click (create release) |
| **Time** | 10-15 minutes | 5-10 minutes (automated) |
| **Errors** | Human error possible | Consistent, automated |
| **Testing** | Manual | Automated (runs tests first) |
| **Audit** | Limited | Full GitHub Actions logs |

---

## 🎯 Recommended Workflow for v1.1+

### For Regular Releases:

1. **Update version:**
```bash
# Edit Mcp.Gateway.Tools/Mcp.Gateway.Tools.csproj
<Version>1.1.0</Version>

# Update CHANGELOG.md
git add .
git commit -m "release: prepare v1.1.0"
git push origin main
```

2. **Create Git tag:**
```bash
git tag -a v1.1.0 -m "Release v1.1.0"
git push origin v1.1.0
```

3. **Create GitHub Release:**
   - Go to: https://github.com/eyjolfurgudnivatne/mcp.gateway/releases/new
   - Choose tag: `v1.1.0`
   - Fill in release notes
   - Click **"Publish release"** 🚀

4. **Wait for automation:**
   - GitHub Actions runs automatically
   - Tests run
   - Package built
   - Published to NuGet.org
   - Done! ✅

---

## 🔒 Security Best Practices

### 1. Branch Protection

**Recommended settings:**
- Require pull request reviews
- Require status checks to pass
- Require branches to be up to date

**Setup:**
- Go to: https://github.com/eyjolfurgudnivatne/mcp.gateway/settings/branches
- Add rule for `main` branch

### 2. Environment Protection

**For production environment:**
- ✅ Required reviewers (yourself)
- ✅ Wait timer (5 min to cancel if mistake)
- ✅ Deployment branches: `main` only

### 3. Workflow Permissions

**Least privilege principle:**
```yaml
permissions:
  contents: read       # Read code
  id-token: write      # Generate OIDC token
  # Nothing else!
```

---

## 🚨 Troubleshooting

### Issue 1: "Unable to generate OIDC token"

**Cause:** Missing `id-token: write` permission

**Fix:**
```yaml
permissions:
  id-token: write
```

### Issue 2: "Package already exists"

**Cause:** Version not incremented

**Fix:**
- Update version in `.csproj`
- Use `--skip-duplicate` flag (already in workflow)

### Issue 3: "Untrusted publisher"

**Cause:** Trusted publisher not configured on NuGet.org

**Fix:**
- Go to NuGet.org package settings
- Add trusted publisher (see Step 1)

### Issue 4: Tests fail in CI

**Cause:** Environment differences

**Fix:**
- Test locally first: `dotnet test -c Release`
- Check GitHub Actions logs for details

---

## 📝 Complete Workflow File

**File:** `.github/workflows/publish.yml`

```yaml
name: Publish to NuGet

on:
  release:
    types: [published]
  workflow_dispatch:

permissions:
  contents: read
  id-token: write

jobs:
  publish:
    name: Build and Publish
    runs-on: ubuntu-latest
    environment: production  # Optional: requires approval
    
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore
        run: dotnet restore
      
      - name: Build
        run: dotnet build -c Release --no-restore
      
      - name: Test
        run: dotnet test -c Release --no-build --verbosity normal
      
      - name: Pack
        run: dotnet pack Mcp.Gateway.Tools -c Release --no-build -o nupkgs
      
      - name: Push to NuGet
        run: dotnet nuget push nupkgs/*.nupkg --source https://api.nuget.org/v3/index.json --skip-duplicate
      
      - name: Summary
        run: |
          echo "✅ Package published successfully!" >> $GITHUB_STEP_SUMMARY
          echo "📦 Version: ${{ github.ref_name }}" >> $GITHUB_STEP_SUMMARY
          echo "🔗 NuGet: https://www.nuget.org/packages/Mcp.Gateway.Tools" >> $GITHUB_STEP_SUMMARY
```

---

## 🎯 Migration Plan

### For v1.0.1 (Current)
- ✅ Already published manually
- ✅ Works perfectly
- ✅ No changes needed

### For v1.1.0 (Next Release)
- [ ] Setup trusted publisher on NuGet.org
- [ ] Create `.github/workflows/publish.yml`
- [ ] Test with pre-release (v1.1.0-preview.1)
- [ ] Use automated workflow for v1.1.0 stable

### For v2.0.0 (Future)
- ✅ Fully automated
- ✅ Trusted publishing enabled
- ✅ No API keys to manage

---

## 📚 Additional Resources

**Official Documentation:**
- [NuGet Trusted Publishers](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package#trusted-publishers)
- [GitHub OIDC](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/about-security-hardening-with-openid-connect)
- [dotnet nuget push](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-push)

**Example Workflows:**
- [.NET Foundation](https://github.com/dotnet/runtime/blob/main/.github/workflows/publish.yml)
- [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet/blob/master/.github/workflows/publish.yml)

---

## ✅ Recommendation

**For v1.0.1:**
- ✅ Already published manually - perfect!
- ✅ No action needed now

**For v1.1.0:**
- ✅ Setup GitHub Actions + Trusted Publishing
- ✅ Test automation before stable release
- ✅ Save time and improve security

**Effort:**
- Setup: ~30 minutes (one-time)
- Future releases: ~5 minutes (just create GitHub release!)

---

## 🎊 Summary

**Trusted Publishing Benefits:**
1. **No API keys** (zero leak risk)
2. **Automated** (create release → auto-publish)
3. **Secure** (OIDC tokens, short-lived)
4. **Auditable** (full GitHub Actions logs)
5. **Faster** (5 min vs. 15 min per release)

**Next Steps:**
1. Use manual process for v1.0.1 ✅ (already done!)
2. Setup automation for v1.1.0 📋 (optional but recommended)
3. Enjoy fully automated releases for v2.0.0+ 🚀

---

**Created by:** GitHub Actions automation guide  
**Date:** 5. desember 2025  
**Status:** Ready to implement  
**Recommended:** Yes for v1.1+
