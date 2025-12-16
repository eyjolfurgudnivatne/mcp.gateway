namespace Mcp.Gateway.Tools.Pagination;

using System;
using System.Text;
using System.Text.Json;

/// <summary>
/// Helper class for cursor-based pagination in MCP protocol (v1.6.0+).
/// Cursors are base64-encoded JSON objects containing pagination state.
/// </summary>
public static class CursorHelper
{
    /// <summary>
    /// Default page size for paginated list operations.
    /// Can be overridden per transport or per request.
    /// </summary>
    public const int DefaultPageSize = 100;

    /// <summary>
    /// Represents the pagination state encoded in a cursor.
    /// </summary>
    public sealed record CursorState(int Offset);

    /// <summary>
    /// Encodes a cursor state to a base64 string.
    /// </summary>
    /// <param name="offset">The offset for the next page</param>
    /// <returns>Base64-encoded cursor string</returns>
    public static string EncodeCursor(int offset)
    {
        var state = new CursorState(offset);
        var json = JsonSerializer.Serialize(state, JsonOptions.Default);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Decodes a cursor string to extract the pagination state.
    /// </summary>
    /// <param name="cursor">Base64-encoded cursor string</param>
    /// <returns>Cursor state with offset, or null if invalid</returns>
    public static CursorState? DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<CursorState>(json, JsonOptions.Default);
        }
        catch
        {
            // Invalid cursor format - return null
            return null;
        }
    }

    /// <summary>
    /// Applies pagination to a collection of items.
    /// </summary>
    /// <typeparam name="T">Type of items in the collection</typeparam>
    /// <param name="items">All items to paginate</param>
    /// <param name="cursor">Optional cursor for pagination state</param>
    /// <param name="pageSize">Number of items per page (default: 100)</param>
    /// <returns>Paginated result with items and optional next cursor</returns>
    public static PaginatedResult<T> Paginate<T>(
        IEnumerable<T> items,
        string? cursor = null,
        int pageSize = DefaultPageSize)
    {
        // Decode cursor to get offset
        var state = DecodeCursor(cursor);
        var offset = state?.Offset ?? 0;

        // Validate offset
        if (offset < 0)
            offset = 0;

        // Validate page size
        if (pageSize <= 0)
            pageSize = DefaultPageSize;

        // Apply pagination
        var allItems = items.ToList();
        var pagedItems = allItems
            .Skip(offset)
            .Take(pageSize)
            .ToList();

        // Calculate next cursor
        string? nextCursor = null;
        var nextOffset = offset + pageSize;
        if (nextOffset < allItems.Count)
        {
            // More items available - generate next cursor
            nextCursor = EncodeCursor(nextOffset);
        }

        return new PaginatedResult<T>(pagedItems, nextCursor);
    }

    /// <summary>
    /// Result of a paginated operation.
    /// </summary>
    /// <typeparam name="T">Type of items</typeparam>
    /// <param name="Items">Items for the current page</param>
    /// <param name="NextCursor">Cursor for next page, or null if last page</param>
    public sealed record PaginatedResult<T>(
        IReadOnlyList<T> Items,
        string? NextCursor);
}
