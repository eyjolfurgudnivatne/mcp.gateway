# 📂 Internal Documentation

Development notes and guides for MCP Gateway.

This folder contains internal notes, guides, and release documentation that are:
- ✅ Committed to Git (visible to contributors)
- ✅ Focused on development process and decisions
- ✅ Used for release management and CI/CD
- ✅ A historical record of design and performance work

> These docs are **not** user-facing API docs; they are meant for maintainers and contributors.

---

## 📋 Folder Overview

### `guides/` – Process & Operations

Process documentation and operational runbooks:

- `GitHub-Actions-Testing.md` – CI/CD testing guide
- `GitHub-Actions-Trusted-Publishing.md` – NuGet publishing with OIDC / Trusted Publishing
- `GitHub-Release-Automation.md` – Automated GitHub release workflow
- `NuGet-Publishing-Guide.md` – Manual NuGet publishing steps
- `RELEASE_INSTRUCTIONS.md` – High-level release checklist for v1.0.0 (kept as reference)

Use this folder when you:
- Set up or change CI/CD
- Prepare a release
- Need to (re)publish the NuGet package manually

---

### `notes/` – Design, Decisions & Experiments

Working notes and design documents:

- `attributes.md` – Tool attribute design and philosophy
- `Auto-Generated-Tool-Names.md` – Design and behaviour of auto-named tools
- `ArrayPool-Implementation.md` – WebSocket buffer optimization (v1.0.1)
- `HybridToolAPI-Plan.md` – Hybrid Tool API concept (deferred to v2.0+)
- `Performance-Optimization-Plan.md` – Performance roadmap and benchmark strategy
- `Quick-Wins-Session-Summary.md` – Quick wins performance session summary
- `v.1.2.0/` – Version-specific implementation notes for v1.2.0
  - `README.md` – Overview of v1.2.0 scope
  - `implementation-plan.md` – Ollama integration plan
  - `phase-0-progress.md` – Tool capabilities implementation progress
  - `phase-0-tool-capabilities.md` – Design document for `ToolCapabilities` and transport filtering
  - `ollama-integration.md` – Ollama provider design
  - `ollama-reverse-integration.md` – Alternative Ollama integration approach

Use this folder when you:
- Want to understand **why** something is implemented a certain way
- Need historical context for a feature (tool attributes, capabilities, performance)
- Plan future work (v1.3+, v2.0)

---

### `releases/` – Release-Specific Docs

Release notes and checklists per version:

- `v1.0.1/`
  - `RELEASE_CHECKLIST.md` – Detailed release checklist used for v1.0.1
  - `v1.0.1-Release-Summary.md` – Summary of v1.0.1 changes
- `v1.1.0/`
  - `release-notes.md` – v1.1.0 release notes
- `v1.2.0/`
  - `release-note.md` – v1.2.0 release notes (internal, used as basis for GitHub release text)

Use this folder when you:
- Prepare a new release (copy from a previous version folder)
- Need to see **what actually shipped** in a given version
- Write or update public release notes / GitHub releases / CHANGELOG entries

---

## 🎯 Purpose of Internal Docs

Why these docs live in `.internal/` instead of `docs/`:

- Development-focused (not end-user or API reference)
- Capture design decisions, trade-offs and alternatives
- Preserve performance experiments and benchmark results
- Describe release and CI/CD processes in more detail than public docs
- Help new contributors ramp up quickly on the **internal** architecture and workflow

Public, user-facing documentation lives in:

- `docs/` – MCP protocol, streaming protocol, JSON-RPC spec
- `README.md` – Product overview and quick start for MCP Gateway
- `Mcp.Gateway.Tools/README.md` – Library usage for tool authors
- `CONTRIBUTING.md` – Contribution and code style guidelines
- `CHANGELOG.md` – Version history

---

## 🗂️ Organization Philosophy

### Clear Separation

```text
docs/              # Public: User/API documentation
  ├── MCP-Protocol.md
  ├── StreamingProtocol.md
  └── JSON-RPC-2.0-spec.md

.internal/         # Development: Process & decisions
  ├── guides/      # How-to guides (release, CI/CD, publishing)
  ├── notes/       # Design decisions, performance, experiments
  └── releases/    # Version-specific release docs
```

### File Naming

- Use descriptive names
- Include version numbers where it adds value (e.g. `v1.0.1-Release-Summary.md`)
- Prefer:
  - kebab-case for general docs (`performance-optimization-plan.md`)
  - PascalCase where it matches existing style (`ArrayPool-Implementation.md`)
- Use Markdown (`.md`) for all documentation

---

## 🚀 Adding New Internal Docs

### New session / design notes

```bash
# Create a new session note
code .internal/notes/session-$(date +%Y-%m-%d).md
```

### Version-specific notes

```bash
# Add a new version folder for implementation notes
mkdir -p .internal/notes/v.1.3.0
code .internal/notes/v.1.3.0/feature-implementation.md
```

### New process guides

```bash
# Add a new process/ops guide
code .internal/guides/new-process-guide.md
```

### New release documentation

```bash
# Create release folder for a new version
mkdir -p .internal/releases/v1.3.0

# Add release docs
code .internal/releases/v1.3.0/release-notes.md
code .internal/releases/v1.3.0/RELEASE_CHECKLIST.md
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

See `.internal/releases/v1.0.1/RELEASE_CHECKLIST.md` for a concrete example you can copy.

---

## 🔍 Finding Information

### By Version
- `.internal/releases/v[version]/` – What shipped in a given version
- `.internal/notes/v.[version]/` – Implementation details and design notes for that version

### By Topic
- **Performance:** `Performance-Optimization-Plan.md`, `ArrayPool-Implementation.md`, quick-wins notes
- **Features:** `Auto-Generated-Tool-Names.md`, `HybridToolAPI-Plan.md`, v1.2.0 notes
- **Process:** `guides/` folder (releases, CI/CD, publishing)
- **Design:** `notes/attributes.md`, `phase-0-tool-capabilities.md`, and other phase/plan docs

### By Date
- Session notes: filename or header contains the date
- Release folders: organized by semantic version (v1.0.1, v1.1.0, v1.2.0, ...)

---

## 🔐 What NOT to Store Here

Even though `.internal/` is committed to Git, **do NOT commit**:

- API keys or secrets
- Passwords or credentials
- Personal or sensitive information
- Large binary files or datasets

Use GitHub Secrets (or other secret stores) for anything sensitive used in CI/CD.

---

## 🎯 Goals

1. **Transparency** – Share the internal development process openly
2. **Context** – Preserve the reasoning behind important decisions
3. **Learning** – Help new contributors understand the "why" behind the code
4. **History** – Track how features and performance evolved over time
5. **Collaboration** – Make it easy to extend, debug and release MCP Gateway safely

---

**Created:** 6 December 2025  
**Last Updated:** 12 December 2025  
**Status:** Active (visible in Git)  
**Purpose:** Internal reference for MCP Gateway maintainers and contributors
