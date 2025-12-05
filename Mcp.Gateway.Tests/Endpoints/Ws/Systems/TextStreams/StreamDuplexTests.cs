namespace Mcp.Gateway.Tests.Endpoints.Ws.Systems.TextStreams;

using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;

[Collection("ServerCollection")]
public class StreamDuplexTests(McpGatewayFixture fixture)
{
    private readonly string ToolPath = "system.text.streams.duplex";
}
