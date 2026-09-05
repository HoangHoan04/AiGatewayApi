using System.Security.Cryptography;
using System.Text;
using AiGatewayApi.Application.Common.Interfaces;

namespace AiGatewayApi.Infrastructure.Security;

public class ApiKeyHashService : IApiKeyHashService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public (string Plaintext, string Prefix) GenerateKey(string projectCode)
    {
        var cleanCode = string.IsNullOrWhiteSpace(projectCode)
            ? "core"
            : projectCode.Trim().ToLowerInvariant().Replace(" ", "-");
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var plaintext = $"sk-gw-{cleanCode}-{secret}";
        var prefix = $"sk-gw-{cleanCode}";
        return (plaintext, prefix);
    }

    public string HashKey(string plaintextKey)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(plaintextKey),
            salt,
            Iterations,
            HashAlgorithmName.SHA512,
            HashSize);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyKey(string plaintextKey, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(plaintextKey) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expectedHash = Convert.FromBase64String(parts[1]);

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(plaintextKey),
                salt,
                Iterations,
                HashAlgorithmName.SHA512,
                HashSize);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }
}
