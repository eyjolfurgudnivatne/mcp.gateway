namespace Mcp.Gateway.Tools;

using System.Text.Json.Serialization;

/// <summary>
/// JSON Source Generation context for compile-time serialization.
/// This provides 30-50% performance improvement over runtime reflection.
/// 
/// NOTE: Source generators have limitations with polymorphic types (object?).
/// We use Metadata mode for full runtime support while still getting some benefits.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(JsonRpcMessage))]
[JsonSerializable(typeof(JsonRpcError))]
[JsonSerializable(typeof(StreamMessage))]
[JsonSerializable(typeof(StreamMessageMeta))]
[JsonSerializable(typeof(ToolService.ToolDefinition))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext
{
}
