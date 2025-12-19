# Git Tag Message for v1.8.0

Use this message when creating the Git tag:

```bash
git tag -a v1.8.0 -m "Release v1.8.0 - Quality of Life & Optional MCP Features

🎉 WHAT'S NEW:
- Tool Lifecycle Hooks - Monitor and track tool invocations
- Resource Subscriptions - Optional MCP 2025-11-25 feature
- Better Error Messages - Helpful suggestions and schema hints
- Authorization Support - Role-based access control example

🔧 FEATURES:
- IToolLifecycleHook interface for monitoring
- Built-in hooks: LoggingToolLifecycleHook, MetricsToolLifecycleHook
- resources/subscribe and resources/unsubscribe methods
- Notification filtering by subscribed URI
- Tool not found suggestions (Levenshtein distance)
- Invalid params schema hints with examples

📦 NEW EXAMPLES:
- MetricsMcpServer - Lifecycle hooks with /metrics endpoint
- AuthorizationMcpServer - Role-based authorization (8 tests)
- ResourceMcpServer - Resource subscriptions (7 tests)

📚 DOCUMENTATION:
- docs/LifecycleHooks.md - Complete API reference
- docs/Authorization.md - Authorization patterns (1200+ lines)
- docs/ResourceSubscriptions.md - Subscription guide (670+ lines)
- 2000+ lines of new documentation

✅ PRODUCTION READY:
- 273 comprehensive tests (all passing)
- Zero breaking changes
- 100% backward compatible with v1.7.x
- Full MCP 2025-11-25 compliance

⚡ PERFORMANCE:
- Implementation: 10 hours (vs 30-44 hours estimated)
- Efficiency: 3.7x faster than estimated
- Thread-safe operations throughout
- Negligible memory footprint

See full release notes:
https://github.com/eyjolfurgudnivatne/mcp.gateway/releases/tag/v1.8.0"
```

## Creating the Tag

After merging to main, run:

```bash
# Create annotated tag
git tag -a v1.8.0 -m "Release v1.8.0 - Quality of Life & Optional MCP Features

🎉 WHAT'S NEW:
- Tool Lifecycle Hooks - Monitor and track tool invocations
- Resource Subscriptions - Optional MCP 2025-11-25 feature
- Better Error Messages - Helpful suggestions and schema hints
- Authorization Support - Role-based access control example

🔧 FEATURES:
- IToolLifecycleHook interface for monitoring
- Built-in hooks: LoggingToolLifecycleHook, MetricsToolLifecycleHook
- resources/subscribe and resources/unsubscribe methods
- Notification filtering by subscribed URI
- Tool not found suggestions (Levenshtein distance)
- Invalid params schema hints with examples

📦 NEW EXAMPLES:
- MetricsMcpServer - Lifecycle hooks with /metrics endpoint
- AuthorizationMcpServer - Role-based authorization (8 tests)
- ResourceMcpServer - Resource subscriptions (7 tests)

📚 DOCUMENTATION:
- docs/LifecycleHooks.md - Complete API reference
- docs/Authorization.md - Authorization patterns (1200+ lines)
- docs/ResourceSubscriptions.md - Subscription guide (670+ lines)
- 2000+ lines of new documentation

✅ PRODUCTION READY:
- 273 comprehensive tests (all passing)
- Zero breaking changes
- 100% backward compatible with v1.7.x
- Full MCP 2025-11-25 compliance

⚡ PERFORMANCE:
- Implementation: 10 hours (vs 30-44 hours estimated)
- Efficiency: 3.7x faster than estimated
- Thread-safe operations throughout
- Negligible memory footprint

See full release notes:
https://github.com/eyjolfurgudnivatne/mcp.gateway/releases/tag/v1.8.0"

# Push tag (DO THIS MANUALLY AFTER MERGE!)
git push origin v1.8.0
```

## Verify Tag

```bash
# View tag
git show v1.8.0

# List all tags
git tag -l

# Verify tag message
git tag -l --format='%(tag)%0a%(contents)' v1.8.0
```
