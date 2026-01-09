namespace Mcp.Gateway.Tools;

using System.Collections.Concurrent;

public partial class ToolService
{
    // Storage for metadata of dynamically registered resources
    private readonly ConcurrentDictionary<string, ResourceDefinition> _dynamicResourceMetadata = new();

    /// <summary>
    /// Dynamically registers a new resource at runtime.
    /// </summary>
    /// <param name="uri">The unique resource URI (e.g., "dynamic://data/1").</param>
    /// <param name="handler">The delegate to handle the request (Func&lt;JsonRpcMessage, Task&lt;JsonRpcMessage&gt;&gt;).</param>
    /// <param name="name">Display name for the resource.</param>
    /// <param name="description">Description for the resource.</param>
    /// <param name="mimeType">MIME type (e.g., "application/json").</param>
    public void RegisterResource(
        string uri,
        Delegate handler,
        string? name = null,
        string? description = null,
        string? mimeType = null)
    {
        if (!IsValidResourceUri(uri))
        {
            throw new ArgumentException($"Invalid resource URI: '{uri}'");
        }

        // 1. Register the execution logic
        // This reuses the existing internal registration logic
        RegisterFunction(uri, FunctionTypeEnum.Resource, handler);

        // 2. Store metadata for discovery (resources/list)
        var metadata = new ResourceDefinition()
        {
            Uri = uri,
            Name = name ?? uri,
            Description = description,
            MimeType = mimeType
        };

        _dynamicResourceMetadata.AddOrUpdate(uri, metadata, (_, _) => metadata);
    }

    /// <summary>
    /// Unregisters an existing resource.
    /// </summary>
    /// <param name="uri">The URI of the resource to remove.</param>
    public void UnregisterResource(string uri)
    {
        // 1. Remove from metadata
        _dynamicResourceMetadata.TryRemove(uri, out _);

        // 2. Remove from execution registry
        // Note: Assuming ConfiguredFunctions is accessible and thread-safe (ConcurrentDictionary)
        if (ConfiguredFunctions is IDictionary<string, FunctionDetails> dict)
        {
            dict.Remove(uri);
        }
        else if (ConfiguredFunctions is ConcurrentDictionary<string, FunctionDetails> concurrentDict)
        {
            concurrentDict.TryRemove(uri, out _);
        }
    }

    /// <summary>
    /// Helper to merge dynamic resources into the discovery list.
    /// Update your main GetAllResourceDefinitions() to call this.
    /// </summary>
    private IEnumerable<ResourceDefinition> GetDynamicResourceDefinitions()
    {
        return _dynamicResourceMetadata.Values;
    }
}