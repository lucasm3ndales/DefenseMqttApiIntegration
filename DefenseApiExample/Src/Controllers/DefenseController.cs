using DefenseApiExample.Dtos;
using DefenseApiExample.Services;
using Microsoft.AspNetCore.Mvc;

namespace DefenseApiExample.Controllers;

[Route("api/defense")]
[ApiController]
public class DefenseController(IDefenseService defenseService) : ControllerBase
{


    /// <summary>
    ///  Salva as credenciais de acesso ao Defense.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> SaveCredentials([FromBody] DefenseCredentialsDto credentialsDto)
    {
        var response = await defenseService.SaveCredentials(credentialsDto);
        return Ok(response);
    }
    
    /// <summary>
    ///  Busca pela credencial de acesso do Defense. 
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetCredentials()
    {
        var response = await defenseService.GetCredential();
        return Ok(response);
    }
    
    /// <summary>
    ///  Busca por eventos do defense salvos no banco.
    /// </summary>
    [HttpGet("events")]
    public async Task<ActionResult> GetEvents()
    {
        var response = await defenseService.GetEvents();
        return Ok(response);
    }
}