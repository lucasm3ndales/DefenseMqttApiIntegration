using System.Text.Json.Serialization;

namespace SecuryIa.Entities.Integracoes.Shared.Integrations.Defense.Dtos;

public class DefenseSecondLoginResDto
{
    [JsonPropertyName("duration")]
    public int Duration { get; set; }
    [JsonPropertyName("token")]
    public string Token { get; set; }
    [JsonPropertyName("userId")]
    public string UserId { get; set; }
    [JsonPropertyName("serviceAbilty")]
    public object serviceAbilty { get; set; }
    [JsonPropertyName("versionInfo")]
    public DefenseVersionInfo DefenseVersionInfo { get; set; }
    [JsonPropertyName("emapUrl")]
    public object EmapUrl { get; set; }
    [JsonPropertyName("sipNum")]
    public string SipNum { get; set; }
    [JsonPropertyName("pocId")]
    public object PocId { get; set; }
    [JsonPropertyName("pocPassword")]
    public object PocPassword { get; set; }
    [JsonPropertyName("tokenRate")]
    public int TokenRate { get; set; }
    [JsonPropertyName("secretKey")]
    public string SecretKey { get; set; }
    [JsonPropertyName("secretVector")]
    public string SecretVector { get; set; }
    [JsonPropertyName("reused")]
    public string Reused { get; set; }
    [JsonPropertyName("userLevel")]
    public string UserLevel { get; set; }
}

public class DefenseVersionInfo
{
    [JsonPropertyName("lastVersion")]
    public string LastVersion { get; set; }
    [JsonPropertyName("updateUrl")]
    public string UpdateUrl { get; set; }
    [JsonPropertyName("patchVersion")]
    public string PatchVersion { get; set; }
    [JsonPropertyName("patchUrl")]
    public string PatchUrl { get; set; }
}