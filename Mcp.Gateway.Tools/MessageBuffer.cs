namespace Mcp.Gateway.Tools;

/// <summary>
/// Thread-safe FIFO message buffer for SSE notification replay (v1.7.0 Phase 2).
/// Stores recent messages per session for Last-Event-ID resumption.
/// </summary>
public sealed class MessageBuffer
{
    private readonly int _maxSize;
    private readonly Queue<BufferedMessage> _messages = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new message buffer with specified maximum size.
    /// </summary>
    /// <param name="maxSize">Maximum number of messages to buffer (default: 100)</param>
    public MessageBuffer(int maxSize = 100)
    {
        if (maxSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size must be greater than 0");

        _maxSize = maxSize;
    }

    /// <summary>
    /// Adds a message to the buffer with event ID and timestamp.
    /// Automatically removes oldest messages when buffer is full.
    /// </summary>
    /// <param name="eventId">SSE event ID (globally unique)</param>
    /// <param name="message">Message payload (notification or response)</param>
    public void Add(string eventId, object message)
    {
        if (string.IsNullOrEmpty(eventId))
            throw new ArgumentNullException(nameof(eventId));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        lock (_lock)
        {
            _messages.Enqueue(new BufferedMessage(eventId, message, DateTime.UtcNow));

            // Remove oldest if over limit (FIFO)
            while (_messages.Count > _maxSize)
            {
                _messages.Dequeue();
            }
        }
    }

    /// <summary>
    /// Gets all messages after the specified event ID (for Last-Event-ID resumption).
    /// If lastEventId is null, returns all buffered messages.
    /// </summary>
    /// <param name="lastEventId">Last event ID received by client (optional)</param>
    /// <returns>Messages after lastEventId, or all messages if null</returns>
    public IEnumerable<BufferedMessage> GetMessagesAfter(string? lastEventId)
    {
        lock (_lock)
        {
            // If no lastEventId, return all messages
            if (string.IsNullOrEmpty(lastEventId))
                return _messages.ToList();

            // Find position of lastEventId
            var skipCount = 0;
            var found = false;
            foreach (var msg in _messages)
            {
                if (msg.EventId == lastEventId)
                {
                    found = true;
                    break;
                }
                skipCount++;
            }

            // If lastEventId not found, return all messages (client may be too far behind)
            if (!found)
                return _messages.ToList();

            // Return messages after lastEventId
            return _messages.Skip(skipCount + 1).ToList();
        }
    }

    /// <summary>
    /// Gets the current count of buffered messages.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _messages.Count;
            }
        }
    }

    /// <summary>
    /// Clears all buffered messages.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _messages.Clear();
        }
    }
}

/// <summary>
/// Represents a buffered message with event ID and timestamp.
/// </summary>
public sealed record BufferedMessage(
    string EventId,
    object Message,
    DateTime Timestamp
);
