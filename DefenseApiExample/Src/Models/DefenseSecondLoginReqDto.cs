using System.Text.Json.Serialization;

namespace SecuryIa.Entities.Integracoes.Shared.Integrations.Defense.Dtos;

public class DefenseSecondLoginReqDto(string signature, string username, string randomKey, string pubKey, string? encryptType = "MD5", string? userType = "0", string? mac = "", string clientType = "WINPC_V2", string ipAddress = "")
{
    [JsonPropertyName("mac")]
    public string? Mac { get; set; } = mac;
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = signature;
    [JsonPropertyName("userName")]
    public string Username { get; set; } = username;
    [JsonPropertyName("randomKey")]
    public string RandomKey { get; set; } = randomKey;
    [JsonPropertyName("publicKey")]
    public string PublicKey { get; set; } = pubKey;
    [JsonPropertyName("encryptType")]
    public string EncryptType { get; set; } = encryptType;
    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = ipAddress;
    [JsonPropertyName("clientType")]
    public string ClientType { get; set; } = clientType;
    [JsonPropertyName("userType")]
    public string UserType { get; set; } = userType;

}