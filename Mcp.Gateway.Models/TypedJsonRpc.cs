namespace Mcp.Gateway.Tools;

public readonly struct TypedJsonRpc<T>(JsonRpcMessage inner)
{
    public object? Id => inner.Id;
    public string? IdAsString => inner.IdAsString;
    public string? Method => inner.Method;
    public JsonRpcMessage Inner => inner;

    public T? GetParams() => inner.GetParams<T>();

    // Evt. noen helper-metoder til
}
