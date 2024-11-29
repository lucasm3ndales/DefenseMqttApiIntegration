using System.Text.Json.Serialization;

namespace SecuryIa.Entities.Integracoes.Shared.Integrations.Defense.Dtos;

public class DefenseFirstLoginReqDto(string username, string? clientType = "WINPC_V2", string? ipAddress = "")
{
    [JsonPropertyName("userName")]
    public string Username { get; set; } = username;
    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; } = ipAddress;
    [JsonPropertyName("clientType")]
    public string ClientType { get; set; } = clientType;
}