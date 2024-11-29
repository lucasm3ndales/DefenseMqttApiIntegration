using System.Text.Json;
using System.Text.Json.Serialization;

namespace SecuryIa.Entities.Eventos.Shared.DefenseEvent.Dtos;

public class DefenseMqMessageDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("method")]
    public string Method { get; set; }
    [JsonPropertyName("info")]
    public JsonElement Info { get; set; }

}

