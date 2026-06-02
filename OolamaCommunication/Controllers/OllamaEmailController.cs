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
    private readonly ITicketCategorizer _categorizer;
    private readonly IConfiguration _configuration;
    private readonly IOllamaEmailDtoRepository _emailRepo;

    public OllamaEmailController(ITicketCategorizer categorizer, IConfiguration configuration, IOllamaEmailDtoRepository emailRepo)
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

        // Persistieren
        var saved = await _emailRepo.CreateAsync(dto);

        // Kategorien aus appsettings.json (alternativ: aus DB/Repository laden)
        var categories = _configuration.GetSection("TicketCategories").Get<string[]>()
                         ?? new[] { "Billing", "Technical", "Sales", "General" };

        var category = await _categorizer.CategorizeAsync(saved.Subject ?? string.Empty, saved.Body ?? string.Empty, categories);

        // Rückgabe: gespeicherte Email + Kategorie
        return Ok(new { email = saved, category });
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
