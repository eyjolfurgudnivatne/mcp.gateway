namespace Mcp.Gateway.Tests.Unit;

using Mcp.Gateway.Tools;
using Xunit;

/// <summary>
/// Unit tests for SseEventMessage (v1.7.0)
/// </summary>
public class SseEventMessageTests
{
    [Fact]
    public void CreateMessage_CreatesMessageEventWithData()
    {
        // Arrange
        var id = "42";
        var data = new { result = "success" };

        // Act
        var message = SseEventMessage.CreateMessage(id, data);

        // Assert
        Assert.Equal("42", message.Id);
        Assert.Equal("message", message.Event);
        Assert.Equal(data, message.Data);
        Assert.Null(message.Retry);
    }

    [Fact]
    public void CreateDone_CreatesDoneEventWithEmptyData()
    {
        // Arrange
        var id = "100";

        // Act
        var message = SseEventMessage.CreateDone(id);

        // Assert
        Assert.Equal("100", message.Id);
        Assert.Equal("done", message.Event);
        Assert.NotNull(message.Data);
        Assert.Null(message.Retry);
    }

    [Fact]
    public void CreateError_CreatesErrorEventWithJsonRpcError()
    {
        // Arrange
        var id = "50";
        var error = new JsonRpcError(-32700, "Parse error", new { detail = "Invalid JSON" });

        // Act
        var message = SseEventMessage.CreateError(id, error);

        // Assert
        Assert.Equal("50", message.Id);
        Assert.Equal("error", message.Event);
        Assert.NotNull(message.Data);
        Assert.Null(message.Retry);
    }

    [Fact]
    public void CreateKeepAlive_CreatesMessageWithoutIdOrEvent()
    {
        // Act
        var message = SseEventMessage.CreateKeepAlive();

        // Assert
        Assert.Empty(message.Id);
        Assert.Null(message.Event);
        Assert.NotNull(message.Data);
        Assert.Null(message.Retry);
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var id = "123";
        var eventType = "custom";
        var data = new { foo = "bar" };
        var retry = 5000;

        // Act
        var message = new SseEventMessage(id, eventType, data, retry);

        // Assert
        Assert.Equal("123", message.Id);
        Assert.Equal("custom", message.Event);
        Assert.Equal(data, message.Data);
        Assert.Equal(5000, message.Retry);
    }

    [Fact]
    public void Constructor_WithNullEvent_AllowsNullEvent()
    {
        // Arrange & Act
        var message = new SseEventMessage("1", null, new { });

        // Assert
        Assert.Equal("1", message.Id);
        Assert.Null(message.Event);
    }

    [Fact]
    public void Constructor_WithNullRetry_AllowsNullRetry()
    {
        // Arrange & Act
        var message = new SseEventMessage("1", "message", new { }, null);

        // Assert
        Assert.Null(message.Retry);
    }
}
