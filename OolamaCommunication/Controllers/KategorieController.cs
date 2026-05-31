using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OolamaCommunication.Models;

namespace OolamaCommunication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KategorieController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetCategories()
        {
            var categories = _configuration.GetSection("TicketCategories").Get<string[]>()
                             ?? new[] { "Allgemein" };
            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CategorizeEmail([FromBody] Category kategorie)
        {
            if (email == null) return BadRequest("Payload fehlt.");

            var categories = _configuration.GetSection("TicketCategories").Get<string[]>()
                             ?? new[] { "Allgemein" };

            var category = await _categorizer.CategorizeAsync(email.Subject ?? string.Empty, email.Body ?? string.Empty, categories);

            return Ok(new { category });
        }
    }
}
