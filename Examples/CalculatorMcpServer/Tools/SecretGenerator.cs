namespace CalculatorMcpServer.Tools;

using Mcp.Gateway.Tools;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

public class SecretGenerator
{
    public enum SecretType
    {
        Guid,
        Hex,
        Base64
    }

    public sealed record SecretRequest(
        [property: JsonPropertyName("format")]
        [property: Description("Format of the secret.")] SecretType Format);

    public sealed record SecretResponse(
        [property: JsonPropertyName("secret")] string Secret,
        [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
        [property: JsonPropertyName("type")] SecretType Type);

    [McpTool("generate_secret",
        Title = "Generate Secret",
        Description = "Generates a random secret token. Use this when you need a unique, unpredictable value that cannot be calculated.")]
    public JsonRpcMessage GenerateSecretTool(TypedJsonRpc<SecretRequest> request)
    {
        // Get format parameter (default to guid)
        var reqParams = request.GetParams()
            ?? throw new ToolInvalidParamsException(
                "Parameter 'format' are required and must be one of enum values.");

        // Generate random secret
        string secret = reqParams.Format switch
        {
            SecretType.Hex => GenerateHexSecret(),
            SecretType.Base64 => GenerateBase64Secret(),
            _ => Guid.NewGuid().ToString("D") // Default: GUID
        };

        return ToolResponse.Success(
            request.Id,
            new SecretResponse(
                Secret: secret,
                Timestamp: DateTimeOffset.UtcNow,
                Type: reqParams.Format));
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
