namespace Mcp.Gateway.Tools.Schema;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class ToolSchemaGenerator
{
    public static string? TryGenerateForTool(MethodInfo method, ToolService.FunctionDetails toolDetails)
    {
        // Kun for TypedJsonRpc<T> + manglende InputSchema
        if (!toolDetails.FunctionArgumentType.IsTypedJsonRpc)
            return null;

        // Finn T fra TypedJsonRpc<T>
        var paramType = toolDetails.FunctionArgumentType.ParameterType;              // typeof(TypedJsonRpc<TParams>)
        var tParams = paramType.GetGenericArguments().FirstOrDefault();
        if (tParams == null)
            return null;

        return GenerateSchemaForTypePublic(tParams);
    }

    public static string GenerateSchemaForTypePublic(Type tParams)
    {
        return GenerateSchemaForType(tParams);
    }

    private static string GenerateSchemaForType(Type tParams)
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject()
        };
        var required = new JsonArray();

        var props = tParams.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var p in props)
        {            
            var (type, format) = MapClrTypeToJsonType(p.PropertyType);

            var propSchema = new JsonObject
            {
                ["type"] = type
            };

            if (format != null)
                propSchema["format"] = format;

            // Enum: legg til enum-verdier som strings
            var underlying = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            if (underlying.IsEnum)
            {
                var names = Enum.GetNames(underlying);
                var enumArray = new JsonArray(names.Select(n => (JsonNode)n).ToArray());
                propSchema["enum"] = enumArray;
            }

            // Description fra [Description]
            var descAttr = p.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            if (descAttr != null && !string.IsNullOrWhiteSpace(descAttr.Description))
            {
                propSchema["description"] = descAttr.Description;
            }

            // Navn fra JsonPropertyName eller property-navn
            var jsonName = p.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>()?.Name
                           ?? ToCamelCase(p.Name);

            // Required = non-nullable
            if (!IsNullable(p))
                required.Add(jsonName);

            ((JsonObject)root["properties"]!)[jsonName] = propSchema;
        }

        if (required.Count > 0)
            root["required"] = required;

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static (string type, string? format) MapClrTypeToJsonType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string))
            return ("string", null);
        if (underlying == typeof(Guid))
            return ("string", "uuid");
        if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset))
            return ("string", "date-time");
        if (underlying.IsEnum)
            return ("string", null);

        if (underlying == typeof(bool))
            return ("boolean", null);

        if (underlying == typeof(int) || underlying == typeof(long) ||
            underlying == typeof(short) || underlying == typeof(byte))
            return ("integer", null);

        if (underlying == typeof(float) || underlying == typeof(double) || underlying == typeof(decimal))
            return ("number", null);

        if (underlying.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(underlying) && underlying != typeof(string))
            return ("array", null);

        // Fallback – nested object
        return ("object", null);
    }

    private static bool IsNullable(PropertyInfo propertyInfo)
    {
        // value types: som før
        if (propertyInfo.PropertyType.IsValueType)
        {
            return Nullable.GetUnderlyingType(propertyInfo.PropertyType) != null;
        }

        // referansetyper: bruk NullabilityInfoContext
        var context = new NullabilityInfoContext();
        var nullability = context.Create(propertyInfo);

        return nullability.ReadState is NullabilityState.Nullable;
    }

    private static string ToCamelCase(string name)
        => string.IsNullOrEmpty(name)
            ? name
            : char.ToLowerInvariant(name[0]) + name[1..];
}