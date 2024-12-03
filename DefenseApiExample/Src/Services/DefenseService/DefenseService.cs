using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using DefenseApiExample.Dtos;
using DefenseApiExample.Models;
using DefenseApiExample.Utils;
using LiteDB;
using SecuryIa.Entities.Integracoes.Shared.Integrations.Defense.Dtos;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DefenseApiExample.Services;

public class DefenseService(LiteDatabase liteDatabase) : IDefenseService
{
    private static string _signatureMd5Temp4 = string.Empty;
    private static string _tokenValue = string.Empty; // token de acesso
    private static int _heartCount; // heatCount do keeppAlive
    private static readonly int SuccessCode = 1000;
    private string PrivateKey { get; set; }
    private string SecretVectorRSA { get; set; }
    private string SecretKeyRSA { get; set; }
    private string LoginUserId { get; set; }
    
    
    // Método para salvar as credencias de integração com o defense
    public async Task<DefenseCredentialsDto> SaveCredentials(DefenseCredentialsDto credentialsDto)
    {
        try
        {
            Console.WriteLine("defense hosted");

            liteDatabase.GetCollection("credentials").DeleteAll();
            
            liteDatabase.GetCollection<DefenseCredentialsDto>("credentials").Insert(credentialsDto);
            
            return credentialsDto;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error to save defense credentials. Reason: {ex.Message}");
        }
    }
    
    // Método para buscar as credencias de integração do defense
    public async Task<DefenseCredentialsDto> GetCredential()
    {
        try
        {
            var response = liteDatabase.GetCollection<DefenseCredentialsDto>("credentials").FindAll().FirstOrDefault();
            if(response != null) return response;
            return null;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error to save defense credentials. Reason: {ex.Message}");
        }
    }
    
    // Método para salvar as eventos recebidos do broker mqtt
    public async Task SaveEvent(DefenseEventDto dto)
    {
        try
        {
            liteDatabase.GetCollection<DefenseEventDto>("events").Insert(dto);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error to save defense event. Reason: {ex.Message}");
        }
    }
    
    // Método para buscar os eventos recebidos do broker mqtt
    public async Task<IEnumerable<DefenseEventDto>> GetEvents()
    {
        try
        {
            return liteDatabase.GetCollection<DefenseEventDto>("events").FindAll();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error to get defense events. Reason: {ex.Message}");
        }
    }
    
    
    // Método para efetuar logina antes de fazer outras requisi
    private async Task DoLogin()
    {
        try
        {
            var credentials = await GetCredential();
            await Login(credentials);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error to login. Reason: {ex.Message}");
        }
    }
    
