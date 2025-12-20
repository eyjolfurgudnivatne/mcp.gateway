---
layout: mcp-default
title: DateTime Server Example
description: Build a DateTime utility MCP server with timezone support
breadcrumbs:
  - title: Home
    url: /
  - title: Examples
    url: /examples/
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
    [McpTool("get_current_datetime",
        Title = "Get current date and time",
        Description = "Get current date and time in specified timezone")]
    public JsonRpcMessage GetCurrentDateTime(JsonRpcMessage message)
    {
        var request = message.GetParams<CurrentDateTimeRequest>();
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
                Timestamp: now.ToString("o"),
                Date: now.ToString("yyyy-MM-dd"),
                Time: now.ToString("HH:mm:ss"),
                Timezone: tz.Id,
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

    [McpTool("add_days",
        Title = "Add or subtract days",
        Description = "Add or subtract days from a date")]
    public JsonRpcMessage AddDays(JsonRpcMessage message)
    {
        var request = message.GetParams<AddDaysRequest>();

        DateTime baseDate = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(request?.Date))
        {
            DateTime.TryParse(request.Date, out baseDate);
        }

        var result = baseDate.AddDays(request?.Days ?? 0);

        return ToolResponse.Success(
            message.Id,
            new AddDaysResponse(
                OriginalDate: baseDate.ToString("yyyy-MM-dd"),
                ResultDate: result.ToString("yyyy-MM-dd"),
                DaysAdded: request?.Days ?? 0,
                ResultDayOfWeek: result.ToString("dddd")
            ));
    }

    [McpTool("is_weekend",
        Title = "Check if weekend",
        Description = "Check if a date is a weekend (Saturday or Sunday)")]
    public JsonRpcMessage IsWeekend(JsonRpcMessage message)
    {
        var request = message.GetParams<IsWeekendRequest>();

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

    [McpTool("get_weeknumber",
        Title = "Get week number",
        Description = "Get ISO 8601 week number for a date")]
    public JsonRpcMessage GetWeeknumber(JsonRpcMessage message)
    {
        var request = message.GetParams<GetWeeknumberRequest>();

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

### Models

```csharp
namespace DateTimeMcpServer.Models;

// Requests
public record CurrentDateTimeRequest(string? TimezoneName);
public record AddDaysRequest(string? Date, int? Days);
public record IsWeekendRequest(string? Date);
public record GetWeeknumberRequest(string? Date);

// Responses
public record CurrentDateTimeResponse(
    string Timestamp,
    string Date,
    string Time,
    string Timezone,
    string DayOfWeek,
    int WeekNumber,
    int Year,
    int Month,
    int Day);

public record Iso8601TimestampResponse(string Timestamp);

public record AddDaysResponse(
    string OriginalDate,
    string ResultDate,
    int DaysAdded,
    string ResultDayOfWeek);

public record IsWeekendResponse(
    string Date,
    string DayOfWeek,
    bool IsWeekend);

public record GetWeekNumberResponse(
    string Date,
    int WeekNumber,
    int Year);
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
        "TimezoneName": "Europe/Oslo"
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
        "text": "{\"Timestamp\":\"2025-12-20T13:45:30+01:00\",\"Date\":\"2025-12-20\",\"Time\":\"13:45:30\",\"Timezone\":\"Europe/Oslo\",\"DayOfWeek\":\"Friday\",\"WeekNumber\":51,\"Year\":2025,\"Month\":12,\"Day\":20}"
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
        "Date": "2025-12-20",
        "Days": 7
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
        "text": "{\"OriginalDate\":\"2025-12-20\",\"ResultDate\":\"2025-12-27\",\"DaysAdded\":7,\"ResultDayOfWeek\":\"Friday\"}"
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
        "Date": "2025-12-21"
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
        "text": "{\"Date\":\"2025-12-21\",\"DayOfWeek\":\"Sunday\",\"IsWeekend\":true}"
      }
    ]
  },
  "id": 3
}
```

## Key Concepts

### 1. Timezone Handling

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

### 2. ISO 8601 Format

```csharp
DateTime.UtcNow.ToString("o")  // "2025-12-20T13:45:30.1234567Z"
```

### 3. ISO Week Numbers

```csharp
System.Globalization.ISOWeek.GetWeekOfYear(date)
```

ISO 8601 week numbers:
- Week starts on Monday
- Week 1 contains January 4th
- Returns 1-53

### 4. Date Arithmetic

```csharp
var future = baseDate.AddDays(7);    // +7 days
var past = baseDate.AddDays(-7);     // -7 days
var nextMonth = baseDate.AddMonths(1);  // +1 month
```

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
            arguments = new { TimezoneName = "UTC" }
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
    
    Assert.Contains("Timestamp", content);
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
            arguments = new { Date = "2025-12-21" }  // Sunday
        },
        id = 2
    };
    
    // Act
    var response = await client.PostAsJsonAsync("/mcp", request);
    var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
    
    // Assert
    var content = JSON.parse(result.RootElement
        .GetProperty("result")
        .GetProperty("content")[0]
        .GetProperty("text")
        .GetString());
    
    Assert.True(content.IsWeekend);
}
```

## Source Code

Full source code available at:
- **GitHub:** [Examples/DateTimeMcpServer](https://github.com/eyjolfurgudnivatne/mcp.gateway/tree/main/Examples/DateTimeMcpServer)
- **Tests:** [Examples/DateTimeMcpServerTests](https://github.com/eyjolfurgudnivatne/mcp.gateway/tree/main/Examples/DateTimeMcpServerTests)

## See Also

- [Getting Started](/getting-started/) - Build your first tool
- [Tools API](/api/tools/) - Complete Tools API reference
- [Examples: Calculator](/examples/calculator/) - Basic arithmetic operations
- [Examples: Metrics](/examples/metrics/) - Add metrics tracking
