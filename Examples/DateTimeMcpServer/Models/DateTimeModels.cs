namespace DateTimeMcpServer.Models;

using System.ComponentModel;
using System.Text.Json.Serialization;

public sealed record CurrentDateTimeRequest(
    [property: JsonPropertyName("timezoneName")]
    [property: Description("Timezone name (e.g., 'Europe/Oslo', 'UTC')")] string? TimezoneName);

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

public sealed record AddDaysRequest(
    [property: JsonPropertyName("date")]
    [property: Description("Starting date in YYYY-MM-DD format (defaults to today)")] string? Date,
    [property: JsonPropertyName("days")]
    [property: Description("Number of days to add (positive) or subtract (negative)")] int Days = 0);

public sealed record AddDaysResponse(
    [property: JsonPropertyName("original")] string Original,
    [property: JsonPropertyName("result")] string Result,
    [property: JsonPropertyName("daysAdded")] int DaysAdded,
    [property: JsonPropertyName("dayOfWeek")] string DayOfWeek);

public sealed record IsWeekendRequest(
    [property: JsonPropertyName("date")]
    [property: Description("Date in YYYY-MM-DD format (defaults to today")]  string? Date);

public sealed record IsWeekendResponse(
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("dayOfWeek")] string DayOfWeek,
    [property: JsonPropertyName("isWeekend")] bool IsWeekend);

public sealed record GetWeeknumberRequest(
    [property: JsonPropertyName("date")]
    [property: Description("Starting date in YYYY-MM-DD format (defaults to today)")]  string? Date);

public sealed record GetWeekNumberResponse(
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("weekNumber")] int WeekNumber,
    [property: JsonPropertyName("year")] int Year);