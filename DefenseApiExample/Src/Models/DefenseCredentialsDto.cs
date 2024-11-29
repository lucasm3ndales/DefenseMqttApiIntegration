using LiteDB;

namespace DefenseApiExample.Dtos;

public class DefenseCredentialsDto
{
    public ObjectId? Id { get; set; }
    public string Username { get; set; } // usuário do defense
    public string Password { get; set; } //  senha do defense
    public string ClientMac { get; set; } // MAC do cliente
    public string ClientIp { get; set; } // Ip do cliente
    public string ServerIp { get; set; } // ip o servidor do defense
    public string ClientType { get; set; } // pode ser WINPC ou WINPC_V2
    public string UserType { get; set; } // pode ser 0  ou 1, System user: 0, Domain user: 1
}
