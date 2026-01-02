namespace Mcp.Gateway.Tools.Schema;

using System;
using System.Collections.Generic;
using System.Reflection;

internal static class PromptSchemaGenerator
{
    public static List<PromptArgument> TryGenerateForPrompt(MethodInfo method, ToolService.FunctionDetails promptDetails)
    {
        // Kun for TypedJsonRpc<T> + manglende InputSchema
        if (!promptDetails.FunctionArgumentType.IsTypedJsonRpc)
            return [];

        // Finn T fra TypedJsonRpc<T>
        var paramType = promptDetails.FunctionArgumentType.ParameterType;   // typeof(TypedJsonRpc<TParams>)
        var tParams = paramType.GetGenericArguments().FirstOrDefault();
        if (tParams == null)
            return [];

        return GenerateSchemaForType(tParams);
    }

    private static List<PromptArgument> GenerateSchemaForType(Type tParams)
    {
        List<PromptArgument> result = [];

        var props = tParams.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var p in props)
        {
            // Description fra [Description]
            string? description = null;
            var descAttr = p.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            if (descAttr != null && !string.IsNullOrWhiteSpace(descAttr.Description))
            {
                description = descAttr.Description;
            }

            // Title fra [DisplayName]
            string? title = null;
            var titleAttr = p.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>();
            if (titleAttr != null && !string.IsNullOrWhiteSpace(titleAttr.DisplayName))
            {
                title = titleAttr.DisplayName;
            }

            // Navn fra JsonPropertyName eller property-navn
            var jsonName = p.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>()?.Name
                           ?? ToCamelCase(p.Name);

            // Required = non-nullable
            bool required = false;
            if (!IsNullable(p))
                required = true;

            result.Add(new PromptArgument
            {
                Name = jsonName,
                Title = title,
                Description = description,
                Required = required
            });
        }

        return result;
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
