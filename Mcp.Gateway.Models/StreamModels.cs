#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

// -----------------------------------------------------------------------------
// StreamMessageType
// Defines symbolic constants for all supported stream message types
// -----------------------------------------------------------------------------
public static class StreamMessageType
{
    public static readonly string Start = "start";
    public static readonly string Chunk = "chunk";
    public static readonly string Done = "done";
    public static readonly string Error = "error";
}

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

// -----------------------------------------------------------------------------
// StreamMessage
// Represents a single message in the streaming protocol (text wrapper). Can be
// a start, chunk, done, or error event. Binary payloads are sent separately.
// -----------------------------------------------------------------------------
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
    // ------------------------------------------------------------------
    // Factory: Create a "start" message
    // ------------------------------------------------------------------
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

    // ------------------------------------------------------------------
    // Factory: Create a "chunk" message
    // ------------------------------------------------------------------
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

    // ------------------------------------------------------------------
    // Factory: Create a "done" message
    // ------------------------------------------------------------------
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

    // ------------------------------------------------------------------
    // Factory: Create an "error" message
    // ------------------------------------------------------------------
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

    // ------------------------------------------------------------------
    // Convenience flags (ignored when serializing)
    // ------------------------------------------------------------------
    [JsonIgnore] public bool IsStart => Type == StreamMessageType.Start;
    [JsonIgnore] public bool IsChunk => Type == StreamMessageType.Chunk;
    [JsonIgnore] public bool IsDone => Type == StreamMessageType.Done;
    [JsonIgnore] public bool IsError => Type == StreamMessageType.Error;

    // Returns method name from metadata if present
    [JsonIgnore]
    public string? GetMethod => Meta is JsonElement e &&
        e.TryGetProperty("method", out var m) ? m.GetString() : null;

    // Whether the client asked for binary content
    [JsonIgnore]
    public bool ClientWantsBinary => Meta is JsonElement e &&
        e.TryGetProperty("binary", out var b) && b.GetBoolean();

    // Size of the binary header for chunk frames (GUID + Int64 index)
    [JsonIgnore]
    public static int BinaryHeaderSize => 16 + 8; // 16 (GUID) + 8 (Int64 index)

    // Get header info (index and streamId) from binary chunk header bytes
    public static bool TryParseBinaryHeader(byte[] frame, out string? id, out long? index)
    {
        if (frame.Length < BinaryHeaderSize)
        {
            id = null;
            index = null;
            return false; // Ugyldig
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

    // Create header info (index and id) into binary chunk header bytes
    public static byte[] CreateBinaryHeader(string? id, long index)
    {
        var header = new byte[BinaryHeaderSize];

        Guid guid;
        if (!Guid.TryParse(id, out guid))
            guid = Guid.Empty;

        _ = guid.TryWriteBytes(header.AsSpan()[..16]);

        // Write the GUID (first 16 bytes)
        //var guidBytes = guid.ToByteArray();
        //guidBytes.CopyTo(header[..16]);

        // Write the Int64 index (next 8 bytes)
        System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(16, 8), index);
        return header;
    }

    // Convert this message to a JsonElement
    public JsonElement ToJsonElement() =>
        JsonDocument.Parse(JsonSerializer.Serialize(this, JsonOptions.Default)).RootElement.Clone();

    // ------------------------------------------------------------------
    // TryGetFromJsonElement — parses a StreamMessage from JSON
    // ------------------------------------------------------------------
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
