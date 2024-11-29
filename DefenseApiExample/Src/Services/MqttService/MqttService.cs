using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace DefenseApiExample.Services;

public class MqttService : IMqttService
{
    private IMqttClient _client;
    private MqttClientTlsOptions _tlsOptions;


    public MqttService()
    {
        _client = new MqttFactory().CreateMqttClient();
        _tlsOptions = new MqttClientTlsOptions()
        {
            UseTls = true,
            CertificateValidationHandler = (args) => true,
            IgnoreCertificateChainErrors = true,
            IgnoreCertificateRevocationErrors = true,
            AllowUntrustedCertificates = true
        };
    }

    public async Task<MqttClientConnectResult?> Connect(
        string url,
        int port,
        string username,
        string password,
        string? clientId = "",
        MqttQualityOfServiceLevel mqttQualityOfServiceLevel = default,
        TimeSpan keepAliveInterval = default)
    {
        var options = new MqttClientOptionsBuilder()
            .WithClientId(clientId)
            .WithTcpServer(url, port)
            .WithCredentials(username, password)
            .WithWillQualityOfServiceLevel(mqttQualityOfServiceLevel)
            .WithTlsOptions(_tlsOptions)
            .WithKeepAlivePeriod(keepAliveInterval)
            .Build();
        
        if (_client.IsConnected) return null;
        
        return await _client.ConnectAsync(options);
    }

    public async Task ListenMessages(Func<string, Task> onMessage)
    {
        _client.ApplicationMessageReceivedAsync += async (render) =>
        {
            var message = Encoding.UTF8.GetString(render.ApplicationMessage.PayloadSegment);

            await onMessage(message);
        };
    }

    public async Task Disconnect()
    {
        await _client.DisconnectAsync();
    }

    public async Task Subscribe(List<string> topics)
    {
        if (_client.IsConnected && topics.Count > 0)
        {
            foreach (var t in topics)
            {
                var topic = new MqttTopicFilterBuilder()
                    .WithTopic(t)
                    .Build();

                await _client.SubscribeAsync(topic);
            }
        }
    }
}