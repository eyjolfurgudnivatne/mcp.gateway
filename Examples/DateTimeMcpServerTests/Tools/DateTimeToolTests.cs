namespace DateTimeMcpServerTests.Tools;

using DateTimeMcpServer.Models;
using DateTimeMcpServerTests.Fixture;
using Mcp.Gateway.Tools;
using System;
using System.Net.Http.Json;
using System.Globalization;

[Collection("ServerCollection")]
public class DateTimeToolTests(DateTimeMcpServerFixture fixture)
{
    [Fact]
    public async Task GetCurrentDateTime_ReturnsCurrentDateTime()
    {
        // Arrange
        var request = JsonRpcMessage.CreateRequest(
            "get_current_datetime",
            Guid.NewGuid().ToString("D"),
            new CurrentDateTimeRequest(null));

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(fixture.CancellationToken);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, $"Failed to generate secret");

        var result = content.GetResult<CurrentDateTimeResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.DateTime);

        // Basic validation of returned date-time format
        bool isValidFormat = DateTime.TryParse(result.DateTime, out DateTime parsedDateTime);
        Assert.True(isValidFormat, "The returned DateTime is not in a valid format.");

        Assert.Equal(parsedDateTime.Year, result.Year);
        Assert.Equal(parsedDateTime.Month, result.Month);
        Assert.Equal(parsedDateTime.Day, result.Day);
    }

    [Fact]
    public async Task GetIso8601Timestamp_ReturnsUtcIso8601Timestamp()
    {
        // Arrange
        var request = JsonRpcMessage.CreateRequest(
            "get_iso8601_timestamp",
            Guid.NewGuid().ToString("D"),
            new { });

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(fixture.CancellationToken);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, "Failed to get ISO8601 timestamp");

        var result = content.GetResult<Iso8601TimestampResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Timestamp));

        // Validate ISO 8601 and UTC
        bool parsed = DateTime.TryParse(result.Timestamp, null, DateTimeStyles.RoundtripKind, out DateTime parsedTimestamp);
        Assert.True(parsed, "Timestamp is not a valid ISO 8601 string");
        Assert.Equal(DateTimeKind.Utc, parsedTimestamp.Kind);
    }

    [Fact]
    public async Task AddDays_WithDateAndDays_ReturnsAdjustedDate()
    {
        // Arrange
        var baseDate = "2025-12-11";
        var daysToAdd = 5;
        var request = JsonRpcMessage.CreateRequest(
            "add_days",
            Guid.NewGuid().ToString("D"),
            new AddDaysRequest(baseDate, daysToAdd));

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(fixture.CancellationToken);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, "Failed to add days");

        var result = content.GetResult<AddDaysResponse>();
        Assert.NotNull(result);
        Assert.Equal(baseDate, result.Original);

        bool parsed = DateTime.TryParse(result.Result, out DateTime resultDate);
        Assert.True(parsed, "Result date is not a valid date string");

        var expected = DateTime.Parse(baseDate).AddDays(daysToAdd);
        Assert.Equal(expected.Year, resultDate.Year);
        Assert.Equal(expected.Month, resultDate.Month);
        Assert.Equal(expected.Day, resultDate.Day);
        Assert.Equal(daysToAdd, result.DaysAdded);
    }

    [Fact]
    public async Task IsWeekend_WithSaturday_ReturnsIsWeekendTrue()
    {
        // Arrange - 2025-12-13 is a Saturday
        var date = "2025-12-13";
        var request = JsonRpcMessage.CreateRequest(
            "is_weekend",
            Guid.NewGuid().ToString("D"),
            new IsWeekendRequest(date));

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(fixture.CancellationToken);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, "Failed to check weekend");

        var result = content.GetResult<IsWeekendResponse>();
        Assert.NotNull(result);

        bool parsed = DateTime.TryParse(result.Date, out DateTime parsedDate);
        Assert.True(parsed, "Returned date is not valid");

        bool expectedIsWeekend = parsedDate.DayOfWeek == DayOfWeek.Saturday || parsedDate.DayOfWeek == DayOfWeek.Sunday;
        Assert.Equal(expectedIsWeekend, result.IsWeekend);
    }

    [Fact]
    public async Task GetWeeknumber_WithDate_ReturnsCorrectWeekNumberAndYear()
    {
        // Arrange
        var date = "2025-12-11";
        var request = JsonRpcMessage.CreateRequest(
            "get_weeknumber",
            Guid.NewGuid().ToString("D"),
            new GetWeeknumberRequest(date));

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(fixture.CancellationToken);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, "Failed to get week number");

        var result = content.GetResult<GetWeekNumberResponse>();
        Assert.NotNull(result);

        bool parsed = DateTime.TryParse(result.Date, out DateTime parsedDate);
        Assert.True(parsed, "Returned date is not valid");

        int expectedWeek = ISOWeek.GetWeekOfYear(parsedDate);
        Assert.Equal(expectedWeek, result.WeekNumber);
        Assert.Equal(parsedDate.Year, result.Year);
    }
}
