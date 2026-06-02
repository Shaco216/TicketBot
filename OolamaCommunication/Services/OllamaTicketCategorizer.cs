using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//using TicketBot; // Namespace anpassen wenn nötig

namespace OolamaCommunication.Services;

public class OllamaTicketCategorizer : IOllamaTicketCategorizer
{
    private readonly OllamaService? _ollama;

    public OllamaTicketCategorizer(OllamaService? ollama = null)
    {
        _ollama = ollama;
    }

    public async Task<string> CategorizeAsync(string subject, string body, IEnumerable<string> categories)
    {
        var list = categories?.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList()
                   ?? new List<string> { "Unbekannt" };

        if (_ollama != null)
        {
            var prompt = $"Verfügbare Kategorien: {string.Join(", ", list)}\n\nBetreff: {subject}\n\nInhalt: {body}\n\n" +
                         "Antworte nur mit genau einer Kategorie aus der Liste.";
            try
            {
                var resp = await _ollama.GetResponseAsync(prompt);
                if (!string.IsNullOrWhiteSpace(resp))
                {
                    var first = resp.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim().Trim('"');
                    var match = list.FirstOrDefault(c => string.Equals(c, first, StringComparison.OrdinalIgnoreCase));
                    if (match != null) return match;
                }
            }
            catch
            {
                // Fallback weiter unten
            }
        }

        // Fallback: Keyword- und Heuristik-basierte Einordnung
        var text = (subject + " " + body).ToLowerInvariant();
        foreach (var cat in list)
        {
            var key = cat.ToLowerInvariant();
            if (text.Contains(key)) return cat;
        }

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