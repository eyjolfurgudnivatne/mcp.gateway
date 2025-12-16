namespace PaginationMcpServerTests.Fixture;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

public class PaginationMcpServerFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient HttpClient { get; private set; } = null!;
    public CancellationToken CancellationToken = TestContext.Current.CancellationToken;

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
