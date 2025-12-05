# 🎨 Hybrid Tool API - Design Plan (v2.0+)

**Created:** 5. desember 2025  
**Updated:** 6. desember 2025  
**Status:** 📋 Deferred to v2.0+ (Too complex for v1.1)

**Target:** Simplify tool creation while maintaining flexibility  
**Inspiration:** [modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk)

---

## ⚠️ DEFERRED TO v2.0+

**Reason:** After analysis, Hybrid Tool API is too complex for v1.1 release:

1. **XML documentation parsing** - Requires separate XML files, complex runtime parsing
2. **Request context management** - Needs AsyncLocal<T> for request ID
3. **Return type wrapping** - Runtime type checking overhead
4. **Parameter resolution** - Ambiguity between JSON params and DI services
5. **Schema generation** - Complex for nested objects and custom types

**v1.1 Decision:** Focus on **auto-generated tool names** only (simpler, high value)

**See:** `.internal/notes/attributes.md` for full analysis and decision rationale

---
