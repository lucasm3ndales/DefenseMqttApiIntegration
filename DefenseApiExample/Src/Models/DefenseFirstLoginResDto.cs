using System.Text.Json.Serialization;

namespace SecuryIa.Entities.Integracoes.Shared.Integrations.Defense.Dtos;

public class DefenseFirstLoginResDto
{
    [JsonPropertyName("realm")]
    public string Realm { get; set; }
    [JsonPropertyName("randomKey")]
    public string RandomKey { get; set; }
    [JsonPropertyName("encryptType")]
    public string EncryptType { get; set; }
    [JsonPropertyName("publickey")]
    public string PublicKey { get; set; }
}