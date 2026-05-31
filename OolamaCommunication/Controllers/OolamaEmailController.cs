using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OolamaCommunication.Models;
using OolamaCommunication.Services;
using System.Threading.Tasks;

namespace OolamaCommunication.Controllers;

[Route("api/OolamaEmail")]
[ApiController]
public class OolamaEmailController : ControllerBase
{
    private readonly ITicketCategorizer _categorizer;
    private readonly IConfiguration _configuration;

    public OolamaEmailController(ITicketCategorizer categorizer, IConfiguration configuration)
    {
        _categorizer = categorizer;
        _configuration = configuration;
    }

    // POST api/OolamaEmail/receive
    [HttpPost("receive")]
    public async Task<IActionResult> ReceivedEmailToCreateTicket([FromBody] ReceivedEmailDto dto)
    {
        if (dto == null) return BadRequest("Payload missing.");

        // Kategorien aus appsettings.json (alternativ: aus DB/Repository laden)
        var categories = _configuration.GetSection("TicketCategories").Get<string[]>()
                         ?? new[] { "Billing", "Technical", "Sales", "General" };

        var category = await _categorizer.CategorizeAsync(dto.Subject ?? string.Empty, dto.Body ?? string.Empty, categories);

        // Hier: Ticket erstellen / speichern etc. -> aktuell nur Rückgabe der Kategorie
        return Ok(new { category });
    }
}
