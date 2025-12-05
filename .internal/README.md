# 📂 Internal Documentation

**Local-only documentation for MCP Gateway development**

This folder contains internal notes, checklists, and guides that are:
- ✅ **Not committed to Git** (via `.gitignore`)
- ✅ **Local reference only** (historical notes)
- ✅ **Development helpers** (release checklists, etc.)

---

## 📋 What's Here

### Release Management
- `RELEASE_CHECKLIST.md` - Step-by-step release checklist
- `RELEASE_INSTRUCTIONS.md` - Detailed release instructions
- `NuGet-Publishing-Guide.md` - How to publish to NuGet.org
- `GitHub-Actions-Trusted-Publishing.md` - Trusted publishing setup

### Development Notes
- Session summaries
- Performance analysis notes
- Design decisions
- Quick reference guides

---

## 🔒 Privacy Note

**This folder is in `.gitignore`** - contents are never pushed to GitHub.

Use this for:
- ✅ Personal notes
- ✅ Sensitive information (API keys, credentials)
- ✅ Work-in-progress drafts
- ✅ Historical reference documents
- ✅ Quick checklists

---

## 📚 Public Documentation

For public-facing docs, use:
- `docs/` - Main documentation folder (committed to Git)
- `README.md` - Project overview
- `CONTRIBUTING.md` - Contribution guidelines
- `CHANGELOG.md` - Version history

---

## 🗂️ Organization Tips

### Recommended Structure

```
.internal/
├── README.md (this file)
├── releases/
│   ├── v1.0.0/
│   │   ├── checklist.md
│   │   └── notes.md
│   └── v1.0.1/
│       ├── checklist.md
│       └── performance-notes.md
├── guides/
│   ├── NuGet-Publishing-Guide.md
│   └── GitHub-Actions-Trusted-Publishing.md
└── notes/
    ├── session-2025-12-05.md
    └── ideas.md
```

### File Naming

- Use descriptive names
- Include dates when relevant
- Use lowercase-with-dashes or PascalCase

---

## 🚀 Quick Start

### Move Existing Docs

```bash
# Create folder structure
mkdir -p .internal/guides
mkdir -p .internal/releases/v1.0.1

# Move existing docs (if any)
mv RELEASE_CHECKLIST.md .internal/releases/v1.0.1/
mv NuGet-Publishing-Guide.md .internal/guides/
```

### Create New Note

```bash
# Create a new session note
code .internal/notes/session-2025-12-06.md
```

---

## 📝 Example: Session Notes Template

```markdown
# Session Notes - [Date]

**Topics:**
- Topic 1
- Topic 2

**Decisions:**
- Decision 1
- Decision 2

**Action Items:**
- [ ] Action 1
- [ ] Action 2

**References:**
- Link 1
- Link 2
```

---

## 🔐 Security Reminder

**DO NOT COMMIT:**
- ❌ API keys
- ❌ Passwords
- ❌ Credentials
- ❌ Personal information
- ❌ Sensitive data

**This folder is gitignored**, but double-check before committing!

---

**Created:** 6. desember 2025  
**Status:** Active (local only)  
**Purpose:** Internal development reference
