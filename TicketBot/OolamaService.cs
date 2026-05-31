using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.Tasks;

public class OllamaService
{
    private readonly Kernel _kernel;

    public OllamaService(string modelId)
    {
        // Erstellen Sie den Kernel und verbinden Sie ihn mit Ihrem lokalen Ollama-Server
        var builder = Kernel.CreateBuilder();
        builder.AddOllamaChatCompletion(
            modelId: modelId, // oder Ihr gewünschtes Modell
            endpoint: new Uri("http://localhost:11434")
        );

        _kernel = builder.Build();
    }

    public OllamaService()
    {
        // Erstellen Sie den Kernel und verbinden Sie ihn mit Ihrem lokalen Ollama-Server
        var builder = Kernel.CreateBuilder();
        builder.AddOllamaChatCompletion(
            modelId: "llama3", // oder Ihr gewünschtes Modell
            endpoint: new Uri("http://localhost:11434")
        );

        _kernel = builder.Build();
    }

    public async Task<string> GetResponseAsync(string prompt)
    {
        var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        var result = await chatCompletionService.GetChatMessageContentAsync(prompt);

        return result.ToString();
    }
}