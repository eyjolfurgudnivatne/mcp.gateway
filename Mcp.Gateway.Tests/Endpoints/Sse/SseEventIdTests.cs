namespace Mcp.Gateway.Tests.Endpoints.Sse;

using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

/// <summary>
/// Integration tests for SSE Event IDs (v1.7.0)
/// </summary>
[Collection("ServerCollection")]
public class SseEventIdTests(McpGatewayFixture fixture)
{
    [Fact]
    public async Task SseResponse_IncludesEventIds()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "system_ping",
            id = "ping-1"
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/sse", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        // Read SSE stream
        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);

        // Assert - Should contain event IDs
        Assert.Contains("id: ", content);
        
        // Parse SSE events
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var eventIdLines = lines.Where(l => l.StartsWith("id: ")).ToList();
        
        // Should have at least 2 event IDs (1 for response, 1 for done)
        Assert.True(eventIdLines.Count >= 2, $"Expected at least 2 event IDs, got {eventIdLines.Count}");
    }

    [Fact]
    public async Task SseResponse_EventIdsAreSequential()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "system_ping",
            id = "ping-sequential"
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/sse", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        // Read SSE stream
        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);

        // Parse event IDs
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var eventIds = lines
            .Where(l => l.StartsWith("id: "))
            .Select(l => l["id: ".Length..])
            .Select(int.Parse)
            .ToList();

        // Assert - IDs should be sequential
        Assert.True(eventIds.Count >= 2);
        
        for (int i = 1; i < eventIds.Count; i++)
        {
            Assert.True(eventIds[i] > eventIds[i - 1], 
                $"Event ID {eventIds[i]} should be greater than {eventIds[i - 1]}");
        }
    }

    [Fact]
    public async Task SseResponse_IncludesEventTypes()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "system_ping",
            id = "ping-event-types"
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/sse", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        // Read SSE stream
        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);

        // Assert - Should contain event types
        Assert.Contains("event: message", content);
        Assert.Contains("event: done", content);
    }

    [Fact]
    public async Task SseBatchRequest_EachResponseHasUniqueEventId()
    {
        // Arrange - batch request with 3 tools
        var batchRequest = new object[]
        {
            new { jsonrpc = "2.0", method = "system_ping", id = "batch-1" },
            new { jsonrpc = "2.0", method = "system_ping", id = "batch-2" },
            new { jsonrpc = "2.0", method = "system_ping", id = "batch-3" }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(batchRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        // Act
        var httpResponse = await fixture.HttpClient.PostAsync("/sse", content, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        // Read SSE stream
        var responseContent = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);

        // Parse event IDs
        var lines = responseContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var eventIds = lines
            .Where(l => l.StartsWith("id: "))
            .Select(l => l["id: ".Length..])
            .ToList();

        // Assert - Should have at least 4 event IDs (3 responses + 1 done)
        Assert.True(eventIds.Count >= 4, $"Expected at least 4 event IDs, got {eventIds.Count}");
        
        // All event IDs should be unique
        var uniqueIds = eventIds.Distinct().ToList();
        Assert.Equal(eventIds.Count, uniqueIds.Count);
    }

    [Fact]
    public async Task SseResponse_DoneEventHasEventId()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "system_ping",
            id = "ping-done"
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/sse", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        // Read SSE stream
        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);

        // Parse SSE events
        var events = ParseSseEvents(content);

        // Assert - Find the "done" event
        var doneEvent = events.FirstOrDefault(e => e.EventType == "done");
        Assert.NotNull(doneEvent);
        Assert.False(string.IsNullOrEmpty(doneEvent.Id), "Done event should have an event ID");
    }

    [Fact]
    public async Task SseError_IncludesEventId()
    {
        // Arrange - Invalid JSON to trigger error
        var invalidJson = "{ this is not valid json }";
        var content = new StringContent(invalidJson, System.Text.Encoding.UTF8, "application/json");

        // Act
        var httpResponse = await fixture.HttpClient.PostAsync("/sse", content, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        // Read SSE stream
        var responseContent = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);

        // Assert - Error event should have an event ID
        Assert.Contains("id: ", responseContent);
        
        var events = ParseSseEvents(responseContent);
        var errorEvent = events.FirstOrDefault(e => e.EventType == "error");
        
        Assert.NotNull(errorEvent);
        Assert.False(string.IsNullOrEmpty(errorEvent.Id));
    }

    // Helper record for parsing SSE events
    private record SseEvent(string? Id, string? EventType, string? Data);

    // Helper method to parse SSE events
    private static List<SseEvent> ParseSseEvents(string sseContent)
    {
        var events = new List<SseEvent>();
        var lines = sseContent.Split('\n');

        string? currentId = null;
        string? currentEventType = null;
        string? currentData = null;

        foreach (var line in lines)
        {
            if (line.StartsWith("id: "))
            {
                currentId = line["id: ".Length..];
            }
            else if (line.StartsWith("event: "))
            {
                currentEventType = line["event: ".Length..];
            }
            else if (line.StartsWith("data: "))
            {
                currentData = line["data: ".Length..];
            }
            else if (string.IsNullOrEmpty(line.Trim()))
            {
                // End of event
                if (currentId != null || currentEventType != null || currentData != null)
                {
                    events.Add(new SseEvent(currentId, currentEventType, currentData));
                    currentId = null;
                    currentEventType = null;
                    currentData = null;
                }
            }
        }

        return events;
    }
}
