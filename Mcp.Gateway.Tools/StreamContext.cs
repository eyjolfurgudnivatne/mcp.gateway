namespace Mcp.Gateway.Tools;

/// <summary>
/// Tracks the state of a streaming operation.
/// Used by ToolConnector to manage stream lifecycle and validation.
/// </summary>
public class StreamContext
{
    /// <summary>
    /// Unique identifier for this stream.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// True if this is a binary stream, false for text/JSON.
    /// </summary>
    public bool IsBinary { get; }

    /// <summary>
    /// Metadata from the start message.
    /// </summary>
    public StreamMessageMeta? Meta { get; }

    /// <summary>
    /// True if local side (server) has sent done.
    /// </summary>
    public bool LocalDone { get; internal set; }

    /// <summary>
    /// True if remote side (client) has sent done.
    /// </summary>
    public bool RemoteDone { get; internal set; }

    /// <summary>
    /// True if an error occurred.
    /// </summary>
    public bool Errored { get; internal set; }

    /// <summary>
    /// True if stream is closed.
    /// </summary>
    public bool Closed { get; internal set; }

    /// <summary>
    /// True if stream has reached a terminal state (both done, error, or closed).
    /// </summary>
    public bool Terminal => (LocalDone && RemoteDone) || Errored || Closed;

    /// <summary>
    /// True if we're expecting binary chunks (based on start message).
    /// </summary>
    public bool ExpectingBinary { get; internal set; }

    /// <summary>
    /// Timestamp of last activity (for timeout detection).
    /// </summary>
    public DateTime LastActivity { get; internal set; }

    /// <summary>
    /// Creates a new StreamContext.
    /// </summary>
    public StreamContext(string id, bool isBinary, StreamMessageMeta? meta)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        IsBinary = isBinary;
        Meta = meta;
        ExpectingBinary = isBinary;
        LastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates last activity timestamp to now.
    /// </summary>
    internal void Touch()
    {
        LastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if stream has been idle for longer than timeout.
    /// </summary>
    public bool IsTimedOut(TimeSpan timeout)
    {
        return DateTime.UtcNow - LastActivity > timeout;
    }
}
