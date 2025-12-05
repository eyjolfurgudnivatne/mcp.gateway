namespace Mcp.Gateway.Tests.Endpoints.Health;

using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;
using System;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

[Collection("ServerCollection")]
public class HealthTests(McpGatewayFixture fixture)
{
    public sealed record HealthResponse(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("machine")] string Machine);

    [Fact]
    public async Task HealthJson()
    {
        using var httpClient = fixture.Factory.CreateClient();
        httpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
        );

        var body = await httpClient.GetStringAsync("/health", fixture.CancellationToken);
        Assert.NotNull(body);

        var response = await httpClient.GetFromJsonAsync<HealthResponse>("/health", fixture.CancellationToken);
        Assert.NotNull(response);
        Assert.Equal("Healthy", response.Status);
        Assert.NotNull(response.Version);
        Assert.NotEmpty(response.Machine);
        Assert.True(response.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-1)); // Nylig timestamp
    }

    [Fact]
    public async Task HealthPlain()
    {
        using var httpClient = fixture.Factory.CreateClient();
        httpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/plain"));

        var response = await httpClient.GetAsync("/health", fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(fixture.CancellationToken);        
        Assert.Equal("Healthy", body);

        Assert.True(response.Headers.TryGetValues("X-App-Version", out var appVersion));
        Assert.NotNull(appVersion);
        Assert.Single(appVersion); // Kun én verdi
        Assert.NotEmpty(appVersion.First()); // Versjon er ikke tom
    }

    [Fact]
    public async Task Health_SetsNoCacheHeaders()
    {
        using var httpClient = fixture.Factory.CreateClient();

        var response = await httpClient.GetAsync("/health", fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.True(response.Headers.CacheControl?.NoCache);
        Assert.Equal("no-cache", response.Headers.Pragma.ToString());
    }
}
