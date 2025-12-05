namespace Mcp.Gateway.Tests.Unit;

using Mcp.Gateway.Tools;
using Xunit;

public class ToolNameGeneratorTests
{
    [Theory]
    [InlineData("AddNumbers", "add_numbers")]
    [InlineData("AddNumbersTool", "add_numbers_tool")]
    [InlineData("GetUserById", "get_user_by_id")]
    [InlineData("Ping", "ping")]
    [InlineData("add_numbers", "add_numbers")]  // Already snake_case
    [InlineData("get_user_by_id", "get_user_by_id")]  // Already snake_case
    [InlineData("PING", "p_i_n_g")]  // All caps
    [InlineData("HTTPRequest", "h_t_t_p_request")]  // Acronym
    public void ToSnakeCase_ValidInput_ReturnsSnakeCase(string input, string expected)
    {
        // Act
        var result = ToolNameGenerator.ToSnakeCase(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void ToSnakeCase_EmptyOrNull_ReturnsInput(string? input, string? expected)
    {
        // Act
        var result = ToolNameGenerator.ToSnakeCase(input!);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("add_numbers_tool", "Add Numbers Tool")]
    [InlineData("get_user_by_id", "Get User By Id")]
    [InlineData("ping", "Ping")]
    [InlineData("add-numbers", "Add Numbers")]  // Hyphen
    [InlineData("get_user-by_id", "Get User By Id")]  // Mixed
    public void ToHumanizedTitle_ValidInput_ReturnsHumanized(string input, string expected)
    {
        // Act
        var result = ToolNameGenerator.ToHumanizedTitle(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void ToHumanizedTitle_EmptyOrNull_ReturnsInput(string? input, string? expected)
    {
        // Act
        var result = ToolNameGenerator.ToHumanizedTitle(input!);

        // Assert
        Assert.Equal(expected, result);
    }
}
