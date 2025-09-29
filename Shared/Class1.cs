namespace Shared;

public static class ErrorCodes
{
    public const string InvalidDocument = "INVALID_DOCUMENT";
    public const string UserUnauthorized = "USER_UNAUTHORIZED";
    public const string InvalidAccount = "INVALID_ACCOUNT";
    public const string InactiveAccount = "INACTIVE_ACCOUNT";
    public const string InvalidValue = "INVALID_VALUE";
    public const string InvalidType = "INVALID_TYPE";
}

public sealed class JwtSettings
{
    public string Issuer { get; set; } = "bankmore";
    public string Audience { get; set; } = "bankmore-clients";
    public string SecretKey { get; set; } = "dev_secret_change_me_please_123456789";
    public int ExpirationMinutes { get; set; } = 60;
}

public static class PasswordHasher
{
    public static string Hash(string password, string salt)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password + ":" + salt);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    public static bool Verify(string password, string salt, string hash)
    {
        return string.Equals(Hash(password, salt), hash, StringComparison.OrdinalIgnoreCase);
    }
}

public record JwtTokenResponse(string Token);

public record ErrorResponse(string Type, string Message);

public enum MovementType
{
    Credit = 1,
    Debit = 2
}

