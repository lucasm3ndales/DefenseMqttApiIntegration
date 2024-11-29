using System.Text.Json.Serialization;

namespace SecuryIa.Entities.Integracoes.Shared.Integrations.Defense.Dtos;

public class DefenseTokenResDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; }
    [JsonPropertyName("duration")]
    public int Duration { get; set; }
}