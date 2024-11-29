using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DefenseApiExample.Dtos;
using DefenseApiExample.Models;
using DefenseApiExample.Utils;
using LiteDB;
using MQTTnet.Protocol;
using SecuryIa.Entities.Integracoes.Shared.Integrations.Defense.Dtos;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DefenseApiExample.Services;

public class DefenseHostedService(
    IMqttService mqttService,
    IServiceProvider provider,
    TcpServerService tcpServerService)
    : BackgroundService
{
    private const string MqMethod = "BRM.Config.GetMqConfig";
    private const string MqttMessageType = "queue"; // must be topic or queue
    private const int MqttPort = 1883; // Porta mqtt
    private static readonly int SuccessCode = 1000;
    private static string _mqTopicAlarm = "mq/alarm/msg/topic"; // tópico/fila par notificação de alarmes
    private static string _mqTopicEvent = "mq/event/msg/topic"; // tópico/fila par notificação de eventos
    private static string _mqTopicCommon = "mq.common.msg.topic"; // tópico/fila par notificação de visitantes
    private static string _signatureMd5Temp4 = string.Empty;
    private static string _tokenValue = string.Empty; // token de acesso
    private static int _heartCount; // heatCount do keeppAlive
    private Timer ExecuteTimer;
    private string PrivateKey { get; set; }
    private string SecretVectorRSA { get; set; }
    private string SecretKeyRSA { get; set; }
    private string LoginUserId { get; set; }

    // Método referente ao BackgroundService
    // Inicia a execução do login com o defense para o recebimento de eventos
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ExecuteTimer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(5)); // Executa a cada 5 minutos 

        return Task.CompletedTask;
    }

    private async void DoWork(object state)
    {
        try
        {
            Console.WriteLine("DefenseHostedService started.\n");
            var defenseService = provider.CreateScope().ServiceProvider.GetRequiredService<IDefenseService>();
            var credential = await defenseService.GetCredential(); // Busca as credenciais para o login no defense
            await Login(credential);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error to start DefenseHostedService: {ex.Message}");
        }
    }

    private (string path, string port) GetPathAndPort(string url)
    {
        var idx = url.LastIndexOf(':');
        var path = url.Substring(0, idx);
        var port = url.Substring(idx + 1);
        return (path, port);
    }

    // Configuração do MQTT para o recebimento de eventos
    private async Task GetMqttConfig(DefenseCredentialsDto credentialsDto)
    {
        try
        {
            var mqttPayload = new DefenseMqttConfigReqDto(
                credentialsDto.ClientType.ToString(),
                credentialsDto.ClientMac,
                "",
                MqMethod
            );

            // Endpoint para a configuração de conexão do MQTT para o recebimento de eventos
            var mqttConfigUrl = $"http://{credentialsDto.ServerIp}:80/brms/api/v1.0/BRM/Config/GetMqConfig";
            var httpRes = await SendHttpRequest(HttpMethod.Post, mqttConfigUrl, mqttPayload);

            var json = await DeserializeResponse<DefenseApiResDto<DefenseMqttConfigResDto>>(httpRes);

            if (json != null && json.Code == SuccessCode)
            {
                var privKeyBytes = Convert.FromBase64String(PrivateKey);

                var secretVector = DecryptWithRSA(SecretVectorRSA, privKeyBytes); // decriptação do secret vector

                var secretKey = DecryptWithRSA(SecretKeyRSA, privKeyBytes); // decriptação do secret key

                var userPassword = DecryptWithAES(json.Data!.Password, secretKey, secretVector); // senha decryptada para acesso ao broker MQTT

                _mqTopicAlarm = $"{_mqTopicAlarm}/{LoginUserId}"; // retorna apenas a notificação de alarmes
                _mqTopicEvent = $"{_mqTopicEvent}/{LoginUserId}"; // retorna diversos tipos de eventos

                // podem ser tópicos ou filas
                var messages = new List<string>()
                {
                    _mqTopicAlarm,
                    _mqTopicEvent,
                    _mqTopicCommon
                };

                // Começa a ouvir os eventos do broker MQTT
                await ListenMqttMessage(
                    json.Data.Address,
                    json.Data.Username,
                    userPassword,
                    messages,
                    MqttMessageType);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while fetching device types: {ex.Message}");
        }
    }

    private string DecryptWithAES(string text, string aesKey, string aesVector)
    {
        try
        {
            using var aes = Aes.Create();

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(aesKey);
            aes.IV = Encoding.UTF8.GetBytes(aesVector);

            using var decrypt = aes.CreateDecryptor(aes.Key, aes.IV);

            var encryptedBytes = Convert.FromHexString(text);

            using var ms = new MemoryStream(encryptedBytes);

            using var cs = new CryptoStream(ms, decrypt, CryptoStreamMode.Read);

            using var reader = new StreamReader(cs);

            var decryptedBytes = Encoding.UTF8.GetBytes(reader.ReadToEnd());

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during decryption. Reason: {ex.Message}");
            return string.Empty;
        }
    }

    private string DecryptWithRSA(string text, byte[] key)
    {
        try
        {
            var data = Convert.FromBase64String(text);

            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(key, out _);

            var blockSize = rsa.KeySize / 8;

            using var ms = new MemoryStream();

            for (var i = 0; i < data.Length; i += blockSize)
            {
                var to = Math.Min(i + blockSize, data.Length);
                var temp = rsa.Decrypt(data[i..to], RSAEncryptionPadding.Pkcs1);
                ms.Write(temp, 0, temp.Length);
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro durante a descriptografia. Motivo: {ex.Message}");
            return string.Empty;
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

    // Executa o primeiro e o segundo login
    public async Task Login(DefenseCredentialsDto credentialsDto)
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

            Console.WriteLine("Login efetuado com sucesso.\n");
            
            _tokenValue = secondRes!.Token;
            SecretVectorRSA = secondRes.SecretVector; // secretVector utilizado para a configuração do MQTT
            SecretKeyRSA = secondRes.SecretKey; // secretKey utilizado para a configurção do MQTT
            LoginUserId = secondRes.UserId; // userId utilizado para configurar o MQTT


            await GetMqttConfig(credentialsDto);
            await SendKeepAlive(credentialsDto); // Inicia o Envio do keepAlive para o defense
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in defense authentication: {ex.Message}");
        }
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

    // Começa a ouvir eventos MQTT
    private async Task ListenMqttMessage(
        string mqUrl,
        string username,
        string password,
        List<string> messages,
        string messageType)
    {
        if (messageType != "topic" && messageType != "queue")
        {
            return;
        }
        
        // Separa o endereço da porta retornados na configuração do MQTT
        var addr = GetPathAndPort(mqUrl);

        try
        {
            var connection =
                await mqttService.Connect(addr.path, MqttPort, username, password, "AlertAi2023",
                    MqttQualityOfServiceLevel.ExactlyOnce);

            if (connection != null && connection.ResultCode == 0)
            {
                await mqttService.Subscribe(messages); // inscreve-se nos tópicos ou filas
                await mqttService.ListenMessages(OnMessage); // começa a ouvir os eventos dos tópicos ou filas
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error to connect in the server: {ex.Message}");
        }
    }

    // Método para receber os eventos retornados do broker MQTT
    private async Task OnMessage(string payload)
    {
        try
        {
            Console.WriteLine($"Mensagem recebida do broker: {payload}\n");
            var defenseService = provider.CreateScope().ServiceProvider.GetRequiredService<IDefenseService>();

            var obj = JsonSerializer.Deserialize<JsonElement>(payload);
            var dto = new DefenseEventDto()
            {
                Id = ObjectId.NewObjectId(),
                JsonData = obj.GetRawText(),
            };

            await defenseService.SaveEvent(dto); // Salva os eventos no banco 
            await SendMessageAsync(obj.GetRawText()); //  Repassa os eventos via socket
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error to get message: {ex.Message}");
        }
    }
    
    private async Task SendMessageAsync(string message)
    {
        var client = tcpServerService.connectedClient;
        if (client.Connected)
        {
            // Escreve as mensagens dos eventos para os clientes conectados
            await client.GetStream().WriteAsync(Encoding.UTF8.GetBytes(message));
            Console.WriteLine("Mensagem enviada com sucesso.\n");
        }
    }
    
    // Método referente ao BackgroundService
    public override Task StopAsync(CancellationToken stoppingToken)
    {
        ExecuteTimer.Change(Timeout.Infinite, 0);
        return base.StopAsync(stoppingToken);
    }

    // Método referente ao BackgroundService
    public override void Dispose()
    {
        ExecuteTimer.Dispose();
        base.Dispose();
    }
}