using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

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

    public static async Task<string[]> QueryOllamaInstalledModelsAsync()
    {

        //"http://localhost:11434/models",
        //    "http://localhost:11434/api/models"
        //var endpoints = new[]
        //{
        //};
        string url = "http://localhost:11434/v1/models";
        HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

        //foreach (var url in endpoints)
        //{
        try
        {
            Debug.WriteLine($"Versuche Ollama-Endpunkt: {url}");
            var resp = await _http.GetAsync(url);

            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                var body = await resp.Content.ReadAsStringAsync();
                Debug.WriteLine($"404 von {url}. Body: {body}");
                //continue;
            }

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                Debug.WriteLine($"Fehler von {url}: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
                throw new HttpRequestException($"Ollama antwortete mit {(int)resp.StatusCode} {resp.ReasonPhrase} für {url}");
            }

            using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                return ExtractStringsFromArray(doc.RootElement);
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("models", out var modelsEl) && modelsEl.ValueKind == JsonValueKind.Array)
                    return ExtractStringsFromArray(modelsEl);

                if (doc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                    return ExtractStringsFromArray(dataEl);

                var single = TryExtractStringFromObject(doc.RootElement);
                if (!string.IsNullOrEmpty(single)) return new[] { single };
            }

            return Array.Empty<string>();
        }
        catch (HttpRequestException hre)
        {
            Debug.WriteLine($"HTTP-RequestException beim Aufruf von {url}: {hre}");
            throw;
        }
        catch (TaskCanceledException tce)
        {
            Debug.WriteLine($"Timeout beim Aufruf von {url}: {tce}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Allgemeiner Fehler bei {url}: {ex}");
            throw;
        }
        //}

        //Debug.WriteLine("Alle bekannten Endpunkte gaben 404 zurück; benutze DefaultCandidates.");
        //return Array.Empty<string>();
    }

    private static string[] ExtractStringsFromArray(JsonElement arrayEl)
    {
        var list = arrayEl.EnumerateArray()
            .Select(el =>
            {
                if (el.ValueKind == JsonValueKind.Object)
                {
                    var s = TryExtractStringFromObject(el);
                    return s ?? string.Empty;
                }
                if (el.ValueKind == JsonValueKind.String) return el.GetString() ?? string.Empty;
                return string.Empty;
            })
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();

        return list;
    }

    private static string? TryExtractStringFromObject(JsonElement obj)
    {
        if (obj.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String) return idProp.GetString();
        if (obj.TryGetProperty("model", out var mProp) && mProp.ValueKind == JsonValueKind.String) return mProp.GetString();
        if (obj.TryGetProperty("name", out var nProp) && nProp.ValueKind == JsonValueKind.String) return nProp.GetString();

        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.String) return prop.Value.GetString();
        }

        return null;
    }
}