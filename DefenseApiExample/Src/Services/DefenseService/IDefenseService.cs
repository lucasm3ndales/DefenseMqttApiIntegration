using DefenseApiExample.Dtos;
using DefenseApiExample.Models;

namespace DefenseApiExample.Services;

public interface IDefenseService
{
    Task<DefenseCredentialsDto> SaveCredentials(DefenseCredentialsDto credentialsDto);
    Task<DefenseCredentialsDto> GetCredential();
    Task SaveEvent(DefenseEventDto dto);
    Task<IEnumerable<DefenseEventDto>> GetEvents();
}