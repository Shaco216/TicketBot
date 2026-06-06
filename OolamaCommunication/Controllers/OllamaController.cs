using Microsoft.AspNetCore.Mvc;
using OolamaCommunication.Models;

namespace OolamaCommunication.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OllamaController : ControllerBase
{
    string _model = string.Empty;
    OllamaService _ollamaService = new OllamaService("llama3");

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
        if ( _model != request.Model)
        {
            _model = request.Model;
            _ollamaService = new OllamaService(_model);
        }
        string response = await _ollamaService.GetResponseAsync(request.Prompt);

        QueryResponse result = new()
        {
            Model = request.Model,
            Prompt = request.Prompt,
            Response = response
        };

        return Ok(result);
    }
}
