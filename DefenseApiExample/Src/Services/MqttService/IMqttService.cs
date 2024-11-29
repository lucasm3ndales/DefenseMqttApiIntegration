using MQTTnet.Client;
using MQTTnet.Protocol;

public interface IMqttService
{
    Task<MqttClientConnectResult?> Connect(
        string url,
        int port,
        string username,
        string password,
        string? clientId = "",
        MqttQualityOfServiceLevel mqttQualityOfServiceLevel = default,
        TimeSpan keepAliveInterval = default);

    Task Disconnect();

    Task ListenMessages(Func<string, Task> onMessage);
    
    Task Subscribe(List<string> topics);
}