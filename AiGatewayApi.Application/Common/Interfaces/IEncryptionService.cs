namespace AiGatewayApi.Application.Common.Interfaces;

/// <summary>
/// AES-256-GCM encryption for provider API keys at rest. Nonce is random per message.
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
