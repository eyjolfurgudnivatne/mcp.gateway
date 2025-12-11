# 🚀 GitHub Release Automation

**Automated release workflow for MCP Gateway**

---

## 🎯 How It Works

### Trigger Release

**Option 1: Git Tag (Automatic)**
```bash
# Create and push a version tag
git tag v1.1.0
git push origin v1.1.0
```

**Option 2: Manual Trigger**
1. Go to: https://github.com/eyjolfurgudnivatne/mcp.gateway/actions/workflows/release.yml
2. Click "Run workflow"
3. Select branch
4. Click "Run workflow"

---

## 📋 What Happens

### Workflow Steps

1. **Checkout code** (full history)
2. **Setup .NET 10**
3. **Extract version** from tag (e.g., `v1.1.0` → `1.1.0`)
4. **Restore dependencies**
5. **Build** (Release mode with version)
6. **Run tests** (all 45+ tests)
7. **Pack NuGet package** (with version)
8. **Create GitHub Release**
   - Attach `.nupkg` and `.snupkg` files
   - Use release notes from `.github/release-notes.md`
   - Mark as prerelease if version contains `-` (e.g., `1.1.0-beta`)
9. **Publish to NuGet.org** (only stable releases)
10. **Upload artifacts** (for download)

---

## 🔧 Configuration

### Trusted Publishing (Recommended) ✨

**No API keys needed!** Uses OIDC for secure authentication.

**Setup on NuGet.org:**

1. Go to: https://www.nuget.org/packages/Mcp.Gateway.Tools/manage
2. Click **"Trusted Publishers"** tab
3. Click **"Add"**
4. Fill in:
   - **Package owner:** eyjolfurgudnivatne (your username or org)
   - **Repository owner:** eyjolfurgudnivatne
   - **Repository name:** mcp.gateway
   - **Workflow file:** `.github/workflows/release.yml`
   - **Environment:** (leave empty or use `production`)
5. Click **"Add"**

**Benefits:**
- ✅ No secrets to manage
- ✅ More secure (short-lived tokens via OIDC)
- ✅ Automatic authentication
- ✅ Zero-trust architecture

---

### API Key (Legacy - Not Recommended)

<details>
<summary>Click to expand legacy API key setup</summary>

**Only use if Trusted Publishing is not available.**

**Get NuGet API key:**
1. Go to: https://www.nuget.org/account/apikeys
2. Create new API key
   - Key name: `GitHub Actions - MCP Gateway`
   - Glob pattern: `Mcp.Gateway.Tools`
   - Scopes: `Push new packages and package versions`
3. Copy key

**Add to GitHub Secrets:**
1. Go to: https://github.com/eyjolfurgudnivatne/mcp.gateway/settings/secrets/actions
2. Click "New repository secret"
3. Name: `NUGET_API_KEY`
4. Value: (paste API key)

**Update workflow:**
```yaml
- name: Publish to NuGet.org
  run: |
    dotnet nuget push ./nupkgs/*.nupkg \
      --api-key ${{ secrets.NUGET_API_KEY }} \
      --source https://api.nuget.org/v3/index.json
```

</details>

---

## 📝 Release Checklist

### Before Release

- [ ] All tests passing locally (`dotnet test`)
- [ ] CHANGELOG.md updated with release notes
- [ ] Version bumped in:
  - [ ] `Mcp.Gateway.Tools/Mcp.Gateway.Tools.csproj` (optional - tag overrides)
  - [ ] README.md (version badges)
  - [ ] Documentation (if version-specific)
- [ ] Update `.github/release-notes.md` with release highlights

### Create Release

```bash
# 1. Commit all changes
git add .
git commit -m "chore: prepare v1.1.0 release"
git push origin main

# 2. Create and push tag
git tag v1.1.0
git push origin v1.1.0
```

### After Release

- [ ] Verify GitHub release created
- [ ] Verify NuGet package published
- [ ] Test installation: `dotnet add package Mcp.Gateway.Tools`
- [ ] Announce release (if applicable)

