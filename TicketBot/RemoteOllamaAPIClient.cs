using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;

namespace TicketBot;

/// <summary>
/// Einfacher HTTP-Client für Ollama-kompatible APIs.
/// Versucht mehrere bekannte Endpunkt-Pfade (z. B. /v1/models, /models, /api/models).
/// Liefert Hilfsmethoden zum Abfragen installierter Modelle und zum Erstellen einer Text-Completion.
/// </summary>
internal sealed class RemoteOllamaApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public RemoteOllamaApiClient(string baseUrl, HttpClient? httpClient = null, TimeSpan? timeout = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentNullException(nameof(baseUrl));
        _baseUrl = baseUrl.TrimEnd('/');

        _http = httpClient ?? new HttpClient();
        if (timeout.HasValue) _http.Timeout = timeout.Value;
        else _http.Timeout = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Fragt die verfügbaren Modelle ab. Gibt ein leeres Array zurück, wenn keine Modelle ermittelt werden konnten.
    /// </summary>
    public async Task<string[]> GetModelsAsync(CancellationToken cancellationToken = default)
    {

        string url = $"{_baseUrl}/api/modells";
        try
        {
            using var resp = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
            using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (doc.RootElement.ValueKind == JsonValueKind.Array) return ExtractStringsFromArray(doc.RootElement);
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
        catch (OperationCanceledException) { throw; }
        catch
        {
            return Array.Empty<string>();
        }

    }


    /// <summary>
    /// Fordert eine Text-Completion an und gibt eine strukturierte QueryResponse zurück.
    /// </summary>
    public async Task<string> CreateCompletionAsync(string model, string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(model)) throw new ArgumentNullException(nameof(model));
        if (prompt is null) throw new ArgumentNullException(nameof(prompt));

        string url = $"{_baseUrl}/v1/complete";
        QueryRequest payload = new()
        {
            Model = model,
            Prompt = prompt
        };
        try
        {
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
            QueryResponse? queryresponse = await resp.Content.ReadFromJsonAsync<QueryResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            if(queryresponse == null)
            {
                return "Fehler: Leere Antwort vom Server";
            }
            else
            {
                return queryresponse.Response ?? "Fehler: Keine 'Response'-Eigenschaft in der Antwort";
            }
            
        }
        catch (Exception ex) { return ex.Message; }
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

    public void Dispose()
    {
        _http.Dispose();
    }
}
