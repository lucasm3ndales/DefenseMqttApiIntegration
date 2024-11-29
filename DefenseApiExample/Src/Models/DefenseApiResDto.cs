using System.Text.Json.Serialization;

namespace SecuryIa.Entities.Integracoes.Shared.Integrations.Defense.Dtos;

public class DefenseApiResDto<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    [JsonPropertyName("desc")]
    public string Description { get; set; }
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

