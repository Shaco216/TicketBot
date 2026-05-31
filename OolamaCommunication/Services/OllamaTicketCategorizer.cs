namespace OolamaCommunication.Services;

public class OllamaTicketCategorizer : ITicketCategorizer
{
    private readonly OllamaService? _ollamaService;

    public OllamaTicketCategorizer(OllamaService? ollamaService = null)
    {
        _ollamaService = ollamaService;
    }

    public async Task<string> CategorizeAsync(string subject, string body, IEnumerable<string> categories)
    {
        var list = categories?.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList()
                   ?? new List<string> { "Unbekannt" };

        // 1) Wenn Ollama vorhanden: versuche LLM -> exakten Kategorienamen zurückgeben
        if (_ollamaService != null)
        {
            var prompt = $"Du bist ein Ticket-Kategorisierer. Verfügbare Kategorien: {string.Join(", ", list)}\n\n" +
                         $"E-Mail-Betreff: {subject}\n\nE-Mail-Inhalt: {body}\n\n" +
                         "Antworte nur mit exakt einem Kategorienamen aus der Liste, ohne zusätzliche Erklärungen.";
            try
            {
                var resp = await _ollamaService.GetResponseAsync(prompt);
                if (!string.IsNullOrWhiteSpace(resp))
                {
                    var first = resp.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim().Trim('"');
                    var match = list.FirstOrDefault(c => string.Equals(c, first, StringComparison.OrdinalIgnoreCase));
                    if (match != null) return match;
                }
            }
            catch
            {
                // LLM-Ausfall: Fallthrough zu Fallback-Logik
            }
        }

        // 2) Keyword-Fallback: simple Heuristik auf Basis von Vorkommen
        var text = (subject + " " + body).ToLowerInvariant();
        foreach (var cat in list)
        {
            var key = cat.ToLowerInvariant();
            if (text.Contains(key)) return cat;
        }

        // 3) Best-effort: match nach Wortüberschneidung
        var words = text.Split(new[] { ' ', '\r', '\n', '\t', '.', ',', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
        string best = list.First();
        int bestScore = -1;
        foreach (var cat in list)
        {
            var score = cat.ToLowerInvariant().Split(' ').Count(w => words.Contains(w));
            if (score > bestScore) { bestScore = score; best = cat; }
        }

        return best;
    }
}