using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OolamaCommunication.Repositories;
using OolamaCommunication.Models;
//using TicketBot; // Namespace anpassen wenn nötig

namespace OolamaCommunication.Services;

public class OllamaTicketCategorizer : IOllamaTicketCategorizer
{
    private readonly OllamaService? _ollama;
    private readonly ICategoryRepository _categoryRepo;

    public OllamaTicketCategorizer(ICategoryRepository categoryRepo, OllamaService? ollama = null)
    {
        _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
        _ollama = ollama;
    }

    public async Task<Category> CategorizeAsync(Guid emailId, string subject, string body)
    {
        IEnumerable<Category> categories = await _categoryRepo.GetAllAsync();
        List<Category> list = categories?.Where(categor => !string.IsNullOrWhiteSpace(categor.Name)).ToList() ?? new List<Category>();

        if (_ollama != null)
        {
            var prompt = $"Verfügbare Kategorien: {string.Join(", ", list.Select(categories=>categories.Name))}\n\nBetreff: {subject}\n\nInhalt: {body}\n\n" +
                         "Antworte nur mit genau einer Kategorie aus der Liste, oder, falls keine passt, mit einem kurzen neuen Kategorienamen.";
            try
            {
                var resp = await _ollama.GetResponseAsync(prompt);
                if (!string.IsNullOrWhiteSpace(resp))
                {
                    var first = resp.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim().Trim('"');
                    if (!string.IsNullOrWhiteSpace(first))
                    {
                        // Prüfen ob bereits vorhandene Kategorie
                        var match = list.FirstOrDefault(c => string.Equals(c.Name, first, StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                        {
                            return match;
                        }

                        // Keine Übereinstimmung: neue Kategorie anlegen
                        // Normalisieren / Kürzen
                        string newCatName = first;
                        if (newCatName.Length > 200) newCatName = newCatName.Substring(0, 200).Trim();

                        // Double-check gegen DB (Groß-/Kleinschreibung)
                        Category? existingFromDb = categories?.FirstOrDefault(n => string.Equals(n.Name, newCatName, StringComparison.OrdinalIgnoreCase));
                        if (existingFromDb != null)
                        {
                            return existingFromDb;
                        }

                        // Create new category in DB
                        Category created = await _categoryRepo.CreateAsync(new Category { Name = newCatName, Description = null });
                        return created;
                    }
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
            var key = cat.Name.ToLowerInvariant();
            if (text.Contains(key)) return cat;
        }

        var words = text.Split(new[] { ' ', '\r', '\n', '\t', '.', ',', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
        Category best = list.First();
        int bestScore = -1;
        foreach (var cat in list)
        {
            var score = cat.Name.ToLowerInvariant().Split(' ').Count(w => words.Contains(w));
            if (score > bestScore) { bestScore = score; best = cat; }
        }

        return best;
    }
}