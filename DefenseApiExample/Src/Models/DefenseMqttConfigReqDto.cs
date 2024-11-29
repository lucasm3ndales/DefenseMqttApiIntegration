using System.Text.Json.Serialization;

namespace SecuryIa.Entities.Integracoes.Shared.Integrations.Defense.Dtos;

public class DefenseMqttConfigReqDto(string clientType, string mac, string clientPushId, string method, string? project = "PSDK")
{
    [JsonPropertyName("clientType")]
    public string ClientType { get; set; } = clientType;
    [JsonPropertyName("clientMac")]
    public string ClientMac { get; set; } = mac;
    [JsonPropertyName("clientPushId")]
    public string ClientPushId { get; set; } = clientPushId;
    [JsonPropertyName("project")]
    public string Project { get; set; } = project;
    [JsonPropertyName("method")]
    public string Method { get; set; } = method;
}