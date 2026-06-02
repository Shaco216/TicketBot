using OolamaCommunication.Models;
using static OllamaSharp.OllamaApiClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OolamaCommunication.Services;

public class TicketCategorizer : ITicketCategorizer
{
    OllamaService _ollamaService;
    public TicketCategorizer()
    {
        _ollamaService = new OllamaService(modelId);
    }
    private string BuildPrompt(string subject, string body, IEnumerable<string> categories)
    {
        var catList = string.Join(", ", categories);
        return $"Given the following email content, categorize it into one of these categories: {catList}. 
                + "If none of the categories fit, return create a new Category."
                + $"This is the Subject: {subject} and Body: {body}."
                + "Just answer with the Categoryname!";
    }

    public Task<Category> CategorizeAsync(string subject, string body, IEnumerable<string> categories)
    {
        string prompt = BuildPrompt(subject, body, categories);
        var modelId = configuration.GetValue<string>("Ollama:ModelId")
        OllamaService ollamaService = new OllamaService();
    }
}
