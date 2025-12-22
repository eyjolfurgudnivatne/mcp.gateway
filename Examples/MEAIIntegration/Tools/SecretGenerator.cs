namespace MEAIIntegration.Tools;

using Mcp.Gateway.Tools;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

public class SecretGenerator
{
    public enum SecretFormat
    {
        Guid,
        Hex,
        Base64
    }

    public sealed record SecretRequest(
        [property: JsonPropertyName("format")]
        [property: Description("Format of the secret. Options: 'guid' (default), 'hex', 'base64'")] SecretFormat? Format);

    public sealed record SecretResponse(
        [property: JsonPropertyName("secret")] string Secret,
        [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
        [property: JsonPropertyName("type")] string Type);

    [McpTool("generate_secret",
        Title = "Generate Secret",
        Description = "Generates a random secret token (GUID). Use this when you need a unique, unpredictable value that cannot be calculated.")]
    public JsonRpcMessage GenerateSecretTool(TypedJsonRpc<SecretRequest> message)
    {
        // Get format parameter (default to guid)
        var tellRequest = message.GetParams();
        string format = tellRequest?.Format?.ToString() ?? "guid";

        // Generate random secret
        string secret = format.ToLowerInvariant() switch
        {
            "hex" => GenerateHexSecret(),
            "base64" => GenerateBase64Secret(),
            _ => Guid.NewGuid().ToString("D") // Default: GUID
        };

        return ToolResponse.Success(
            message.Id,
            new SecretResponse(
                Secret: secret,
                Timestamp: DateTimeOffset.UtcNow,
                Type: format));
    }

    private static string GenerateHexSecret()
    {
        byte[] bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateBase64Secret()
    {
        byte[] bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
