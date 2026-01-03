#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System;

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
public class ToolInvalidParamsException : Exception
{
    /// <summary>
    /// The name of the tool associated with the invalid parameters.
    /// </summary>
    public string? ToolName { get; }

    /// <summary>
    /// Initializes a new instance of the ToolInvalidParamsException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ToolInvalidParamsException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the ToolInvalidParamsException class with a specified error message and the name
    /// of the tool that caused the exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="toolName">The name of the tool associated with the invalid parameters.</param>
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
/// <remarks>
/// Initializes a new instance of the SessionExpiredException class with a specified session ID.
/// </remarks>
/// <param name="sessionId">The unique identifier for the current session.</param>
/// <param name="timeoutMinutes">Set the timeout duration, in minutes, for the associated operation or process.</param>
public class SessionExpiredException(string sessionId, int timeoutMinutes = 30) : Exception($"Session '{sessionId}' not found or expired after {timeoutMinutes} minutes of inactivity")
{
    /// <summary>
    /// Gets the unique identifier for the current session.
    /// </summary>
    public string SessionId { get; } = sessionId;

    /// <summary>
    /// Gets the timeout duration, in minutes, for the associated operation or process.
    /// </summary>
    public int TimeoutMinutes { get; } = timeoutMinutes;
}
