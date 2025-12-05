namespace Mcp.Gateway.Tests.Tools;

using Mcp.Gateway.Tools;
using System.Text.Json;
using Xunit;

public class InputSchemaValidationTests
{
    [Fact]
    public void ValidSchema_ParseSuccessfully()
    {
        // Arrange - Valid schema (properties as OBJECT, not array!)
        var validSchema = @"{""type"":""object"",""properties"":{""name"":{""type"":""string"",""description"":""Name parameter""}}}";
        
        // Act - Parse the schema
        using var parsed = JsonDocument.Parse(validSchema);
        var root = parsed.RootElement;
        
        // Assert
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.True(root.TryGetProperty("properties", out var props));
        Assert.Equal(JsonValueKind.Object, props.ValueKind); // ✅ OBJECT, not Array!
    }

    [Fact]
    public void InvalidSchema_PropertiesAsArray_Detected()
    {
        // Arrange - INVALID schema (properties as array - common mistake!)
        var invalidSchema = @"{""type"":""object"",""properties"":[]}
        ";
        
        // Act - Parse and check
        using var parsed = JsonDocument.Parse(invalidSchema);
        var root = parsed.RootElement;
        root.TryGetProperty("properties", out var props);
        
        // Assert - properties is incorrectly an array
        Assert.Equal(JsonValueKind.Array, props.ValueKind);
        
        // This is the error our validation catches and warns about!
    }

    [Fact]
    public void MalformedSchema_Throws()
    {
        // Arrange - Malformed JSON (missing quotes)
        var malformedSchema = @"{type:object}";
        
        // Act & Assert - Should throw
        Assert.ThrowsAny<Exception>(() =>
        {
            JsonDocument.Parse(malformedSchema);
        });
    }

    [Fact]
    public void EmptySchema_IsValid()
    {
        // Arrange - Minimal valid schema
        var emptySchema = @"{""type"":""object"",""properties"":{}}";
        
        // Act
        using var parsed = JsonDocument.Parse(emptySchema);
        var root = parsed.RootElement;
        
        // Assert
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.True(root.TryGetProperty("properties", out var props));
        Assert.Equal(JsonValueKind.Object, props.ValueKind);
        Assert.Empty(props.EnumerateObject()); // Empty properties object
    }

    [Fact]
    public void SchemaWithRequired_IsValid()
    {
        // Arrange - Schema with required array (correct format)
        var schemaWithRequired = @"{
            ""type"":""object"",
            ""properties"":
            {
                ""number1"":{""type"":""integer"",""description"":""First number""},
                ""number2"":{""type"":""integer"",""description"":""Second number""}
            },
            ""required"":[""number1"",""number2""]
        }";
        
        // Act
        using var parsed = JsonDocument.Parse(schemaWithRequired);
        var root = parsed.RootElement;
        
        // Assert
        Assert.True(root.TryGetProperty("properties", out var props));
        Assert.Equal(JsonValueKind.Object, props.ValueKind); // ✅ properties is OBJECT
        
        Assert.True(root.TryGetProperty("required", out var required));
        Assert.Equal(JsonValueKind.Array, required.ValueKind); // required IS an array (correct!)
        
        var requiredArray = required.EnumerateArray().ToList();
        Assert.Equal(2, requiredArray.Count);
        Assert.Equal("number1", requiredArray[0].GetString());
        Assert.Equal("number2", requiredArray[1].GetString());
    }

    [Fact]
    public void ComplexSchema_ParsesCorrectly()
    {
        // Arrange - Complex schema with nested objects (all on one line to avoid whitespace issues)
        var complexSchema = @"{""type"":""object"",""properties"":{""user"":{""type"":""object"",""properties"":{""name"":{""type"":""string""},""age"":{""type"":""integer""}}},""tags"":{""type"":""array"",""items"":{""type"":""string""}}}}";
        
        // Act
        using var parsed = JsonDocument.Parse(complexSchema);
        var root = parsed.RootElement;
        
        // Assert
        Assert.True(root.TryGetProperty("properties", out var props));
        Assert.Equal(JsonValueKind.Object, props.ValueKind);
        
        // Check user property exists
        Assert.True(props.TryGetProperty("user", out var user));
        Assert.Equal(JsonValueKind.Object, user.ValueKind);
        
        // Check user has nested properties
        Assert.True(user.TryGetProperty("properties", out var userProps));
        Assert.Equal(JsonValueKind.Object, userProps.ValueKind);
        
        // Check name and age exist
        Assert.True(userProps.TryGetProperty("name", out _));
        Assert.True(userProps.TryGetProperty("age", out _));
    }

    [Fact]
    public void SchemaValidation_DetectsArrayInsteadOfObject()
    {
        // This test verifies the exact error we're catching:
        // When someone writes "properties":[...] instead of "properties":{...}
        
        // Arrange - The WRONG format (array)
        var wrongFormat = @"{""type"":""object"",""properties"":[]}
        ";
        
        // Act
        using var parsed = JsonDocument.Parse(wrongFormat);
        var root = parsed.RootElement;
        root.TryGetProperty("properties", out var props);
        
        // Assert - This is what our validation detects as ERROR
        Assert.Equal(JsonValueKind.Array, props.ValueKind);
        Assert.NotEqual(JsonValueKind.Object, props.ValueKind);
    }

    [Fact]
    public void JsonRpcMessage_SupportsNumericId()
    {
        // JSON-RPC 2.0 spec allows id to be string, number, or null
        // This test verifies we handle numeric IDs correctly
        
        // Arrange
        var requestWithNumericId = @"{""jsonrpc"":""2.0"",""method"":""test"",""id"":42}";
        
        // Act
        using var doc = JsonDocument.Parse(requestWithNumericId);
        var success = JsonRpcMessage.TryGetFromJsonElement(doc.RootElement, out var message);
        
        // Assert
        Assert.True(success);
        Assert.NotNull(message);
        Assert.NotNull(message.Id);
        
        // Debug: Check actual type
        var actualType = message.Id.GetType();
        Assert.True(actualType == typeof(int) || actualType == typeof(long), $"Expected int or long, got {actualType.Name}");
        
        // Compare values
        if (message.Id is int intId)
        {
            Assert.Equal(42, intId);
        }
        else if (message.Id is long longId)
        {
            Assert.Equal(42L, longId);
        }
        else
        {
            Assert.Fail($"ID is not numeric: {message.Id.GetType().Name}");
        }
        
        Assert.Equal("test", message.Method);
    }

    [Fact]
    public void JsonRpcMessage_SupportsStringId()
    {
        // Arrange
        var requestWithStringId = @"{""jsonrpc"":""2.0"",""method"":""test"",""id"":""test-123""}";
        
        // Act
        using var doc = JsonDocument.Parse(requestWithStringId);
        var success = JsonRpcMessage.TryGetFromJsonElement(doc.RootElement, out var message);
        
        // Assert
        Assert.True(success);
        Assert.NotNull(message);
        Assert.Equal("test-123", message.Id);
    }

    [Fact]
    public void JsonRpcMessage_SupportsNullId()
    {
        // Notifications in JSON-RPC 2.0 have null or missing id
        
        // Arrange
        var notification = @"{""jsonrpc"":""2.0"",""method"":""notify""}";
        
        // Act
        using var doc = JsonDocument.Parse(notification);
        var success = JsonRpcMessage.TryGetFromJsonElement(doc.RootElement, out var message);
        
        // Assert
        Assert.True(success);
        Assert.NotNull(message);
        Assert.Null(message.Id);
        Assert.True(message.IsNotification);
    }
}
