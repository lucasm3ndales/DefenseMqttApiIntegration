using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DefenseApiExample.Services;

public class TcpServerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    public TcpClient connectedClient = new();

    public TcpServerService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Configura o servidor TCP
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var address = _configuration["AppSettings:TcpAddress"];
        var port = _configuration["AppSettings:TcpPort"];

        var tcpListener = new TcpListener(IPAddress.Parse(address), int.Parse(port));
        tcpListener.Start();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var tcpClient = await tcpListener.AcceptTcpClientAsync(stoppingToken);
                Console.WriteLine("Cliente conectado.");

                connectedClient = tcpClient;
                _ = ProcessClientAsync(tcpClient, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no servidor: {ex.Message}");
        }
    }

    // Processa mensagens de clientes
    private static async Task ProcessClientAsync(TcpClient tcpClient, CancellationToken stoppingToken)
    {
        using (tcpClient)
        {
            var stream = tcpClient.GetStream();

            try
            {
                var bytes = new byte[256];
                var bytesRead = 0;
                while ((bytesRead = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    var data = Encoding.UTF8.GetString(bytes, 0, bytesRead);
                    Console.WriteLine("Mensagem recebida: " + data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error to listen messages: {ex.Message}");
            }
            finally
            {
                stream.Close();
                Console.WriteLine("Conexão com o cliente fechada.");
            }
        }
    }
}