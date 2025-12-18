namespace Mcp.Gateway.Tests.Unit;

using Mcp.Gateway.Tools;
using Xunit;

/// <summary>
/// Unit tests for SseStreamRegistry (v1.7.0 Phase 2)
/// Tests SSE stream management and broadcasting
/// Note: BroadcastAsync is tested via integration tests due to HttpResponse dependencies
/// </summary>
public class SseStreamRegistryTests
{
    [Fact]
    public void Register_WithValidStream_IncreasesCount()
    {
        // Arrange
        var registry = new SseStreamRegistry();
        var (response, _) = CreateTestResponse();
        var ct = CancellationToken.None;

        // Act
        registry.Register("session-1", response, ct);

        // Assert
        Assert.Equal(1, registry.GetStreamCount("session-1"));
        Assert.Equal(1, registry.ActiveSessionCount);
    }

    [Fact]
    public void Register_MultipleStreamsForSameSession_IncreasesCount()
    {
        // Arrange
        var registry = new SseStreamRegistry();
        var (response1, _) = CreateTestResponse();
        var (response2, _) = CreateTestResponse();
        var ct = CancellationToken.None;

        // Act
        registry.Register("session-1", response1, ct);
        registry.Register("session-1", response2, ct);

        // Assert
        Assert.Equal(2, registry.GetStreamCount("session-1"));
        Assert.Equal(1, registry.ActiveSessionCount);
    }

    [Fact]
    public void Register_DifferentSessions_IncreasesSessionCount()
    {
        // Arrange
        var registry = new SseStreamRegistry();
        var (response1, _) = CreateTestResponse();
        var (response2, _) = CreateTestResponse();
        var ct = CancellationToken.None;

        // Act
        registry.Register("session-1", response1, ct);
        registry.Register("session-2", response2, ct);

        // Assert
        Assert.Equal(1, registry.GetStreamCount("session-1"));
        Assert.Equal(1, registry.GetStreamCount("session-2"));
        Assert.Equal(2, registry.ActiveSessionCount);
    }

    [Fact]
    public void Register_WithNullSessionId_ThrowsException()
    {
        // Arrange
        var registry = new SseStreamRegistry();
        var (response, _) = CreateTestResponse();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            registry.Register(null!, response, CancellationToken.None));
    }

    [Fact]
    public void Register_WithEmptySessionId_ThrowsException()
    {
        // Arrange
        var registry = new SseStreamRegistry();
        var (response, _) = CreateTestResponse();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            registry.Register("", response, CancellationToken.None));
    }

    [Fact]
    public void Register_WithNullResponse_ThrowsException()
    {
        // Arrange
        var registry = new SseStreamRegistry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            registry.Register("session-1", null!, CancellationToken.None));
    }

    [Fact]
    public void Unregister_RemovesStream()
    {
        // Arrange
        var registry = new SseStreamRegistry();
        var (response, _) = CreateTestResponse();
        registry.Register("session-1", response, CancellationToken.None);

        // Act
        registry.Unregister("session-1", response);

        // Assert
        Assert.Equal(0, registry.GetStreamCount("session-1"));
        Assert.Equal(0, registry.ActiveSessionCount);
    }

    [Fact]
    public void Unregister_WithMultipleStreams_RemovesOnlySpecified()
    {
        // Arrange
        var registry = new SseStreamRegistry();
        var (response1, _) = CreateTestResponse();
        var (response2, _) = CreateTestResponse();
        registry.Register("session-1", response1, CancellationToken.None);
        registry.Register("session-1", response2, CancellationToken.None);

        // Act
        registry.Unregister("session-1", response1);

        // Assert
        Assert.Equal(1, registry.GetStreamCount("session-1"));
    }

    [Fact]
    public void Unregister_WithNullSessionId_DoesNotThrow()
    {
        // Arrange
        var registry = new SseStreamRegistry();
        var (response, _) = CreateTestResponse();

        // Act & Assert - Should not throw
        registry.Unregister(null!, response);
    }

    [Fact]
    public void Unregister_WithNullResponse_DoesNotThrow()
    {
        // Arrange
        var registry = new SseStreamRegistry();

        // Act & Assert - Should not throw
        registry.Unregister("session-1", null!);
    }

    [Fact]
    public async Task BroadcastAsync_WithNullSessionId_DoesNotThrow()
    {
        // Arrange
        var registry = new SseStreamRegistry();
        var message = SseEventMessage.CreateMessage("event-1", new { test = "data" });

        // Act & Assert - Should not throw
        await registry.BroadcastAsync(null!, message);
    }

    [Fact]
    public async Task BroadcastAsync_WithNullMessage_DoesNotThrow()
    {
        // Arrange
        var registry = new SseStreamRegistry();

        // Act & Assert - Should not throw
        await registry.BroadcastAsync("session-1", null!);
    }

    [Fact]
    public async Task BroadcastAsync_WithNonExistentSession_DoesNotThrow()
    {
        // Arrange
        var registry = new SseStreamRegistry();
        var message = SseEventMessage.CreateMessage("event-1", new { test = "data" });

        // Act & Assert - Should not throw
        await registry.BroadcastAsync("non-existent", message);
    }

    [Fact]
    public void GetStreamCount_WithNullSessionId_ReturnsZero()
    {
        // Arrange
        var registry = new SseStreamRegistry();

        // Act
        var count = registry.GetStreamCount(null!);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void GetStreamCount_WithNonExistentSession_ReturnsZero()
    {
        // Arrange
        var registry = new SseStreamRegistry();

        // Act
        var count = registry.GetStreamCount("non-existent");

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void CleanupSession_RemovesAllStreams()
    {
        // Arrange
        var registry = new SseStreamRegistry();
        var (response1, _) = CreateTestResponse();
        var (response2, _) = CreateTestResponse();
        registry.Register("session-1", response1, CancellationToken.None);
        registry.Register("session-1", response2, CancellationToken.None);

        // Act
        registry.CleanupSession("session-1");

        // Assert
        Assert.Equal(0, registry.GetStreamCount("session-1"));
        Assert.Equal(0, registry.ActiveSessionCount);
    }

    [Fact]
    public void CleanupSession_WithNullSessionId_DoesNotThrow()
    {
        // Arrange
        var registry = new SseStreamRegistry();

        // Act & Assert - Should not throw
        registry.CleanupSession(null!);
    }

    [Fact]
    public void ActiveSessionCount_ReflectsActiveSessions()
    {
        // Arrange
        var registry = new SseStreamRegistry();
        var (response1, _) = CreateTestResponse();
        var (response2, _) = CreateTestResponse();
        var (response3, _) = CreateTestResponse();

        // Act
        registry.Register("session-1", response1, CancellationToken.None);
        registry.Register("session-2", response2, CancellationToken.None);
        registry.Register("session-2", response3, CancellationToken.None);

        // Assert
        Assert.Equal(2, registry.ActiveSessionCount);
    }

    [Fact]
    public void ActiveSseStream_ContainsCorrectData()
    {
        // Arrange
        var (response, _) = CreateTestResponse();
        var ct = new CancellationToken();

        // Act
        var stream = new ActiveSseStream(response, ct);

        // Assert
        Assert.Equal(response, stream.Response);
        Assert.Equal(ct, stream.CancellationToken);
    }

    // Helper method to create test HttpResponse (using DefaultHttpContext)
    private (Microsoft.AspNetCore.Http.HttpResponse, MemoryStream) CreateTestResponse()
    {
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var stream = new MemoryStream();
        context.Response.Body = stream;
        return (context.Response, stream);
    }
}
