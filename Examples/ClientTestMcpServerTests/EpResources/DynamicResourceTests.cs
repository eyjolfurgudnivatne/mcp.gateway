namespace ClientTestMcpServerTests.EpResources;

using ClientTestMcpServer.Models;
using ClientTestMcpServerTests.Fixture;
using Mcp.Gateway.Client;
using System;
using System.Collections.Generic;
using System.Text;

[Collection("ServerCollection")]
public class DynamicResourceTests(ClientTestMcpServerFixture fixture)
{
    [Fact]
    public async Task Add_Resource()
    {
        // 1. Opprett transport (her HTTP)
        await using var transport = new HttpMcpTransport(fixture.HttpClient);
        Assert.NotNull(transport);

        // 2. Opprett klient
        await using var client = new McpClient(transport);
        Assert.NotNull(client);

        // 3. Koble til (utfører handshake)
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // 4. Legg til ressurs
        var addResourceResponse = await client.CallToolAsync<DynamicResourceResponse>("add-test-resource", null, ct: TestContext.Current.CancellationToken);
        Assert.NotNull(addResourceResponse);
        Assert.Equal("All resources added.", addResourceResponse.Message);

        // 5. List resources
        var resources = await client.ListResourcesAsync(ct: TestContext.Current.CancellationToken);
        Assert.NotNull(resources);
        Assert.Contains(resources.Resources, r => r.Name == "Test Resource 1");

        // 6. Get resource
        var resource = await client.ReadResourceAsync("dynamic://test/resource1", TestContext.Current.CancellationToken);
        Assert.NotNull(resource);
        Assert.NotNull(resource.Contents);
        Assert.NotNull(resource.Contents[0]);
        Assert.NotNull(resource.Contents[0].Text);

        // 7. Ta bort ressurs
        var removeResourceResponse = await client.CallToolAsync<DynamicResourceResponse>("remove-test-resource", null, ct: TestContext.Current.CancellationToken);
        Assert.NotNull(removeResourceResponse);
        Assert.Equal("Resource removed.", removeResourceResponse.Message);

        // 8. List resources
        var resources2 = await client.ListResourcesAsync(ct: TestContext.Current.CancellationToken);
        Assert.NotNull(resources2);
        Assert.DoesNotContain(resources2.Resources, r => r.Name == "Test Resource 1");
    }
}
