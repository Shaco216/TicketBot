using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OolamaCommunication.Models;
using OolamaCommunication.Repositories;
using OolamaCommunication.Services;
using System;
using System.Threading.Tasks;

namespace OolamaCommunication.Controllers;

[Route("api/OllamaEmail")]
[ApiController]
public class OllamaEmailController : ControllerBase
{
    private readonly IOllamaTicketCategorizer _categorizer;
    private readonly IConfiguration _configuration;
    private readonly IOllamaEmailDtoRepository _emailRepo;

    public OllamaEmailController(IOllamaTicketCategorizer categorizer, IConfiguration configuration, IOllamaEmailDtoRepository emailRepo)
    {
        _categorizer = categorizer;
        _configuration = configuration;
        _emailRepo = emailRepo;
    }

    // POST api/OllamaEmail/receive
    // Speichert die empfangene Email und gibt die zugeordnete Kategorie zurück
    [HttpPost("receive")]
    public async Task<IActionResult> ReceivedEmailToCreateTicket([FromBody] ReceivedEmailDto dto)
    {
        if (dto == null) return BadRequest("Payload missing.");
        string subject = dto.Subject;
        string body = dto.Body;
        // Persistieren
        Guid emailId = new Guid(); // Generiere eine neue ID für die Email
        bool saved = await _emailRepo.InsertAsync(emailId, dto.From, dto.To, subject, body);

        Category category = await _categorizer.CategorizeAsync(emailId, subject, body);
        ReceivedEmailDto? emailSaved = await _emailRepo.GetByIdAsync(emailId);
        if (emailSaved == null || saved == false)
        {
            return StatusCode(500, "Failed to save email.");
        }
        else if (category == null)
        {
            return StatusCode(500, "Failed to categorize email.");
        }
        else
        {
            // Rückgabe: gespeicherte Email + Kategorie
            return Ok(new { dto, category });
        }
    }

    // GET api/OllamaEmail
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var emails = await _emailRepo.GetAllAsync();
        return Ok(emails);
    }

    // GET api/OllamaEmail/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var email = await _emailRepo.GetByIdAsync(id);
        if (email == null) return NotFound();
        return Ok(email);
    }

    // DELETE api/OllamaEmail/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _emailRepo.DeleteAsync(id);
        return NoContent();
    }
}
