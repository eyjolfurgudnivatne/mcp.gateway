# 📂 Internal Documentation

**Development notes and guides for MCP Gateway**

This folder contains internal notes, guides, and release documentation that are:
- ✅ **Committed to Git** (visible to contributors)
- ✅ **Development philosophy** (share approach and decisions)
- ✅ **Process guides** (release, CI/CD, publishing)
- ✅ **Historical context** (session notes, design decisions)

**Note:** Changed from `.gitignore` to **visible in Git** to share development insights with contributors!

---

## 📋 What's Here

### `guides/` - Process Documentation
- `GitHub-Actions-Testing.md` - CI/CD testing guide
- `GitHub-Actions-Trusted-Publishing.md` - Automated NuGet publishing with OIDC
- `GitHub-Release-Automation.md` - Automated release workflow guide
- `NuGet-Publishing-Guide.md` - Manual NuGet package publishing
- `RELEASE_INSTRUCTIONS.md` - Release process checklist

### `notes/` - Development Notes & Decisions
- `attributes.md` - Tool attribute design notes
- `Auto-Generated-Tool-Names.md` - Auto-naming feature documentation
- `ArrayPool-Implementation.md` - ArrayPool optimization (v1.0.1)
- `HybridToolAPI-Plan.md` - Hybrid API design (deferred to v2.0)
- `Performance-Optimization-Plan.md` - Performance roadmap and benchmarks
- `Quick-Wins-Session-Summary.md` - Quick wins session notes
- `v.1.2.0/` - Version-specific implementation notes
  - `README.md` - Overview of v1.2.0 changes
  - `implementation-plan.md` - Ollama integration plan
  - `phase-0-progress.md` - Tool capabilities implementation progress
  - `phase-0-tool-capabilities.md` - Design document
  - `ollama-integration.md` - Ollama provider design
  - `ollama-reverse-integration.md` - Alternative integration approach

### `releases/` - Release-Specific Documentation
- `v1.0.1/` - v1.0.1 release notes
  - `RELEASE_CHECKLIST.md` - Release checklist
  - `v1.0.1-Release-Summary.md` - Release summary
- `v1.1.0/` - v1.1.0 release notes
  - `release-notes.md` - Release notes

---

## 🎯 Purpose

**Why these docs are in Git:**
- Share development philosophy with contributors
- Document design decisions and trade-offs
- Provide historical context for future development
- Show thought process behind features
- Help new contributors understand the project

**These docs don't belong in `docs/`** because they are:
- Development-focused (not user-focused)
- Process-oriented (not feature documentation)
- Historical/contextual (not current API docs)

---

## 📚 Public Documentation

For user-facing documentation, see:
- `docs/` - Public documentation (MCP protocol, streaming, JSON-RPC)
- `README.md` - Project overview and quick start
- `CONTRIBUTING.md` - Contribution guidelines
- `CHANGELOG.md` - Version history

---

## 🗂️ Organization Philosophy

### Clear Separation

```
docs/              # Public: User/API documentation
  ├── MCP-Protocol.md
  ├── StreamingProtocol.md
  └── JSON-RPC-2.0-spec.md

.internal/         # Development: Process & decisions
  ├── guides/      # How-to guides (release, CI/CD)
  ├── notes/       # Design decisions, session notes
  └── releases/    # Version-specific release docs
```

### File Naming

- Use descriptive names
- Include version numbers when relevant (`v1.0.1-Release-Summary.md`)
- Use kebab-case (`performance-optimization-plan.md`) or PascalCase (`ArrayPool-Implementation.md`)
- Prefer markdown (`.md`) for all documentation

---

## 🚀 Contributing

### Adding New Notes

**Session notes:**
```bash
# Create a new session note
code .internal/notes/session-$(date +%Y-%m-%d).md
```

**Version-specific notes:**
```bash
# Add to version folder
code .internal/notes/v.1.3.0/feature-implementation.md
```

**Process guides:**
```bash
# Add new guide
code .internal/guides/new-process-guide.md
```

### Adding Release Documentation

```bash
# Create release folder
mkdir -p .internal/releases/v1.2.0

# Add release docs
code .internal/releases/v1.2.0/release-notes.md
code .internal/releases/v1.2.0/RELEASE_CHECKLIST.md
```

---

## 📝 Templates

### Session Notes Template

```markdown
# Session Notes - [Date]

**Focus:** [Main topic/feature]

## Decisions Made
- Decision 1: [Rationale]
- Decision 2: [Rationale]

## Implementation Notes
- Note 1
- Note 2

## Deferred/Blocked
- Item 1: [Reason]

## References
- [Link to PR/issue]
- [Link to documentation]
```

### Release Checklist Template

See `.internal/releases/v1.0.1/RELEASE_CHECKLIST.md` for example.

---

## 🔍 Finding Information

### By Version
- Check `.internal/releases/v[version]/` for release-specific docs
- Check `.internal/notes/v.[version]/` for implementation notes

### By Topic
- **Performance:** `Performance-Optimization-Plan.md`, `ArrayPool-Implementation.md`
- **Features:** `Auto-Generated-Tool-Names.md`, `HybridToolAPI-Plan.md`
- **Process:** `guides/` folder
- **Design:** `notes/attributes.md`, phase documents

### By Date
- Session notes include dates in filename or metadata
- Release folders organized by version/date

---

## 🔐 What NOT to Commit

Even though this folder is in Git, **DO NOT COMMIT:**
- ❌ API keys or secrets
- ❌ Passwords or credentials
- ❌ Personal information
- ❌ Sensitive data
- ❌ Large binary files

**Use GitHub Secrets** for sensitive data in CI/CD workflows.

---

## 🎯 Goals

1. **Transparency** - Share development process openly
2. **Context** - Preserve decision-making rationale
3. **Learning** - Help contributors understand the "why"
4. **History** - Document evolution of features
5. **Collaboration** - Make it easy for others to contribute

---

**Created:** 6. desember 2025  
**Updated:** 7. desember 2025  
**Status:** Active (visible in Git)  
**Purpose:** Share development philosophy and process with contributors
