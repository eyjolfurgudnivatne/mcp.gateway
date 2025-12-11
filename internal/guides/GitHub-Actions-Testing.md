# 🧪 GitHub Actions - Automated Testing

**Automatic test execution on every push/merge to `main`**

---

## ✅ What's Configured

### Workflow: `.github/workflows/test.yml`

**Triggers:**
- ✅ Push to `main` branch
- ✅ Pull requests to `main` branch
- ✅ Manual trigger (workflow_dispatch)

**Steps:**
1. Checkout code
2. Setup .NET 10 SDK
3. Restore dependencies
4. Build in Release mode
5. Run all tests (45+ tests)
6. Generate test report
7. Create summary

---

## 📊 What You Get

### Test Status Badge

Added to README.md:
```markdown
[![Tests](https://github.com/eyjolfurgudnivatne/mcp.gateway/actions/workflows/test.yml/badge.svg)](...)
```

Shows:
- ✅ Green badge = All tests passing
- ❌ Red badge = Tests failing

### Test Reports

After each run:
- Full test results in GitHub Actions UI
- Test summary in workflow summary
- Failed tests highlighted

---

## 🚀 How to Use

### View Test Results

1. Go to: https://github.com/eyjolfurgudnivatne/mcp.gateway/actions
2. Click on latest workflow run
3. View test results and logs

### Manual Trigger

1. Go to Actions tab
2. Select "Tests" workflow
3. Click "Run workflow"
4. Select branch
5. Click "Run workflow"

---

## 🔍 Example Output

### Successful Run
```
✅ All tests passed!

Tests: 45 passed, 0 failed, 0 skipped
Duration: ~30 seconds
```

### Failed Run
```
❌ Some tests failed!

Tests: 43 passed, 2 failed, 0 skipped

Failed tests:
- Add_Numbers_ReturnsCorrectSum
- Multiply_Numbers_WithZero
```

---

## 📋 What Runs

All test projects:
- `Mcp.Gateway.Tests` - 45+ comprehensive tests
  - HTTP RPC tests
  - WebSocket tests
  - SSE tests
  - stdio tests
  - MCP protocol tests
  - Streaming tests (binary, text, duplex)
  - Tool discovery tests

---

## ⚙️ Workflow Configuration

### Environment

- **OS:** Ubuntu Latest (Linux)
- **.NET:** 10.0.x (latest patch)
- **Build:** Release configuration
- **Verbosity:** Normal (shows test names and results)

### Permissions

- `contents: read` - Read repository code
- `checks: write` - Write test reports

---

## 🎯 Next Steps (Optional)

### Add Code Coverage

Add to workflow:
```yaml
- name: Test with Coverage
  run: dotnet test --configuration Release --no-build --collect:"XPlat Code Coverage"

- name: Upload Coverage
  uses: codecov/codecov-action@v3
  with:
    files: '**/coverage.cobertura.xml'
```

### Add Performance Benchmarks

Create `benchmark.yml`:
```yaml
- name: Run Benchmarks
  run: dotnet run -c Release --project Mcp.Gateway.Benchmarks
```

### Add Multiple .NET Versions

Test against multiple versions:
```yaml
strategy:
  matrix:
    dotnet: [ '10.0.x', '11.0.x' ]
```

---

## 🐛 Troubleshooting

### Tests Fail Locally But Pass on GitHub

- Check .NET version: `dotnet --version`
- Run in Release mode: `dotnet test -c Release`
- Check for platform-specific code

### Tests Pass Locally But Fail on GitHub

- Check file paths (use `Path.Combine`)
- Check environment variables
- Review GitHub Actions logs

### Workflow Doesn't Trigger

- Ensure workflow is in `main` branch
- Check `.github/workflows/test.yml` exists
- Verify workflow syntax (YAML)

---

## 📚 Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/)
- [Test Reporter Action](https://github.com/dorny/test-reporter)

---

## ✅ Verification

After pushing to `main`:

1. **Check Actions tab**
   - https://github.com/eyjolfurgudnivatne/mcp.gateway/actions

2. **Verify badge**
   - README should show green Tests badge

3. **View results**
   - Click on workflow run for details

---

**Created:** 6. desember 2025  
**Status:** Active  
**Tests:** 45+ comprehensive tests  
**Duration:** ~30 seconds per run
