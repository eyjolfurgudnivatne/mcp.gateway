namespace ClientTestMcpServerTests.Fixture;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;

public class ClientTestMcpServerFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient HttpClient { get; private set; } = null!;
    public CancellationToken CancellationToken = TestContext.Current.CancellationToken;

    public Process? ServerProcess;

    public async Task<WebSocket> CreateWebSocketClientAsync(string path = "/ws")
    {
        var server = Factory.Server;
        var client = server.CreateWebSocketClient();
        var uri = new Uri($"ws://localhost{path}");
        var socket = await client.ConnectAsync(uri, TestContext.Current.CancellationToken);
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
        HttpClient.DefaultRequestHeaders.Add("MCP-Protocol-Version", "2025-11-25");
        HttpClient.Timeout = TimeSpan.FromSeconds(30);

        // Find the path to the server project
        // Assuming we are in bin/Debug/net10.0 and the project is in Examples/ClientTestMcpServer
        var currentDir = AppContext.BaseDirectory;
        var projectDir = Path.GetFullPath(Path.Combine(currentDir, "../../../../ClientTestMcpServer"));
        var projectFile = Path.Combine(projectDir, "ClientTestMcpServer.csproj");

        if (!File.Exists(projectFile))
        {
            // Fallback for different directory structures (e.g. CI/CD)
            // Try to find it relative to the solution root if possible, or just assume it's in the path
            // For now, let's log if we can't find it, but try to run anyway
            Console.WriteLine($"Warning: Could not find project file at {projectFile}");
        }

        ServerProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{projectFile}\" -- --stdio",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true, // Capture error output
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = projectDir // Set working directory to project dir
            }
        };
        
        // Add error logging
        ServerProcess.ErrorDataReceived += (sender, e) => 
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine($"Server Stderr: {e.Data}");
        };

        ServerProcess.Start();
        ServerProcess.BeginErrorReadLine();


        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        ServerProcess?.Dispose();
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
