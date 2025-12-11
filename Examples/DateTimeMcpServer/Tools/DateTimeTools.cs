namespace DateTimeMcpServer.Tools;

using DateTimeMcpServer.Models;
using Mcp.Gateway.Tools;

public class DateTimeTools
{
    [McpTool("get_current_datetime",
        Title = "Get current date and time",
        Description = "Get current date and time in specified timezone (default: local).",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""timezoneName"":{
                    ""type"":""string"",
                    ""description"":""Timezone name (e.g., 'Europe/Oslo', 'UTC')"",
                    ""default"":""UTC""
                }
            }
        }")]
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
            // Fallback to local timezone if specified timezone is invalid
            tz = TimeZoneInfo.Local;
        }

        var now = TimeZoneInfo.ConvertTime(DateTime.Now, tz);

        return ToolResponse.Success(
            message.Id,
            new CurrentDateTimeResponse(
                now.ToString("o"),  // ISO 8601 format
                now.ToString("yyyy-MM-dd"),
                now.ToString("HH:mm:ss"),
                tz.Id,
                now.ToString("dddd"),  // Full day name (culture-sensitive)
                System.Globalization.ISOWeek.GetWeekOfYear(now),
                now.Year,
                now.Month,
                now.Day
            )
        );
    }

    [McpTool("get_iso8601_timestamp",
        Title = "Get current timestamp in ISO 8601 format",
        Description = "Get current timestamp in ISO 8601 format (UTC).",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{}
        }")]
    public JsonRpcMessage GetIso8601Timestamp(JsonRpcMessage message)
    {
        return ToolResponse.Success(
            message.Id,
            new Iso8601TimestampResponse(
                DateTime.UtcNow.ToString("o")
            )
        );
    }

    [McpTool(
        Title = "Add or subtract days from a date",
        Description = "Add or subtract days from a date.",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""date"":{
                    ""type"":""string"",
                    ""description"":""Starting date in YYYY-MM-DD format (defaults to today)""
                },
                ""days"":{
                    ""type"":""integer"",
                    ""description"":""Number of days to add (positive) or subtract (negative)""
                }
            }
        }")]
    public JsonRpcMessage AddDays(JsonRpcMessage message)
    {
        var request = message.GetParams<AddDaysRequest>();

        DateTime baseDate = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(request?.Date))
        {
            _ = DateTime.TryParse(request.Date, out baseDate);
        }

        var result = baseDate.AddDays(request?.Days ?? 0);

        return ToolResponse.Success(
            message.Id,
            new AddDaysResponse(
                baseDate.ToString("yyyy-MM-dd"),
                result.ToString("yyyy-MM-dd"),
                request?.Days ?? 0,
                result.ToString("dddd")  // Full day name (culture-sensitive)
            )
        );
    }

    [McpTool(
            Title = "Check if a date is a weekend",
            Description = "Check if a date is a weekend (Saturday or Sunday).",
            InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""date"":{
                    ""type"":""string"",
                    ""description"":""Date in YYYY-MM-DD format (defaults to today)""
                }
            }
        }")]
    public JsonRpcMessage IsWeekend(JsonRpcMessage message)
    {
        var request = message.GetParams<IsWeekendRequest>();

        DateTime baseDate = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(request?.Date))
        {
            _ = DateTime.TryParse(request.Date, out baseDate);
        }

        var isWeekend = baseDate.DayOfWeek == DayOfWeek.Saturday || baseDate.DayOfWeek == DayOfWeek.Sunday;

        return ToolResponse.Success(
            message.Id,
            new IsWeekendResponse(
                baseDate.ToString("yyyy-MM-dd"),
                baseDate.ToString("dddd"),
                isWeekend)
            );
    }

    [McpTool(
            Title = "Get week number for a date",
            Description = "Get ISO 8601 week number for a date.",
            InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""date"":{
                    ""type"":""string"",
                    ""description"":""Starting date in YYYY-MM-DD format (defaults to today)""
                }
            }
        }")]
    public JsonRpcMessage GetWeeknumber(JsonRpcMessage message)
    {
        var request = message.GetParams<GetWeeknumberRequest>();

        DateTime baseDate = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(request?.Date))
        {
            _ = DateTime.TryParse(request.Date, out baseDate);
        }

        var weekNumber = System.Globalization.ISOWeek.GetWeekOfYear(baseDate);

        return ToolResponse.Success(
            message.Id,
            new GetWeekNumberResponse(
                baseDate.ToString("yyyy-MM-dd"),
                weekNumber,
                baseDate.Year)
            );
    }
}
