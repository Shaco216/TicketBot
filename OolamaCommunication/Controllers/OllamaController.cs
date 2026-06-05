using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OolamaCommunication.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OolamaCommunication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OllamaController : ControllerBase
    {
        public OllamaController()
        {
        }

        [HttpGet("modells")]
        public async Task<IActionResult> Get()
        {
            IEnumerable<string> modells = await OllamaService.QueryOllamaInstalledModelsAsync();
            return Ok(modells);
        }

        [HttpPost("query")]
        public async Task<IActionResult> Query([FromBody] QueryRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Model) || string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { error = "Model and prompt are required." });
            }

            // Optional: Validierungen für Temperature/MaxTokens etc. hinzufügen

            string response = await OllamaService.QueryOllamaAsync(request.Model, request.Prompt);

            var result = new QueryResponse
            {
                Model = request.Model,
                Prompt = request.Prompt,
                Response = response
            };

            return Ok(result);
        }
    }
}
