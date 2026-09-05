namespace AiGatewayApi.Domain.Enums;

[Flags]
public enum AiModelCapability
{
    None = 0,
    Chat = 1,
    Vision = 2,
    Ocr = 4,
    Embedding = 8,
    JsonMode = 16,
    Tools = 32
}
