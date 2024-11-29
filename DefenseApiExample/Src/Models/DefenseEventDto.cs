using LiteDB;

namespace DefenseApiExample.Models;

public class DefenseEventDto
{
    public ObjectId Id { get; set; }
    public string JsonData { get; set; }
}