using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
//using TicketBot; // <- Ersetzen Sie 'TicketBot' durch den tatsächlichen Namespace von OllamaService, falls nötig

namespace OolamaCommunication;

public static class OllamaServiceExtensions
{
    public static IServiceCollection AddOllamaServiceFromConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var modelId = configuration.GetValue<string>("Ollama:ModelId") ?? "llama3";
        services.AddSingleton<OllamaService>(_ => new OllamaService(modelId));
        return services;
    }
}