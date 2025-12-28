#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public readonly struct TypedJsonRpc<T>(JsonRpcMessage inner)
{
    public object? Id => inner.Id;
    public string? IdAsString => inner.IdAsString;
    public string? Method => inner.Method;
    public JsonRpcMessage Inner => inner;

    public T? GetParams() => inner.GetParams<T>();

    // Evt. noen helper-metoder til
}
