---
layout: mcp-default
title: DateTime Server Example
description: Build a DateTime utility MCP server with timezone support
breadcrumbs:
  - title: Home
    url: /
  - title: Examples
    url: /examples/datetime/
  - title: DateTime Server
    url: /examples/datetime/
toc: true
---

# DateTime Server Example

A complete MCP server for date and time operations with timezone support.

## Overview

The DateTime server demonstrates:
- ✅ **Current date/time** - Multiple timezones
- ✅ **ISO 8601 timestamps** - Standard format
- ✅ **Date arithmetic** - Add/subtract days
- ✅ **Weekend checking** - Saturday/Sunday detection
- ✅ **Week numbers** - ISO 8601 week of year
- ✅ **Timezone handling** - TimeZoneInfo integration
- ✅ **TypedJsonRpc<T>** - Type-safe parameter handling
- ✅ **Auto-generated tool names** - From method names

## Complete Code

### Program.cs

```csharp
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);

// Register MCP Gateway
builder.AddToolsService();

var app = builder.Build();

// stdio mode for GitHub Copilot
if (args.Contains("--stdio"))
{
    await ToolInvoker.RunStdioModeAsync(app.Services);
    return;
}

// HTTP mode
app.UseWebSockets();
app.UseProtocolVersionValidation();
app.MapStreamableHttpEndpoint("/mcp");

app.Run();
```

### DateTimeTools.cs

```csharp
using Mcp.Gateway.Tools;
using DateTimeMcpServer.Models;

namespace DateTimeMcpServer.Tools;

public class DateTimeTools
{
    [McpTool(Title = "Get current date and time",
             Description = "Get current date and time in specified timezone (default: local).")]
    public JsonRpcMessage GetCurrentDatetime(TypedJsonRpc<CurrentDateTimeRequest> message)
    {
        var request = message.GetParams();
        TimeZoneInfo tz;

        try
        {
            tz = string.IsNullOrWhiteSpace(request?.TimezoneName)
                ? TimeZoneInfo.Local
                : TimeZoneInfo.FindSystemTimeZoneById(request.TimezoneName);
        }
        catch
        {
            // Fallback to local timezone if invalid
            tz = TimeZoneInfo.Local;
        }

        var now = TimeZoneInfo.ConvertTime(DateTime.Now, tz);

        return ToolResponse.Success(
            message.Id,
            new CurrentDateTimeResponse(
                DateTime: now.ToString("o"),  // ISO 8601 format
                Date: now.ToString("yyyy-MM-dd"),
                Time: now.ToString("HH:mm:ss"),
                TimeZone: tz.Id,
                DayOfWeek: now.ToString("dddd"),
                WeekNumber: System.Globalization.ISOWeek.GetWeekOfYear(now),
                Year: now.Year,
                Month: now.Month,
                Day: now.Day
            ));
    }

    [McpTool("get_iso8601_timestamp",
             Title = "Get current timestamp",
             Description = "Get current timestamp in ISO 8601 format (UTC)")]
    public JsonRpcMessage GetIso8601Timestamp(JsonRpcMessage message)
    {
        return ToolResponse.Success(
            message.Id,
            new Iso8601TimestampResponse(
                Timestamp: DateTime.UtcNow.ToString("o")
            ));
    }

    [McpTool(Title = "Add or subtract days",
             Description = "Add or subtract days from a date")]
    public JsonRpcMessage AddDays(TypedJsonRpc<AddDaysRequest> message)
    {
        var request = message.GetParams();

        DateTime baseDate = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(request?.Date))
        {
            DateTime.TryParse(request.Date, out baseDate);
        }

        var result = baseDate.AddDays(request?.Days ?? 0);

        return ToolResponse.Success(
            message.Id,
            new AddDaysResponse(
                Original: baseDate.ToString("yyyy-MM-dd"),
                Result: result.ToString("yyyy-MM-dd"),
                DaysAdded: request?.Days ?? 0,
                DayOfWeek: result.ToString("dddd")
            ));
    }

    [McpTool(Title = "Check if weekend",
             Description = "Check if a date is a weekend (Saturday or Sunday)")]
    public JsonRpcMessage IsWeekend(TypedJsonRpc<IsWeekendRequest> message)
    {
        var request = message.GetParams();

        DateTime baseDate = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(request?.Date))
        {
            DateTime.TryParse(request.Date, out baseDate);
        }

        var isWeekend = baseDate.DayOfWeek == DayOfWeek.Saturday || 
                        baseDate.DayOfWeek == DayOfWeek.Sunday;

        return ToolResponse.Success(
            message.Id,
            new IsWeekendResponse(
                Date: baseDate.ToString("yyyy-MM-dd"),
                DayOfWeek: baseDate.ToString("dddd"),
                IsWeekend: isWeekend
            ));
    }

    [McpTool(Title = "Get week number",
             Description = "Get ISO 8601 week number for a date")]
    public JsonRpcMessage GetWeeknumber(TypedJsonRpc<GetWeeknumberRequest> message)
    {
        var request = message.GetParams();

        DateTime baseDate = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(request?.Date))
        {
            DateTime.TryParse(request.Date, out baseDate);
        }

        var weekNumber = System.Globalization.ISOWeek.GetWeekOfYear(baseDate);

        return ToolResponse.Success(
            message.Id,
            new GetWeekNumberResponse(
                Date: baseDate.ToString("yyyy-MM-dd"),
                WeekNumber: weekNumber,
                Year: baseDate.Year
            ));
    }
}
```

