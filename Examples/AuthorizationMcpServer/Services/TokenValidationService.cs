namespace AuthorizationMcpServer.Services;

/// <summary>
/// Simple in-memory token validation service for demo purposes.
/// In production, use proper JWT validation with Microsoft.AspNetCore.Authentication.JwtBearer.
/// </summary>
public interface ITokenValidationService
{
    Task<TokenValidationResult> ValidateTokenAsync(string token);
}

public record TokenValidationResult(
    bool IsValid,
    string? UserId,
    List<string> Roles,
    string? ErrorMessage,
    bool IsExpired = false);

public class SimpleTokenValidationService : ITokenValidationService
{
    // Demo tokens (in production: use JWT with proper validation!)
    private readonly Dictionary<string, (string UserId, List<string> Roles)> _validTokens = new()
    {
        ["admin-token-123"] = ("admin-user", new List<string> { "Admin" }),
        ["user-token-456"] = ("regular-user", new List<string> { "User" }),
        ["manager-token-789"] = ("manager-user", new List<string> { "Manager", "User" }),
        ["public-token-000"] = ("public-user", new List<string>())
    };
    
    public Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(new TokenValidationResult(
                IsValid: false,
                UserId: null,
                Roles: new List<string>(),
                ErrorMessage: "Token is null or empty"));
        }
        
        if (_validTokens.TryGetValue(token, out var userInfo))
        {
            return Task.FromResult(new TokenValidationResult(
                IsValid: true,
                UserId: userInfo.UserId,
                Roles: userInfo.Roles,
                ErrorMessage: null));
        }
        
        return Task.FromResult(new TokenValidationResult(
            IsValid: false,
            UserId: null,
            Roles: new List<string>(),
            ErrorMessage: "Invalid or expired token"));
    }
}
