namespace Mcp.Gateway.Tools;

/// <summary>
/// Invalid JSON was received by the server.
/// An error occurred on the server while parsing the JSON text.
/// </summary>
/// <remarks>-32700 Parse error</remarks>
/// <param name="message">Extra error information passed to data propery.</param>
public class ToolParseErrorException(string message) : Exception(message) { }

/// <summary>
/// The JSON sent is not a valid Request object.
/// </summary>
/// <remarks>-32600 Invalid Request</remarks>
/// <param name="message">Extra error information passed to data propery.</param>
public class ToolInvalidRequestException(string message) : Exception(message) { }

/// <summary>
/// The method does not exist / is not available.
/// </summary>
/// <remarks>-32601 Method not found</remarks>
/// <param name="message">Extra error information passed to data propery.</param>
public class ToolNotFoundException(string message) : Exception(message) { }

/// <summary>
/// Invalid method parameter(s).
/// </summary>
/// <remarks>-32602 Invalid params</remarks>
/// <param name="message">Extra error information passed to data propery.</param>
public class ToolInvalidParamsException : Exception
{
    public string? ToolName { get; }
    
    public ToolInvalidParamsException(string message) : base(message) { }
    
    public ToolInvalidParamsException(string message, string toolName) : base(message)
    {
        ToolName = toolName;
    }
}

/// <summary>
/// Internal JSON-RPC error.
/// </summary>
/// <remarks>-32603 Internal error</remarks>
/// <param name="message">Extra error information passed to data propery.</param>
public class ToolInternalErrorException(string message) : Exception(message) { }

/// <summary>
/// Session not found or expired (v1.8.0).
/// Used for better error guidance when sessions expire.
/// </summary>
/// <remarks>-32001 Session error (custom code)</remarks>
public class SessionExpiredException : Exception
{
    public string SessionId { get; }
    public int TimeoutMinutes { get; }
    
    public SessionExpiredException(string sessionId, int timeoutMinutes = 30) 
        : base($"Session '{sessionId}' not found or expired after {timeoutMinutes} minutes of inactivity")
    {
        SessionId = sessionId;
        TimeoutMinutes = timeoutMinutes;
    }
}
