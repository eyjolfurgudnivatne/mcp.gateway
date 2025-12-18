namespace Mcp.Gateway.Tests.Unit;

using Mcp.Gateway.Tools;
using Xunit;

/// <summary>
/// Unit tests for EventIdGenerator (v1.7.0)
/// </summary>
public class EventIdGeneratorTests
{
    [Fact]
    public void GenerateEventId_WithoutSession_ReturnsGlobalId()
    {
        // Arrange
        var generator = new EventIdGenerator();

        // Act
        var id1 = generator.GenerateEventId();
        var id2 = generator.GenerateEventId();

        // Assert
        Assert.Equal("1", id1);
        Assert.Equal("2", id2);
    }

    [Fact]
    public void GenerateEventId_WithSession_ReturnsSessionScopedId()
    {
        // Arrange
        var generator = new EventIdGenerator();
        var sessionId = "abc123";

        // Act
        var id1 = generator.GenerateEventId(sessionId);
        var id2 = generator.GenerateEventId(sessionId);

        // Assert
        Assert.Equal("abc123-1", id1);
        Assert.Equal("abc123-2", id2);
    }

    [Fact]
    public void GenerateEventId_MultipleCallsInParallel_AreThreadSafe()
    {
        // Arrange
        var generator = new EventIdGenerator();
        var ids = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act - Generate 1000 IDs in parallel
        Parallel.For(0, 1000, _ =>
        {
            var id = generator.GenerateEventId();
            ids.Add(id);
        });

        // Assert - All IDs should be unique
        var distinctIds = ids.Distinct().ToList();
        Assert.Equal(1000, distinctIds.Count);
    }

    [Fact]
    public void GenerateEventId_Increments_Atomically()
    {
        // Arrange
        var generator = new EventIdGenerator();

        // Act
        var id1 = generator.GenerateEventId();
        var id2 = generator.GenerateEventId();
        var id3 = generator.GenerateEventId();

        // Assert
        Assert.Equal("1", id1);
        Assert.Equal("2", id2);
        Assert.Equal("3", id3);
    }

    [Fact]
    public void GenerateEventId_WithNullSession_ReturnsGlobalId()
    {
        // Arrange
        var generator = new EventIdGenerator();

        // Act
        var id = generator.GenerateEventId(sessionId: null);

        // Assert
        Assert.Equal("1", id);
    }

    [Fact]
    public void GenerateEventId_WithEmptySession_ReturnsGlobalId()
    {
        // Arrange
        var generator = new EventIdGenerator();

        // Act
        var id = generator.GenerateEventId(sessionId: "");

        // Assert
        Assert.Equal("1", id);
    }

    [Fact]
    public void GenerateEventId_MixedSessionAndGlobal_MaintainsSeparateCounters()
    {
        // Arrange
        var generator = new EventIdGenerator();

        // Act
        var globalId1 = generator.GenerateEventId();
        var sessionId1 = generator.GenerateEventId("session1");
        var globalId2 = generator.GenerateEventId();
        var sessionId2 = generator.GenerateEventId("session1");

        // Assert
        Assert.Equal("1", globalId1);
        Assert.Equal("session1-2", sessionId1);  // Uses global counter
        Assert.Equal("3", globalId2);
        Assert.Equal("session1-4", sessionId2);  // Uses global counter
    }

    [Fact]
    public void GenerateEventId_WithMultipleSessions_SharesGlobalCounter()
    {
        // Arrange
        var generator = new EventIdGenerator();

        // Act
        var session1Id1 = generator.GenerateEventId("session1");
        var session2Id1 = generator.GenerateEventId("session2");
        var session1Id2 = generator.GenerateEventId("session1");

        // Assert - All use the same global counter
        Assert.Equal("session1-1", session1Id1);
        Assert.Equal("session2-2", session2Id1);
        Assert.Equal("session1-3", session1Id2);
    }
}
