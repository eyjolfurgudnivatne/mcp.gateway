namespace Mcp.Gateway.Tests.Unit;

using Mcp.Gateway.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;
using Xunit;

/// <summary>
/// Unit tests for McpProtocolVersionMiddleware (v1.7.0)
/// </summary>
public class McpProtocolVersionMiddlewareTests
{
    [Fact]
    public async Task McpEndpoint_WithSupportedVersion_Passes()
    {
        // Arrange
        using var host = await CreateTestHost();
        var client = host.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/mcp");
        request.Headers.Add("MCP-Protocol-Version", "2025-11-25");

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task McpEndpoint_WithOlderSupportedVersion_Passes()
    {
        // Arrange
        using var host = await CreateTestHost();
        var client = host.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/mcp");
        request.Headers.Add("MCP-Protocol-Version", "2025-06-18");

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task McpEndpoint_WithUnsupportedVersion_Returns400()
    {
        // Arrange
        using var host = await CreateTestHost();
        var client = host.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/mcp");
        request.Headers.Add("MCP-Protocol-Version", "2024-01-01");

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task McpEndpoint_WithUnsupportedVersion_ReturnsErrorDetails()
    {
        // Arrange
        using var host = await CreateTestHost();
        var client = host.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/mcp");
        request.Headers.Add("MCP-Protocol-Version", "2024-01-01");

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("Unsupported protocol version", content);
        Assert.Contains("2024-01-01", content);
        Assert.Contains("2025-11-25", content);
        Assert.Contains("2025-06-18", content);
    }

    [Fact]
    public async Task McpEndpoint_WithMissingVersion_DefaultsTo202503()
    {
        // Arrange
        using var host = await CreateTestHost();
        var client = host.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/mcp");
        // No MCP-Protocol-Version header

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task NonMcpEndpoint_SkipsValidation()
    {
        // Arrange
        using var host = await CreateTestHost();
        var client = host.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/other");
        request.Headers.Add("MCP-Protocol-Version", "invalid-version");

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task McpEndpoint_EmptyVersionHeader_DefaultsTo202503()
    {
        // Arrange
        using var host = await CreateTestHost();
        var client = host.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/mcp");
        request.Headers.Add("MCP-Protocol-Version", "");

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task McpEndpoint_StoresVersionInHttpContext()
    {
        // Arrange
        string? capturedVersion = null;
        using var host = await CreateTestHost(ctx =>
        {
            capturedVersion = ctx.Items["MCP-Protocol-Version"] as string;
        });
        var client = host.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/mcp");
        request.Headers.Add("MCP-Protocol-Version", "2025-11-25");

        // Act
        await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("2025-11-25", capturedVersion);
    }

    [Fact]
    public async Task McpEndpoint_CaseInsensitiveHeaderName()
    {
        // Arrange
        using var host = await CreateTestHost();
        var client = host.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/mcp");
        request.Headers.Add("mcp-protocol-version", "2025-11-25"); // lowercase

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Helper method to create test host
    private static async Task<IHost> CreateTestHost(Action<HttpContext>? endpointAction = null)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    // Empty services - middleware doesn't need DI
                });
                webHost.Configure(app =>
                {
                    app.UseProtocolVersionValidation();

                    app.Map("/mcp", builder =>
                    {
                        builder.Run(async context =>
                        {
                            endpointAction?.Invoke(context);
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync("OK");
                        });
                    });

                    app.Map("/other", builder =>
                    {
                        builder.Run(async context =>
                        {
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync("OK");
                        });
                    });
                });
            });

        var host = await hostBuilder.StartAsync();
        return host;
    }
}
