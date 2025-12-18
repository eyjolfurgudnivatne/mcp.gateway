namespace Mcp.Gateway.Tests.Unit;

using Mcp.Gateway.Tools;
using Xunit;

/// <summary>
/// Unit tests for MessageBuffer (v1.7.0 Phase 2)
/// Tests message buffering and Last-Event-ID replay functionality
/// </summary>
public class MessageBufferTests
{
    [Fact]
    public void MessageBuffer_DefaultConstructor_SetsMaxSize100()
    {
        // Arrange & Act
        var buffer = new MessageBuffer();

        // Assert
        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public void MessageBuffer_WithCustomMaxSize_SetsCorrectly()
    {
        // Arrange & Act
        var buffer = new MessageBuffer(maxSize: 50);

        // Assert
        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public void MessageBuffer_WithZeroMaxSize_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new MessageBuffer(maxSize: 0));
    }

    [Fact]
    public void MessageBuffer_WithNegativeMaxSize_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new MessageBuffer(maxSize: -1));
    }

    [Fact]
    public void Add_WithValidMessage_IncreasesCount()
    {
        // Arrange
        var buffer = new MessageBuffer();

        // Act
        buffer.Add("event-1", new { message = "test" });

        // Assert
        Assert.Equal(1, buffer.Count);
    }

    [Fact]
    public void Add_WithNullEventId_ThrowsException()
    {
        // Arrange
        var buffer = new MessageBuffer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => buffer.Add(null!, new { message = "test" }));
    }

    [Fact]
    public void Add_WithEmptyEventId_ThrowsException()
    {
        // Arrange
        var buffer = new MessageBuffer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => buffer.Add("", new { message = "test" }));
    }

    [Fact]
    public void Add_WithNullMessage_ThrowsException()
    {
        // Arrange
        var buffer = new MessageBuffer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => buffer.Add("event-1", null!));
    }

    [Fact]
    public void Add_MultipleMessages_MaintainsOrder()
    {
        // Arrange
        var buffer = new MessageBuffer();

        // Act
        buffer.Add("event-1", new { message = "one" });
        buffer.Add("event-2", new { message = "two" });
        buffer.Add("event-3", new { message = "three" });

        // Assert
        var messages = buffer.GetMessagesAfter(null).ToList();
        Assert.Equal(3, messages.Count);
        Assert.Equal("event-1", messages[0].EventId);
        Assert.Equal("event-2", messages[1].EventId);
        Assert.Equal("event-3", messages[2].EventId);
    }

    [Fact]
    public void Add_ExceedingMaxSize_RemovesOldest()
    {
        // Arrange
        var buffer = new MessageBuffer(maxSize: 10);

        // Act - Add 15 messages
        for (int i = 0; i < 15; i++)
        {
            buffer.Add($"event-{i}", new { message = $"test-{i}" });
        }

        // Assert - Only last 10 should remain
        var messages = buffer.GetMessagesAfter(null).ToList();
        Assert.Equal(10, messages.Count);
        Assert.Equal("event-5", messages[0].EventId); // First is event-5 (oldest kept)
        Assert.Equal("event-14", messages[9].EventId); // Last is event-14 (newest)
    }

    [Fact]
    public void GetMessagesAfter_WithNullLastEventId_ReturnsAllMessages()
    {
        // Arrange
        var buffer = new MessageBuffer();
        buffer.Add("event-1", new { message = "one" });
        buffer.Add("event-2", new { message = "two" });
        buffer.Add("event-3", new { message = "three" });

        // Act
        var messages = buffer.GetMessagesAfter(null).ToList();

        // Assert
        Assert.Equal(3, messages.Count);
    }

    [Fact]
    public void GetMessagesAfter_WithEmptyLastEventId_ReturnsAllMessages()
    {
        // Arrange
        var buffer = new MessageBuffer();
        buffer.Add("event-1", new { message = "one" });
        buffer.Add("event-2", new { message = "two" });

        // Act
        var messages = buffer.GetMessagesAfter("").ToList();

        // Assert
        Assert.Equal(2, messages.Count);
    }

    [Fact]
    public void GetMessagesAfter_WithValidLastEventId_ReturnsSubsequentMessages()
    {
        // Arrange
        var buffer = new MessageBuffer();
        buffer.Add("event-1", new { message = "one" });
        buffer.Add("event-2", new { message = "two" });
        buffer.Add("event-3", new { message = "three" });
        buffer.Add("event-4", new { message = "four" });

        // Act
        var messages = buffer.GetMessagesAfter("event-2").ToList();

        // Assert
        Assert.Equal(2, messages.Count);
        Assert.Equal("event-3", messages[0].EventId);
        Assert.Equal("event-4", messages[1].EventId);
    }

    [Fact]
    public void GetMessagesAfter_WithLastMessage_ReturnsEmpty()
    {
        // Arrange
        var buffer = new MessageBuffer();
        buffer.Add("event-1", new { message = "one" });
        buffer.Add("event-2", new { message = "two" });
        buffer.Add("event-3", new { message = "three" });

        // Act
        var messages = buffer.GetMessagesAfter("event-3").ToList();

        // Assert
        Assert.Empty(messages);
    }

    [Fact]
    public void GetMessagesAfter_WithNonExistentEventId_ReturnsAllMessages()
    {
        // Arrange
        var buffer = new MessageBuffer();
        buffer.Add("event-1", new { message = "one" });
        buffer.Add("event-2", new { message = "two" });

        // Act - Client is too far behind (event not in buffer)
        var messages = buffer.GetMessagesAfter("event-999").ToList();

        // Assert - Return all messages (client needs full replay)
        Assert.Equal(2, messages.Count);
    }

    [Fact]
    public void Clear_RemovesAllMessages()
    {
        // Arrange
        var buffer = new MessageBuffer();
        buffer.Add("event-1", new { message = "one" });
        buffer.Add("event-2", new { message = "two" });
        buffer.Add("event-3", new { message = "three" });

        // Act
        buffer.Clear();

        // Assert
        Assert.Equal(0, buffer.Count);
        Assert.Empty(buffer.GetMessagesAfter(null));
    }

    [Fact]
    public void BufferedMessage_ContainsCorrectData()
    {
        // Arrange
        var buffer = new MessageBuffer();
        var beforeAdd = DateTime.UtcNow;

        // Act
        buffer.Add("event-1", new { message = "test", value = 42 });

        // Assert
        var messages = buffer.GetMessagesAfter(null).ToList();
        var buffered = messages[0];

        Assert.Equal("event-1", buffered.EventId);
        Assert.NotNull(buffered.Message);
        Assert.True(buffered.Timestamp >= beforeAdd);
        Assert.True(buffered.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void MessageBuffer_ThreadSafe_ConcurrentAdds()
    {
        // Arrange
        var buffer = new MessageBuffer(maxSize: 1000);

        // Act - Add 100 messages in parallel
        Parallel.For(0, 100, i =>
        {
            buffer.Add($"event-{i}", new { message = $"test-{i}" });
        });

        // Assert
        Assert.Equal(100, buffer.Count);
    }

    [Fact]
    public void MessageBuffer_ThreadSafe_ConcurrentReads()
    {
        // Arrange
        var buffer = new MessageBuffer();
        for (int i = 0; i < 10; i++)
        {
            buffer.Add($"event-{i}", new { message = $"test-{i}" });
        }

        // Act - Read in parallel
        var results = new System.Collections.Concurrent.ConcurrentBag<int>();
        Parallel.For(0, 100, _ =>
        {
            var messages = buffer.GetMessagesAfter(null).ToList();
            results.Add(messages.Count);
        });

        // Assert - All reads should return same count
        Assert.All(results, count => Assert.Equal(10, count));
    }

    [Fact]
    public void MessageBuffer_IntegrationWithSessionInfo()
    {
        // Arrange
        var session = new SessionInfo
        {
            Id = "test-session",
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow
        };

        // Act
        session.MessageBuffer.Add("event-1", new { notification = "test" });
        session.MessageBuffer.Add("event-2", new { notification = "test2" });

        // Assert
        Assert.Equal(2, session.MessageBuffer.Count);
        var messages = session.MessageBuffer.GetMessagesAfter(null).ToList();
        Assert.Equal(2, messages.Count);
    }
}
