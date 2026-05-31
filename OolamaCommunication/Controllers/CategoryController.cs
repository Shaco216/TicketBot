using Microsoft.AspNetCore.Mvc;
using OolamaCommunication.Models;
using OolamaCommunication.Repositories;
using OolamaCommunication.Services;
using System.Linq;
using System.Threading.Tasks;

namespace OolamaCommunication.Controllers;

[Route("api/categories")]
[ApiController]
public class CategoryController : ControllerBase
{
    private readonly ICategoryRepository _repo;
    private readonly ITicketCategorizer _categorizer;

    public CategoryController(ICategoryRepository repo, ITicketCategorizer categorizer)
    {
        _repo = repo;
        _categorizer = categorizer;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _repo.GetAllAsync();
        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var cat = await _repo.GetByIdAsync(id);
        if (cat == null) return NotFound();
        return Ok(cat);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name)) return BadRequest("Name required.");
        var created = await _repo.CreateAsync(category);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Category category)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();
        existing.Name = category.Name;
        existing.Description = category.Description;
        await _repo.UpdateAsync(existing);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _repo.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("categorize")]
    public async Task<IActionResult> CategorizeEmail([FromBody] ReceivedEmailDto email)
    {
        if (email == null) return BadRequest("Payload fehlt.");

        var categories = (await _repo.GetAllAsync()).Select(c => c.Name).Where(n => !string.IsNullOrWhiteSpace(n));
        var category = await _categorizer.CategorizeAsync(email.Subject ?? string.Empty, email.Body ?? string.Empty, categories);
        return Ok(new { category });
    }
}