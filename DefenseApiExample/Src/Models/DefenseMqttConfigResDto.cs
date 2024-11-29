using System.Text.Json.Serialization;

namespace SecuryIa.Entities.Integracoes.Shared.Integrations.Defense.Dtos;

public class DefenseMqttConfigResDto
{
    [JsonPropertyName("enableTls")]
    public string EnableTls { get; set; }
    [JsonPropertyName("userName")]
    public string  Username { get; set; }
    [JsonPropertyName("addr")]
    public string Address { get; set; } // Usar porta 1883
    [JsonPropertyName("mqtt")]
    public string Mqtt { get; set; }
    [JsonPropertyName("password")]
    public string Password { get; set; }
}