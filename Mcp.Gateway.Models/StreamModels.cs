#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Defines symbolic constants for all supported stream message types
/// </summary>
public static class StreamMessageType
{
    /// <summary>
    /// Identifies a message that initiates a new stream.
    /// </summary>
    public static readonly string Start = "start";
    /// <summary>
    /// Identifies a message containing a data chunk.
    /// </summary>
    public static readonly string Chunk = "chunk";
    /// <summary>
    /// Identifies a message indicating the stream has completed.
    /// </summary>
    public static readonly string Done = "done";
    /// <summary>
    /// Identifies a message reporting an error in the stream.
    /// </summary>
    public static readonly string Error = "error";
}

/// <summary>
/// Represents metadata describing a stream message, including method, content type, encoding, and related properties.
/// </summary>
/// <param name="Method">The name of the method or operation associated with the stream message. This value is required and typically
/// indicates the action to be performed.</param>
/// <param name="Binary">true if the stream message content is binary; otherwise, false. Defaults to true.</param>
/// <param name="Name">The optional name of the stream or file associated with the message. May be null if not specified.</param>
/// <param name="Mime">The optional MIME type of the stream content. May be null if not specified.</param>
/// <param name="CorrelationId">An optional identifier used to correlate this message with related operations or messages. May be null if not
/// specified.</param>
/// <param name="TotalSize">The optional total size, in bytes, of the stream content. May be null if not specified.</param>
/// <param name="Compression">The optional compression algorithm used for the stream content, if any. May be null if not specified.</param>
/// <param name="Encoding">The optional encoding applied to the stream content, such as 'utf-8'. May be null if not specified.</param>
public record StreamMessageMeta(
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("binary")] bool Binary = true,
    [property: JsonPropertyName("name")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name = null,
    [property: JsonPropertyName("mime")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Mime = null,
    [property: JsonPropertyName("correlationId")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CorrelationId = null,
    [property: JsonPropertyName("totalSize")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? TotalSize = null,
    [property: JsonPropertyName("compression")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Compression = null,
    [property: JsonPropertyName("encoding")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Encoding = null)
{
    /// <summary>
    /// Attempts to extract a StreamMessageMeta instance from the specified object or JSON element.
    /// </summary>
    /// <remarks>If the input is a JsonElement, it must represent a JSON object with properties compatible
    /// with StreamMessageMeta. Properties that are missing or of an unexpected type will result in default values for
    /// those fields in the resulting StreamMessageMeta instance.</remarks>
    /// <param name="element">The object to inspect for StreamMessageMeta data. This can be a StreamMessageMeta instance or a JsonElement
    /// representing an object with compatible properties.</param>
    /// <param name="message">When this method returns, contains the extracted StreamMessageMeta if successful; otherwise, null. This
    /// parameter is passed uninitialized.</param>
    /// <returns>true if a StreamMessageMeta instance was successfully extracted from the input; otherwise, false.</returns>
    public static bool TryGetFromObject(object? element, out StreamMessageMeta? message)
    {
        if (element is StreamMessageMeta meta)
        {
            message = meta;
            return true;
        }
        if (element is JsonElement e && e.ValueKind == JsonValueKind.Object)
        {
            var method = e.TryGetProperty("method", out var mProp) ? mProp.GetString() ?? "" : "";
            var binary = e.TryGetProperty("binary", out var bProp) && bProp.GetBoolean();
            var name = e.TryGetProperty("name", out var nProp) ? nProp.GetString() : null;
            var mime = e.TryGetProperty("mime", out var mimeProp) ? mimeProp.GetString() : null;
            var correlationId = e.TryGetProperty("correlationId", out var cProp) ? cProp.GetString() : null;
            long? totalSize = e.TryGetProperty("totalSize", out var tsProp) && tsProp.ValueKind == JsonValueKind.Number
                ? tsProp.GetInt64()
                : null;
            var compression = e.TryGetProperty("compression", out var compProp) ? compProp.GetString() : null;
            var encoding = e.TryGetProperty("encoding", out var encProp) ? encProp.GetString() : null;
            
            message = new StreamMessageMeta(
                Method: method,
                Binary: binary,
                Name: name,
                Mime: mime,
                CorrelationId: correlationId,
                TotalSize: totalSize,
                Compression: compression,
                Encoding: encoding);
            return true;
        }
        message = null;
        return false;
    }
};

/// <summary>
/// Represents a single message in the streaming protocol (text wrapper). Can be
/// a start, chunk, done, or error event. Binary payloads are sent separately.
/// </summary>
/// <param name="Type">The message type discriminator. Must be one of: <c>start</c>, <c>chunk</c>, <c>done</c>, or <c>error</c>.</param>
/// <param name="Timestamp">The UTC timestamp when the message was created.</param>
/// <param name="Id">A unique identifier (GUID) for the stream. Used to correlate chunks to a specific stream.</param>
/// <param name="Meta">Metadata object describing the stream (e.g., method name, MIME type). Only present on <c>start</c> messages.</param>
/// <param name="Index">A sequential counter for ordering chunks. Only present on <c>chunk</c> messages.</param>
/// <param name="Data">The actual payload content for text streams. Only present on <c>chunk</c> messages.</param>
/// <param name="Summary">An aggregated result or final status object. Only present on <c>done</c> messages.</param>
/// <param name="Error">A structured JSON-RPC error object. Only present on <c>error</c> messages.</param>
public sealed record StreamMessage(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("id")] string? Id = null,

    // Optional metadata attached to the start message
    [property: JsonPropertyName("meta")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Meta = default,

    // Optional sequence number for chunk events
    [property: JsonPropertyName("index")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? Index = null,

    // Optional JSON payload (used for text streaming)
    [property: JsonPropertyName("data")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Data = default,

    // Optional aggregated summary when stream ends
    [property: JsonPropertyName("summary")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Summary = default,

    // Optional JSON-RPC error object for error events
    [property: JsonPropertyName("error")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] JsonRpcError? Error = null)
{
    /// <summary>
    /// Factory: Create a "start" message to initiate a new stream.
    /// </summary>
    /// <param name="Meta">The metadata object describing the stream properties (e.g., method, MIME type, encoding).</param>
    /// <returns>A new <see cref="StreamMessage"/> instance initialized as a start message.</returns>
    public static StreamMessage CreateStartMessage(object? Meta) =>
        new(
            Type: StreamMessageType.Start,
            Id: Guid.NewGuid().ToString("D"),
            Timestamp: DateTimeOffset.UtcNow,
            Meta: Meta,
            Index: null,
            Data: default,
            Summary: default,
            Error: null);

    /// <summary>
    /// Factory: Create a "chunk" message containing a piece of the stream data.
    /// </summary>
    /// <param name="Id">The unique identifier for the stream.</param>
    /// <param name="Index">The index of the chunk within the stream.</param>
    /// <param name="Data">The data payload for the chunk.</param>
    /// <returns>A new <see cref="StreamMessage"/> instance initialized as a chunk message.</returns>
    public static StreamMessage CreateChunkMessage(string? Id, long? Index, object? Data) =>
        new(
            Type: StreamMessageType.Chunk,
            Id: Id,
            Timestamp: DateTimeOffset.UtcNow,
            Meta: default,
            Index: Index,
            Data: Data,
            Summary: default,
            Error: null);

    /// <summary>
    /// Factory: Create a "done" message indicating the end of the stream.
    /// </summary>
    /// <param name="Id">The unique identifier for the stream.</param>
    /// <param name="Summary">The summary object containing final stream metadata or results.</param>
    /// <returns>A new <see cref="StreamMessage"/> instance initialized as a done message.</returns>
    public static StreamMessage CreateDoneMessage(string? Id, object? Summary) =>
        new(
            Type: StreamMessageType.Done,
            Id: Id,
            Timestamp: DateTimeOffset.UtcNow,
            Meta: default,
            Index: null,
            Data: default,
            Summary: Summary,
            Error: null);

    /// <summary>
    /// Creates a new error message for a stream, associating it with the specified identifier and error details.
    /// </summary>
    /// <param name="Id">The identifier of the message to associate with the error. Can be null if the error is not related to a specific
    /// message.</param>
    /// <param name="Error">The error details to include in the message. Can be null if no additional error information is available.</param>
    /// <returns>A <see cref="StreamMessage"/> instance representing an error message with the specified identifier and error
    /// details.</returns>
    public static StreamMessage CreateErrorMessage(string? Id, JsonRpcError? Error) =>
        new(
            Type: StreamMessageType.Error,
            Id: Id,
            Timestamp: DateTimeOffset.UtcNow,
            Meta: default,
            Index: null,
            Data: default,
            Summary: default,
            Error: Error);

    /// <summary>
    /// Is this a "start" message?
    /// </summary>
    [JsonIgnore] public bool IsStart => Type == StreamMessageType.Start;
    /// <summary>
    /// Is this a "chunk" message?
    /// </summary>
    [JsonIgnore] public bool IsChunk => Type == StreamMessageType.Chunk;
    /// <summary>
    /// Gets a value indicating whether this message represents a completed operation.
    /// </summary>
    [JsonIgnore] public bool IsDone => Type == StreamMessageType.Done;
    /// <summary>
    /// Is this an "error" message?
    /// </summary>
    [JsonIgnore] public bool IsError => Type == StreamMessageType.Error;

    /// <summary>
    /// Gets the value of the "method" property from the associated metadata, if present.
    /// </summary>
    [JsonIgnore]
    public string? GetMethod => Meta is JsonElement e &&
        e.TryGetProperty("method", out var m) ? m.GetString() : null;

    /// <summary>
    /// Gets a value indicating whether the client has requested binary content.
    /// </summary>
    /// <remarks>This property checks for a "binary" flag in the associated metadata to determine the client's
    /// content preference. Use this property to adapt response formats based on client capabilities or
    /// requests.</remarks>
    [JsonIgnore]
    public bool ClientWantsBinary => Meta is JsonElement e &&
        e.TryGetProperty("binary", out var b) && b.GetBoolean();

    /// <summary>
    /// Gets the size, in bytes, of the binary header used for serialization.
    /// Size of the binary header for chunk frames (GUID + Int64 index)
    /// </summary>
    [JsonIgnore]
    public static int BinaryHeaderSize => 16 + 8; // 16 (GUID) + 8 (Int64 index)

    /// <summary>
    /// Attempts to parse a binary header from the specified frame and extract the identifier and index values.
    /// </summary>
    /// <remarks>The method expects the frame to contain a GUID (16 bytes) followed by a 64-bit integer (8
    /// bytes) in binary format. If the frame is too short or the data is invalid, the method returns <see
    /// langword="false"/> and sets both output parameters to <see langword="null"/>.</remarks>
    /// <param name="frame">The byte array containing the binary header to parse. Must be at least the required header size in length.</param>
    /// <param name="id">When this method returns, contains the parsed identifier as a string if parsing succeeds; otherwise, <see
    /// langword="null"/>. This parameter is passed uninitialized.</param>
    /// <param name="index">When this method returns, contains the parsed index value if parsing succeeds; otherwise, <see
    /// langword="null"/>. This parameter is passed uninitialized.</param>
    /// <returns><see langword="true"/> if the binary header was successfully parsed and both values were extracted; otherwise,
    /// <see langword="false"/>.</returns>
    public static bool TryParseBinaryHeader(byte[] frame, out string? id, out long? index)
    {
        if (frame.Length < BinaryHeaderSize)
        {
            id = null;
            index = null;
            return false; // Invalid
        }

        Span<byte> span = frame;

        try
        {
            // Read the GUID (first 16 bytes)
            var guidBytes = span[..16];
            id = new Guid(guidBytes).ToString("D");

            // Read the Int64 index (next 8 bytes)
            var indexBytes = span[16..24];
            index = BitConverter.ToInt64(indexBytes);
            return true;
        }
        catch { /*Silent*/ }

        id = null;
        index = null;
        return false;
    }

    /// <summary>
    /// Creates a binary header containing a GUID and a 64-bit integer index, formatted for use in binary protocols or
    /// file formats.
    /// </summary>
    /// <remarks>The returned header is always of a fixed size. The GUID is stored in standard binary format,
    /// and the index is stored in little-endian order. This method does not throw exceptions for invalid GUID input;
    /// instead, it substitutes an empty GUID.</remarks>
    /// <param name="id">The string representation of the GUID to include in the header. If null or not a valid GUID, an empty GUID is
    /// used.</param>
    /// <param name="index">The 64-bit integer value to include in the header, written in little-endian format.</param>
    /// <returns>A byte array containing the binary header, with the first 16 bytes representing the GUID and the next 8 bytes
    /// representing the index.</returns>
    public static byte[] CreateBinaryHeader(string? id, long index)
    {
        var header = new byte[BinaryHeaderSize];

        if (!Guid.TryParse(id, out Guid guid))
            guid = Guid.Empty;

        _ = guid.TryWriteBytes(header.AsSpan()[..16]);

        // Write the GUID (first 16 bytes)
        //var guidBytes = guid.ToByteArray();
        //guidBytes.CopyTo(header[..16]);

        // Write the Int64 index (next 8 bytes)
        System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(16, 8), index);
        return header;
    }

    /// <summary>
    /// Converts the current object to a JSON representation as a <see cref="JsonElement"/>.
    /// </summary>
    /// <remarks>The returned <see cref="JsonElement"/> is a deep clone and is independent of the original
    /// object. Changes to the object after calling this method are not reflected in the returned JSON
    /// element.</remarks>
    /// <returns>A <see cref="JsonElement"/> containing the serialized JSON representation of the current object.</returns>
    public JsonElement ToJsonElement() =>
        JsonDocument.Parse(JsonSerializer.Serialize(this, JsonOptions.Default)).RootElement.Clone();

    /// <summary>
    /// Attempts to parse a StreamMessage from the specified JSON element.
    /// </summary>
    /// <remarks>The JSON element must contain at least the "type" and "timestamp" properties for parsing to
    /// succeed. Additional properties may be required depending on the message type. If the element does not conform to
    /// the expected structure, the method returns false and sets message to null.</remarks>
    /// <param name="element">The JsonElement containing the data to parse as a StreamMessage. Must represent a valid JSON object with
    /// required properties.</param>
    /// <param name="message">When this method returns, contains the parsed StreamMessage if parsing succeeded; otherwise, null. This
    /// parameter is passed uninitialized.</param>
    /// <returns>true if the JSON element was successfully parsed into a StreamMessage; otherwise, false.</returns>
    public static bool TryGetFromJsonElement(JsonElement element, out StreamMessage? message)
    {
        var type = element.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
        var timestamp = element.TryGetProperty("timestamp", out var tsProp)
            ? tsProp.GetDateTimeOffset()
            : default;

        if (type is null || timestamp == default)
        {
            message = null;
            return false;
        }

        var id = element.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

        switch (type)
        {
            // --------------------------------------------------------------
            // START
            // --------------------------------------------------------------
            case var t when t == StreamMessageType.Start:
                {
                    object? meta = element.TryGetProperty("meta", out var mProp)
                        ? JsonSerializer.Deserialize<object>(mProp.GetRawText())
                        : null;

                    message = new(
                        Type: StreamMessageType.Start,
                        Id: id,
                        Timestamp: timestamp,
                        Meta: meta,
                        Index: null,
                        Data: null,
                        Summary: null,
                        Error: null);
                    return true;
                }

            // --------------------------------------------------------------
            // CHUNK
            // --------------------------------------------------------------
            case var t when t == StreamMessageType.Chunk:
                {
                    long? index = element.TryGetProperty("index", out var idxProp) && idxProp.ValueKind == JsonValueKind.Number
                        ? idxProp.GetInt64()
                        : null;

                    object? data = element.TryGetProperty("data", out var dProp)
                        ? JsonSerializer.Deserialize<object>(dProp.GetRawText())
                        : null;

                    message = new(
                        Type: StreamMessageType.Chunk,
                        Id: id,
                        Timestamp: timestamp,
                        Meta: null,
                        Index: index,
                        Data: data,
                        Summary: null,
                        Error: null);
                    return true;
                }

            // --------------------------------------------------------------
            // DONE
            // --------------------------------------------------------------
            case var t when t == StreamMessageType.Done:
                {
                    object? summary = element.TryGetProperty("summary", out var sProp)
                        ? JsonSerializer.Deserialize<object>(sProp.GetRawText())
                        : null;

                    message = new(
                        Type: StreamMessageType.Done,
                        Id: id,
                        Timestamp: timestamp,
                        Meta: null,
                        Index: null,
                        Data: null,
                        Summary: summary,
                        Error: null);
                    return true;
                }

            // --------------------------------------------------------------
            // ERROR
            // --------------------------------------------------------------
            case var t when t == StreamMessageType.Error:
                {
                    JsonRpcError? err = null;
                    if (element.TryGetProperty("error", out var eProp))
                        _ = JsonRpcError.TryGetFromJsonElement(eProp, out err);

                    message = new(
                        Type: StreamMessageType.Error,
                        Id: id,
                        Timestamp: timestamp,
                        Meta: null,
                        Index: null,
                        Data: null,
                        Summary: null,
                        Error: err);
                    return true;
                }
        }

        message = null;
        return false;
    }
}