### Models (DateTimeModels.cs)

```csharp
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DateTimeMcpServer.Models;

// Requests
public sealed record CurrentDateTimeRequest(
    [property: JsonPropertyName("timezoneName")]
    [property: Description("Timezone name (e.g., 'Europe/Oslo', 'UTC')")] 
    string? TimezoneName);

public sealed record AddDaysRequest(
    [property: JsonPropertyName("date")]
    [property: Description("Starting date in YYYY-MM-DD format (defaults to today)")] 
    string? Date,
    [property: JsonPropertyName("days")]
    [property: Description("Number of days to add (positive) or subtract (negative)")] 
    int Days = 0);

public sealed record IsWeekendRequest(
    [property: JsonPropertyName("date")]
    [property: Description("Date in YYYY-MM-DD format (defaults to today)")] 
    string? Date);

public sealed record GetWeeknumberRequest(
    [property: JsonPropertyName("date")]
    [property: Description("Starting date in YYYY-MM-DD format (defaults to today)")] 
    string? Date);

// Responses
public sealed record CurrentDateTimeResponse(
    [property: JsonPropertyName("datetime")] string DateTime,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("time")] string Time,
    [property: JsonPropertyName("timezone")] string TimeZone,
    [property: JsonPropertyName("dayOfWeek")] string DayOfWeek,
    [property: JsonPropertyName("weekNumber")] int WeekNumber,
    [property: JsonPropertyName("year")] int Year,
    [property: JsonPropertyName("month")] int Month,
    [property: JsonPropertyName("day")] int Day);

public sealed record Iso8601TimestampResponse(
    [property: JsonPropertyName("timestamp")] string Timestamp);

public sealed record AddDaysResponse(
    [property: JsonPropertyName("original")] string Original,
    [property: JsonPropertyName("result")] string Result,
    [property: JsonPropertyName("daysAdded")] int DaysAdded,
    [property: JsonPropertyName("dayOfWeek")] string DayOfWeek);

public sealed record IsWeekendResponse(
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("dayOfWeek")] string DayOfWeek,
    [property: JsonPropertyName("isWeekend")] bool IsWeekend);

public sealed record GetWeekNumberResponse(
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("weekNumber")] int WeekNumber,
    [property: JsonPropertyName("year")] int Year);
```

## Running the Server

### HTTP Mode

```bash
dotnet run
```

Server runs at: `http://localhost:5000/mcp`

### stdio Mode (GitHub Copilot)

```bash
dotnet run -- --stdio
```

## Testing

