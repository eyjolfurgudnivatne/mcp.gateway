namespace Mcp.Gateway.Tests.Unit;

using Mcp.Gateway.Tools;
using Xunit;

/// <summary>
/// Unit tests for SessionService (v1.7.0)
/// </summary>
public class SessionServiceTests
{
    [Fact]
    public void CreateSession_ReturnsValidSessionId()
    {
        // Arrange
        var service = new SessionService();

        // Act
        var sessionId = service.CreateSession();

        // Assert
        Assert.NotNull(sessionId);
        Assert.NotEmpty(sessionId);
        Assert.Equal(32, sessionId.Length); // GUID in "N" format (32 hex digits)
        Assert.True(Guid.TryParseExact(sessionId, "N", out _), "Session ID should be a valid GUID in N format");
    }

    [Fact]
    public void CreateSession_EachCallReturnsUniqueId()
    {
        // Arrange
        var service = new SessionService();

        // Act
        var sessionId1 = service.CreateSession();
        var sessionId2 = service.CreateSession();
        var sessionId3 = service.CreateSession();

        // Assert
        Assert.NotEqual(sessionId1, sessionId2);
        Assert.NotEqual(sessionId2, sessionId3);
        Assert.NotEqual(sessionId1, sessionId3);
    }

    [Fact]
    public void ValidateSession_WithValidSession_ReturnsTrue()
    {
        // Arrange
        var service = new SessionService();
        var sessionId = service.CreateSession();

        // Act
        var isValid = service.ValidateSession(sessionId);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateSession_WithNonExistentSession_ReturnsFalse()
    {
        // Arrange
        var service = new SessionService();
        var nonExistentSessionId = Guid.NewGuid().ToString("N");

        // Act
        var isValid = service.ValidateSession(nonExistentSessionId);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateSession_WithNullSessionId_ReturnsFalse()
    {
        // Arrange
        var service = new SessionService();

        // Act
        var isValid = service.ValidateSession(null!);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateSession_WithEmptySessionId_ReturnsFalse()
    {
        // Arrange
        var service = new SessionService();

        // Act
        var isValid = service.ValidateSession(string.Empty);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateSession_UpdatesLastActivity()
    {
        // Arrange
        var service = new SessionService();
        var sessionId = service.CreateSession();
        var sessionBefore = service.GetSession(sessionId);
        var lastActivityBefore = sessionBefore!.LastActivity;

        // Wait a bit to ensure time difference
        Thread.Sleep(10);

        // Act
        service.ValidateSession(sessionId);
        var sessionAfter = service.GetSession(sessionId);
        var lastActivityAfter = sessionAfter!.LastActivity;

        // Assert
        Assert.True(lastActivityAfter > lastActivityBefore, "LastActivity should be updated");
    }

    [Fact]
    public void ValidateSession_WithExpiredSession_ReturnsFalse()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(50);
        var service = new SessionService(timeout);
        var sessionId = service.CreateSession();

        // Wait for session to expire
        Thread.Sleep(100);

        // Act
        var isValid = service.ValidateSession(sessionId);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateSession_ExpiredSessionIsRemoved()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(50);
        var service = new SessionService(timeout);
        var sessionId = service.CreateSession();

        // Confirm session exists
        Assert.Equal(1, service.ActiveSessionCount);

        // Wait for session to expire
        Thread.Sleep(100);

        // Act
        service.ValidateSession(sessionId);

        // Assert - session should be removed
        Assert.Equal(0, service.ActiveSessionCount);
    }

    [Fact]
    public void DeleteSession_RemovesSession()
    {
        // Arrange
        var service = new SessionService();
        var sessionId = service.CreateSession();

        // Confirm session exists
        Assert.True(service.ValidateSession(sessionId));

        // Act
        var deleted = service.DeleteSession(sessionId);

        // Assert
        Assert.True(deleted);
        Assert.False(service.ValidateSession(sessionId));
    }

    [Fact]
    public void DeleteSession_WithNonExistentSession_ReturnsFalse()
    {
        // Arrange
        var service = new SessionService();
        var nonExistentSessionId = Guid.NewGuid().ToString("N");

        // Act
        var deleted = service.DeleteSession(nonExistentSessionId);

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public void GetNextEventId_ReturnsIncrementingValues()
    {
        // Arrange
        var service = new SessionService();
        var sessionId = service.CreateSession();

        // Act
        var eventId1 = service.GetNextEventId(sessionId);
        var eventId2 = service.GetNextEventId(sessionId);
        var eventId3 = service.GetNextEventId(sessionId);

        // Assert
        Assert.Equal(1, eventId1);
        Assert.Equal(2, eventId2);
        Assert.Equal(3, eventId3);
    }

    [Fact]
    public void GetNextEventId_WithNonExistentSession_ThrowsException()
    {
        // Arrange
        var service = new SessionService();
        var nonExistentSessionId = Guid.NewGuid().ToString("N");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.GetNextEventId(nonExistentSessionId));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void GetNextEventId_IsThreadSafe()
    {
        // Arrange
        var service = new SessionService();
        var sessionId = service.CreateSession();
        var eventIds = new System.Collections.Concurrent.ConcurrentBag<long>();

        // Act - Generate 1000 event IDs in parallel
        Parallel.For(0, 1000, _ =>
        {
            var eventId = service.GetNextEventId(sessionId);
            eventIds.Add(eventId);
        });

        // Assert - All event IDs should be unique
        var distinctIds = eventIds.Distinct().ToList();
        Assert.Equal(1000, distinctIds.Count);
    }

    [Fact]
    public void GetSession_ReturnsSessionInfo()
    {
        // Arrange
        var service = new SessionService();
        var sessionId = service.CreateSession();

        // Act
        var session = service.GetSession(sessionId);

        // Assert
        Assert.NotNull(session);
        Assert.Equal(sessionId, session.Id);
        Assert.True(session.CreatedAt <= DateTime.UtcNow);
        Assert.Equal(0, session.EventIdCounter);
    }

    [Fact]
    public void GetSession_WithNonExistentSession_ReturnsNull()
    {
        // Arrange
        var service = new SessionService();
        var nonExistentSessionId = Guid.NewGuid().ToString("N");

        // Act
        var session = service.GetSession(nonExistentSessionId);

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public void ActiveSessionCount_ReturnsCorrectCount()
    {
        // Arrange
        var service = new SessionService();

        // Act & Assert
        Assert.Equal(0, service.ActiveSessionCount);

        var session1 = service.CreateSession();
        Assert.Equal(1, service.ActiveSessionCount);

        var session2 = service.CreateSession();
        Assert.Equal(2, service.ActiveSessionCount);

        service.DeleteSession(session1);
        Assert.Equal(1, service.ActiveSessionCount);

        service.DeleteSession(session2);
        Assert.Equal(0, service.ActiveSessionCount);
    }

    [Fact]
    public void CleanupExpiredSessions_RemovesOnlyExpiredSessions()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(50);
        var service = new SessionService(timeout);

        var session1 = service.CreateSession();
        Thread.Sleep(60); // Let session1 expire
        var session2 = service.CreateSession(); // Fresh session

        // Act
        var removedCount = service.CleanupExpiredSessions();

        // Assert
        Assert.Equal(1, removedCount);
        Assert.Equal(1, service.ActiveSessionCount);
        Assert.False(service.ValidateSession(session1));
        Assert.True(service.ValidateSession(session2));
    }

    [Fact]
    public void CleanupExpiredSessions_WithNoExpiredSessions_ReturnsZero()
    {
        // Arrange
        var service = new SessionService();
        service.CreateSession();
        service.CreateSession();

        // Act
        var removedCount = service.CleanupExpiredSessions();

        // Assert
        Assert.Equal(0, removedCount);
        Assert.Equal(2, service.ActiveSessionCount);
    }
}
