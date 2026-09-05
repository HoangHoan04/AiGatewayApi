namespace AiGatewayApi.Application.Common.Interfaces;

/// <summary>
/// PBKDF2-SHA512 hashing cho internal API key.
/// Plaintext không bao gi? du?c luu — ch? luu hash d? verify.
/// </summary>
public interface IApiKeyHashService
{
    /// <summary>T?o hash PBKDF2 t? plaintext key.</summary>
    string HashKey(string plaintextKey);

    /// <summary>So sánh constant-time d? tránh timing attack.</summary>
    bool VerifyKey(string plaintextKey, string storedHash);

    /// <summary>Sinh random key d?ng "sk-gw-{prefix}-{random}".</summary>
    (string Plaintext, string Prefix) GenerateKey(string projectCode);
}