### Get Current DateTime

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "get_current_datetime",
      "arguments": {
        "timezoneName": "Europe/Oslo"
      }
    },
    "id": 1
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"datetime\":\"2025-12-20T13:45:30+01:00\",\"date\":\"2025-12-20\",\"time\":\"13:45:30\",\"timezone\":\"Europe/Oslo\",\"dayOfWeek\":\"Friday\",\"weekNumber\":51,\"year\":2025,\"month\":12,\"day\":20}"
      }
    ]
  },
  "id": 1
}
```

### Add Days

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "add_days",
      "arguments": {
        "date": "2025-12-20",
        "days": 7
      }
    },
    "id": 2
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"original\":\"2025-12-20\",\"result\":\"2025-12-27\",\"daysAdded\":7,\"dayOfWeek\":\"Friday\"}"
      }
    ]
  },
  "id": 2
}
```

### Check Weekend

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "is_weekend",
      "arguments": {
        "date": "2025-12-21"
      }
    },
    "id": 3
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"date\":\"2025-12-21\",\"dayOfWeek\":\"Sunday\",\"isWeekend\":true}"
      }
    ]
  },
  "id": 3
}
```

## Key Concepts

### 1. TypedJsonRpc<T>

Type-safe parameter handling with automatic deserialization:

```csharp
public JsonRpcMessage AddDays(TypedJsonRpc<AddDaysRequest> message)
{
    var request = message.GetParams();  // Returns AddDaysRequest
    // request.Date and request.Days are strongly typed!
}
```

**Benefits:**
- ✅ Compile-time type safety
- ✅ Automatic JSON deserialization
- ✅ IntelliSense support
- ✅ Cleaner code

### 2. Description Attributes

Use `[Description]` on record properties for documentation:

```csharp
public sealed record AddDaysRequest(
    [property: Description("Starting date in YYYY-MM-DD format")] 
    string? Date,
    [property: Description("Number of days to add")] 
    int Days = 0);
```

**Generates InputSchema automatically!**

### 3. JsonPropertyName

Control JSON serialization names:

```csharp
public sealed record CurrentDateTimeResponse(
    [property: JsonPropertyName("datetime")] string DateTime,
    [property: JsonPropertyName("timezone")] string TimeZone);
```

**JSON output:**
```json
{
  "datetime": "2025-12-20T13:45:30+01:00",
  "timezone": "Europe/Oslo"
}
```

### 4. Auto-Generated Tool Names

Method names automatically convert to snake_case:

```csharp
[McpTool]  // No explicit name!
public JsonRpcMessage GetCurrentDatetime(...) { }
// Auto-generates: "get_current_datetime"

[McpTool]
public JsonRpcMessage AddDays(...) { }
// Auto-generates: "add_days"
```

### 5. Sealed Records

Use `sealed record` for immutable data models:

```csharp
public sealed record AddDaysRequest(
    string? Date,
    int Days = 0);  // Default value
```

**Benefits:**
- ✅ Immutable
- ✅ Value semantics
- ✅ Built-in equality
- ✅ Sealed for performance

### 6. Timezone Handling

```csharp
TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");
var now = TimeZoneInfo.ConvertTime(DateTime.Now, tz);
```

**Common timezones:**
- `UTC` - Coordinated Universal Time
- `Europe/Oslo` - Norway
- `America/New_York` - US Eastern
- `Asia/Tokyo` - Japan
- `Australia/Sydney` - Australia

### 7. ISO 8601 Format

```csharp
DateTime.UtcNow.ToString("o")  // "2025-12-20T13:45:30.1234567Z"
```

### 8. ISO Week Numbers

```csharp
System.Globalization.ISOWeek.GetWeekOfYear(date)
```

ISO 8601 week numbers:
- Week starts on Monday
- Week 1 contains January 4th
- Returns 1-53

## Using with GitHub Copilot

Configure `.mcp.json`:

```json
{
  "mcpServers": {
    "datetime": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\path\\to\\DateTimeMcpServer",
        "--",
        "--stdio"
      ]
    }
  }
}
```

Then in Copilot Chat:

```
@datetime what's the current time in Tokyo?
@datetime add 30 days to 2025-12-20
@datetime is 2025-12-25 a weekend?
@datetime what week number is today?
```

## Enhancements

### 1. Date Formatting

```csharp
[McpTool("format_date")]
public JsonRpcMessage FormatDate(TypedJsonRpc<FormatDateRequest> request)
{
    var args = request.GetParams()!;
    var date = DateTime.Parse(args.Date);
    
    return ToolResponse.Success(request.Id, new
    {
        shortDate = date.ToString("d"),      // 12/20/2025
        longDate = date.ToString("D"),       // Friday, December 20, 2025
        fullDateTime = date.ToString("F"),   // Friday, December 20, 2025 1:45 PM
        custom = date.ToString(args.Format ?? "yyyy-MM-dd")
    });
}

