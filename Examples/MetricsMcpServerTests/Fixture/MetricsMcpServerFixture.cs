namespace MetricsMcpServerTests.Fixture;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;

public class MetricsMcpServerFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient HttpClient { get; private set; } = null!;
    public CancellationToken CancellationToken = TestContext.Current.CancellationToken;

    public async Task<WebSocket> CreateWebSocketClientAsync(string path = "/ws")
    {
        var server = Factory.Server;
        var client = server.CreateWebSocketClient();
        var uri = new Uri($"ws://localhost{path}");
        var socket = await client.ConnectAsync(uri, CancellationToken.None);
        return socket;
    }

    public async Task InitializeAsync()
    {
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Development");

                // Configure logging to output to console
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug);
                });
            });

        HttpClient = Factory.CreateClient();

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        Factory.Dispose();
        await Task.CompletedTask;
    }

    async ValueTask IAsyncLifetime.InitializeAsync() => await InitializeAsync();
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