    // Executa o primeiro e o segundo login
    private async Task Login(DefenseCredentialsDto credentialsDto)
    {
        try
        {
            var firstLogin =
                new DefenseFirstLoginReqDto(credentialsDto.Username, credentialsDto.ClientType.ToString(),
                    credentialsDto.ClientIp);

            // Endpoint primeiro Login
            var authUrl = $"http://{credentialsDto.ServerIp}:80/brms/api/v1.0/accounts/authorize";
            var firstResStr = await SendHttpRequest(HttpMethod.Post, authUrl, firstLogin);

            var firstRes = await DeserializeResponse<DefenseFirstLoginResDto>(firstResStr);

            if (firstRes == null) return;

            var signature = GenerateSignature(credentialsDto.Username, credentialsDto.Password, firstRes.Realm,
                firstRes.RandomKey);

            var keyPair = GenerateRsaKeys();
            PrivateKey = Convert.ToBase64String(keyPair.privKeyPKCS8);
            var publicKey = Convert.ToBase64String(keyPair.pubKeyInfo);

            var userType = credentialsDto.UserType;

            var secondLogin = new DefenseSecondLoginReqDto(
                signature,
                credentialsDto.Username,
                firstRes.RandomKey,
                publicKey,
                "MD5",
                userType,
                credentialsDto.ClientMac,
                credentialsDto.ClientType,
                credentialsDto.ClientIp
            );

            // Endpoint para efetuar o segundo Login
            var secondResStr = await SendHttpRequest(HttpMethod.Post, authUrl, secondLogin);

            var secondRes = await DeserializeResponse<DefenseSecondLoginResDto>(secondResStr);

            Console.WriteLine("Login efetuado com sucesso.");
            
            _tokenValue = secondRes!.Token;
            SecretVectorRSA = secondRes.SecretVector; // secretVector utilizado para a configuração do MQTT
            SecretKeyRSA = secondRes.SecretKey; // secretKey utilizado para a configurção do MQTT
            LoginUserId = secondRes.UserId; // userId utilizado para configurar o MQTT

            _ = Task.Run(async () =>
            {
                await SendKeepAlive(credentialsDto); // Inicia o Envio do keepAlive para o defense
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in defense authentication: {ex.Message}");
        }
    }
    
    private (byte[] privKey, byte[] pubKey, byte[] privKeyPKCS8, byte[] pubKeyInfo) GenerateRsaKeys(int? size = 2048)
    {
        if (size == null) throw new ArgumentException($"Key size musn't be null.");
        using var rsa = RSA.Create();
        var privKey = rsa.ExportRSAPrivateKey();
        var pubKey = rsa.ExportRSAPublicKey();
        var privKeyPKCS8 = rsa.ExportPkcs8PrivateKey();
        var pubKeyInfo = rsa.ExportSubjectPublicKeyInfo();
        return (privKey, pubKey, privKeyPKCS8, pubKeyInfo);
    }
    
    //  Executa o envio do keepAlive a cada 22 segundos
    private async Task SendKeepAlive(DefenseCredentialsDto credentialsDto)
    {
        try
        {
            while (true)
            {
                await Task.Delay(22000);

                var keepAlive = new Dictionary<string, string>
                {
                    { "token", _tokenValue }
                };

                // Endpoint para envio do keepAlive
                var keepAliveUrl = $"http://{credentialsDto.ServerIp}:80/brms/api/v1.0/accounts/keepalive";
                await SendHttpRequest(HttpMethod.Put, keepAliveUrl, keepAlive);

                //var heartbeat = await DeserializeResponse<Dictionary<string, string>>(heartbeatStr);

                _heartCount++;

                if (_heartCount % 60 != 0) await UpdateToken(credentialsDto); // Quando o contador chegar a 60, atualiza o token de acesso
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error to send keep-alive: {ex.Message}");
        }
    }
    
    // Atuasliza o token de acesso a api do defense
    private async Task UpdateToken(DefenseCredentialsDto credentialsDto)
    {
        try
        {
            _heartCount = 0;

            var signature = DefenseUtils.EncryptWithMD5($"{_signatureMd5Temp4}:{_tokenValue}");

            var updateTokenParams = new Dictionary<string, string>
            {
                { "token", _tokenValue },
                { "signature", signature }
            };

            // Endpoint para o atualizar o token
            var updateTokenUrl = $"http://{credentialsDto.ServerIp}:80/brms/api/v1.0/accounts/updateToken";
            var updateTokenRes = await SendHttpRequest(HttpMethod.Post, updateTokenUrl, updateTokenParams);

            var tokenRes = await DeserializeResponse<DefenseApiResDto<DefenseTokenResDto>>(updateTokenRes);

            if (tokenRes != null && tokenRes.Code == SuccessCode && tokenRes.Data != null)
            {
                _tokenValue = tokenRes.Data!.Token; // atualiza o token
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error to update auth token: {ex.Message}");
        }
    }
    
    // Gera a assinatura para efetuar o segundo login
    private string GenerateSignature(string userName, string password, string realm, string randomKey)
    {
        var temp1 = DefenseUtils.EncryptWithMD5(password);
        var temp2 = DefenseUtils.EncryptWithMD5(userName + temp1);
        var temp3 = DefenseUtils.EncryptWithMD5(temp2);
        var temp4 = DefenseUtils.EncryptWithMD5($"{userName}:{realm}:{temp3}");

        _signatureMd5Temp4 = temp4;

        return DefenseUtils.EncryptWithMD5($"{temp4}:{randomKey}");
    }
    
    private async Task<T?> DeserializeResponse<T>(string raw)
    {
        try
        {
            var json = JsonSerializer.Deserialize<T>(raw);

            if (json == null) return default;

            return json;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error to deserialize defense response. Reason: {ex.Message}");
            return default;
        }
    }
    
    // Método auxiliar para enviar as requests ao defense
    private async Task<string> SendHttpRequest<T>(HttpMethod method, string url, T? payload,
        HttpHeaders? headers = null)
    {
        using var client = new HttpClient();

        var request = new HttpRequestMessage(method, url);

        if (!string.IsNullOrEmpty(_tokenValue))
        {
            request.Headers.TryAddWithoutValidation("X-Subject-Token", _tokenValue);
        }

        if (headers != null)
        {
            foreach (var h in headers)
            {
                if (!request.Headers.TryAddWithoutValidation(h.Key, h.Value))
                {
                    request.Headers.TryAddWithoutValidation(h.Key, h.Value);
                }
            }
        }

        if (payload != null)
        {
            var objJson = JsonSerializer.Serialize(payload);

            request.Content = new StringContent(objJson, Encoding.UTF8, "application/json");
        }

        var res = await client.SendAsync(request);

        return await res.Content.ReadAsStringAsync();
    }
    
}