public sealed record FormatDateRequest(
    [property: Description("Date to format")] string Date,
    [property: Description("Custom format string")] string? Format = null);
```

### 2. Business Days Calculator

```csharp
[McpTool("add_business_days")]
public JsonRpcMessage AddBusinessDays(TypedJsonRpc<BusinessDaysRequest> request)
{
    var args = request.GetParams()!;
    var date = DateTime.Parse(args.Date);
    var daysToAdd = args.Days;
    
    while (daysToAdd > 0)
    {
        date = date.AddDays(1);
        if (date.DayOfWeek != DayOfWeek.Saturday && 
            date.DayOfWeek != DayOfWeek.Sunday)
        {
            daysToAdd--;
        }
    }
    
    return ToolResponse.Success(request.Id, new
    {
        resultDate = date.ToString("yyyy-MM-dd"),
        dayOfWeek = date.ToString("dddd")
    });
}

public sealed record BusinessDaysRequest(
    [property: Description("Starting date")] string Date,
    [property: Description("Business days to add")] int Days);
```

### 3. Time Until/Since

```csharp
[McpTool("time_until")]
public JsonRpcMessage TimeUntil(TypedJsonRpc<TimeUntilRequest> request)
{
    var args = request.GetParams()!;
    var targetDate = DateTime.Parse(args.Date);
    var now = DateTime.Now;
    
    var timeSpan = targetDate - now;
    
    return ToolResponse.Success(request.Id, new
    {
        days = timeSpan.Days,
        hours = timeSpan.Hours,
        minutes = timeSpan.Minutes,
        totalDays = timeSpan.TotalDays,
        isPast = timeSpan < TimeSpan.Zero
    });
}

public sealed record TimeUntilRequest(
    [property: Description("Target date")] string Date);
```

## Integration Tests

```csharp
[Fact]
public async Task GetCurrentDateTime_WithTimezone_ReturnsFormattedDate()
{
    // Arrange
    using var server = new McpGatewayFixture();
    var client = server.CreateClient();
    
    var request = new
    {
        jsonrpc = "2.0",
        method = "tools/call",
        @params = new
        {
            name = "get_current_datetime",
            arguments = new { timezoneName = "UTC" }
        },
        id = 1
    };
    
    // Act
    var response = await client.PostAsJsonAsync("/mcp", request);
    var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
    
    // Assert
    Assert.NotNull(result);
    var content = result.RootElement
        .GetProperty("result")
        .GetProperty("content")[0]
        .GetProperty("text")
        .GetString();
    
    Assert.Contains("datetime", content);
    Assert.Contains("UTC", content);
}

[Fact]
public async Task IsWeekend_Sunday_ReturnsTrue()
{
    // Arrange
    var request = new
    {
        jsonrpc = "2.0",
        method = "tools/call",
        @params = new
        {
            name = "is_weekend",
            arguments = new { date = "2025-12-21" }  // Sunday
        },
        id = 2
    };
    
    // Act
    var response = await client.PostAsJsonAsync("/mcp", request);
    var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
    
    // Assert
    var content = JsonSerializer.Deserialize<IsWeekendResponse>(
        result.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString()!);
    
    Assert.True(content.IsWeekend);
}
```

## Source Code

Full source code available at:
- **GitHub:** [Examples/DateTimeMcpServer](https://github.com/eyjolfurgudnivatne/mcp.gateway/tree/main/Examples/DateTimeMcpServer)
- **Tests:** [Examples/DateTimeMcpServerTests](https://github.com/eyjolfurgudnivatne/mcp.gateway/tree/main/Examples/DateTimeMcpServerTests)

## See Also

- [Getting Started](/mcp.gateway/getting-started/index/) - Build your first tool
- [Tools API](/mcp.gateway/api/tools/) - Complete Tools API reference
- [Calculator Example](/mcp.gateway/examples/calculator/) - Basic arithmetic operations
- [Metrics Example](/mcp.gateway/examples/metrics/) - Add metrics tracking