---

## 🎨 Version Naming

**Stable Releases:**
```
v1.0.0    → NuGet: 1.0.0 (published)
v1.1.0    → NuGet: 1.1.0 (published)
v2.0.0    → NuGet: 2.0.0 (published)
```

**Prerelease Versions:**
```
v1.1.0-beta    → NuGet: 1.1.0-beta (NOT published)
v1.1.0-rc1     → NuGet: 1.1.0-rc1 (NOT published)
v1.1.0-alpha.1 → NuGet: 1.1.0-alpha.1 (NOT published)
```

**Note:** Prerelease versions create GitHub releases but **do not** publish to NuGet.org

---

## 🐛 Troubleshooting

### Release Failed

**Check logs:**
1. Go to: https://github.com/eyjolfurgudnivatne/mcp.gateway/actions
2. Click on failed workflow run
3. Review error logs

**Common issues:**

**Tests failing:**
```bash
# Run tests locally first
dotnet test
```

**NuGet publish failed:**
- Check Trusted Publishing is configured on NuGet.org
  - Go to: https://www.nuget.org/packages/Mcp.Gateway.Tools/manage
  - Check "Trusted Publishers" tab
- Verify workflow file path matches: `.github/workflows/release.yml`
- Check if package version already exists
- **Fallback:** Use API key (see Configuration section)

**Version extraction failed:**
```bash
# Tag must start with 'v'
git tag v1.1.0  ✅ Good
git tag 1.1.0   ❌ Bad (won't trigger)
```

---

## 🔄 Manual Release (Fallback)

If automated workflow fails, release manually:

```bash
# 1. Build and test
dotnet build -c Release
dotnet test -c Release

# 2. Pack NuGet
dotnet pack Mcp.Gateway.Tools/Mcp.Gateway.Tools.csproj \
  -c Release \
  -o ./nupkgs \
  /p:PackageVersion=1.1.0

# 3. Push to NuGet
dotnet nuget push ./nupkgs/Mcp.Gateway.Tools.1.1.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json

# 4. Create GitHub release manually
# Go to: https://github.com/eyjolfurgudnivatne/mcp.gateway/releases/new
```

---

## 📊 Release History

Track releases:
- **GitHub Releases**: https://github.com/eyjolfurgudnivatne/mcp.gateway/releases
- **NuGet Packages**: https://www.nuget.org/packages/Mcp.Gateway.Tools/

---

## 🎯 Example: v1.1.0 Release

### Step-by-Step

```bash
# 0. Setup Trusted Publishing (one-time)
# Go to: https://www.nuget.org/packages/Mcp.Gateway.Tools/manage
# Add trusted publisher: eyjolfurgudnivatne/mcp.gateway

# 1. Update CHANGELOG.md
# Add release notes for v1.1.0

# 2. Update release-notes.md
# Highlight key features

# 3. Commit changes
git add .
git commit -m "chore: prepare v1.1.0 release

- Auto-generated tool names
- GitHub Actions testing
- Updated documentation"

git push origin main

# 4. Create tag
git tag v1.1.0

# 5. Push tag (triggers workflow)
git push origin v1.1.0

# 6. Wait for workflow to complete
# Check: https://github.com/eyjolfurgudnivatne/mcp.gateway/actions

# 7. Verify release
# https://github.com/eyjolfurgudnivatne/mcp.gateway/releases/tag/v1.1.0
```

---

## 🚀 Next Steps

After automated release setup:

1. **Test workflow** with a prerelease tag (`v1.1.0-beta`)
2. **Verify NuGet secret** is configured correctly
3. **Document release process** in CONTRIBUTING.md
4. **Create release templates** for different release types

---

## 📚 Resources

- [GitHub Actions - Creating Releases](https://docs.github.com/en/repositories/releasing-projects-on-github/automatically-generated-release-notes)
- [NuGet Package Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
- [Semantic Versioning](https://semver.org/)

---

**Created:** 5. desember 2025  
**Status:** Active  
**Workflow:** `.github/workflows/release.yml`
