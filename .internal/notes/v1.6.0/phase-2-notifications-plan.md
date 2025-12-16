# Phase 2: Notifications Implementation Plan

**Status:** In Progress  
**Date:** 16. desember 2025  
**Goal:** Implement MCP notifications infrastructure for dynamic updates

---

## 🎯 Overview

Implement notification system that allows server to push updates to clients when:
- Tools are added/removed/changed
- Prompts are updated
- Resources are modified

---

## 📋 Implementation Steps

### Step 1: Notification Models (30 min)
**File:** `Mcp.Gateway.Tools/Notifications/NotificationModels.cs`

Create models for:
- `NotificationType` enum (ToolsChanged, PromptsChanged, ResourcesUpdated)
- `NotificationMessage` record
- `INotificationSender` interface

### Step 2: NotificationService (45 min)
**File:** `Mcp.Gateway.Tools/Notifications/NotificationService.cs`

Implement:
- Subscriber management (WebSocket connections)
- Send notification to all subscribers
- Thread-safe subscriber list

### Step 3: Update ToolInvoker (30 min)
**File:** `Mcp.Gateway.Tools/ToolInvoker.Notifications.cs` (new partial)

Add handlers for:
- Sending notifications via WebSocket
- Storing last notification state

### Step 4: Update Initialize Capabilities (15 min)
**File:** `Mcp.Gateway.Tools/ToolInvoker.Protocol.cs`

Add notification capabilities to `initialize` response:
```json
{
  "capabilities": {
    "tools": {},
    "prompts": {},
    "resources": {},
    "notifications": {
      "tools": {},
      "prompts": {},
      "resources": {}
    }
  }
}
```

### Step 5: Create NotificationMcpServer Example (1 hour)
**Directory:** `Examples/NotificationMcpServer`

Features:
- Dynamic tool registration/unregistration
- File watcher for resource changes
- WebSocket notification demo
- Hot-reload example

### Step 6: Create NotificationMcpServerTests (1 hour)
**Directory:** `Examples/NotificationMcpServerTests`

Tests:
- Subscribe to notifications
- Receive tools/changed notification
- Receive prompts/changed notification
- Receive resources/updated notification
- Multiple subscribers

---

## 🔧 Technical Design

### Notification Flow:

```
1. Client connects via WebSocket
2. Client sends 'initialize' → Server responds with notification capabilities
3. Client subscribes (implicit via WebSocket connection)
4. Server detects change (tool added/removed/resource modified)
5. Server sends notification to all WebSocket subscribers:
   {
     "jsonrpc": "2.0",
     "method": "notifications/tools/changed",
     "params": {}
   }
6. Client re-fetches tools/list
```

### Thread Safety:
- Use `ConcurrentBag<WebSocket>` for subscriber list
- Lock-free notification sending
- Graceful handling of closed connections

### Transport Support:
- **WebSocket:** Full push notifications ✅
- **SSE:** Push notifications possible ✅
- **HTTP:** Must poll (no push) ⚠️
- **stdio:** Must poll (no push) ⚠️

---

## 📝 Notes

- Keep it simple for v1.6.0 - just basic notifications
- No subscription filtering (subscribe to all or nothing)
- No resource-specific subscriptions yet (defer to v1.7+)
- Focus on WebSocket first, SSE can be added later

---

## ✅ Success Criteria

- [ ] NotificationService can manage WebSocket subscribers
- [ ] Server can send notifications to all subscribers
- [ ] NotificationMcpServer demonstrates hot-reload
- [ ] 10+ notification tests pass
- [ ] Backward compatible (no breaking changes)
- [ ] All existing 130 tests still pass

---

**Estimated Time:** 3-4 hours  
**Dependencies:** Phase 1 (Pagination) ✅ Complete
