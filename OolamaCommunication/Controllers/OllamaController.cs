using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            return Ok("Ollama API is running.");
        }
    }
}
