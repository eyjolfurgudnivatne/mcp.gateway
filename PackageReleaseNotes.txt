MCP Gateway v1.8.0 - Quality of Life & Optional MCP Features
============================================================

🎉 WHAT'S NEW:
- ✅ Tool Lifecycle Hooks - Monitor and track tool invocations
- ✅ Resource Subscriptions - Optional MCP 2025-11-25 feature
- ✅ Better Error Messages - Helpful suggestions and schema hints
- ✅ Authorization Support - Role-based access control example

🔧 IMPROVEMENTS:
- Tool not found → suggests similar tool names (Levenshtein distance)
- Invalid params → shows expected schema with examples
- Session expired → clear re-initialization guidance
- Lifecycle hooks → metrics, logging, authorization

📦 RESOURCE SUBSCRIPTIONS:
- Subscribe to specific resource URIs
- Notification filtering by subscription
- Session-based with automatic cleanup
- Exact URI matching (wildcards in v1.9.0)

📊 LIFECYCLE HOOKS:
- IToolLifecycleHook interface for monitoring
- Built-in hooks: LoggingToolLifecycleHook, MetricsToolLifecycleHook
- Track: invocation count, success rate, duration, errors
- Production-ready metrics for Prometheus, Application Insights

🎯 AUTHORIZATION:
- Role-based access control via lifecycle hooks
- Declarative [RequireRole] attribute
- Complete example: AuthorizationMcpServer
- 1200+ lines of documentation

✅ COMPATIBILITY:
- 100% backward compatible with v1.7.x
- Zero breaking changes
- All features are opt-in
- 273/273 tests passing

📚 DOCUMENTATION:
- docs/LifecycleHooks.md - Complete API reference
- docs/Authorization.md - Authorization patterns (1200+ lines)
- docs/ResourceSubscriptions.md - Subscription guide (670+ lines)
- Examples/ResourceMcpServer/README.md - Workflow guide (450+ lines)

🚀 EXAMPLES:
- MetricsMcpServer - Lifecycle hooks with /metrics endpoint
- AuthorizationMcpServer - Role-based authorization
- ResourceMcpServer - Resource subscriptions

For full details, see: https://github.com/eyjolfurgudnivatne/mcp.gateway/releases/tag/v1.8.0
