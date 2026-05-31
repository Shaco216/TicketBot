using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;

namespace TicketBot
{
    /// <summary>
    /// Interaktionslogik für ModelSelectionWindow.xaml
    /// </summary>
    public partial class ModelSelectionWindow : Window
    {
        private readonly ObservableCollection<ModelItem> _models = new();
        private static readonly string[] DefaultCandidates = new[] { "llama3", "llama2", "gpt-4o", "falcon", "llama_cpp" };
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

        public ModelSelectionWindow()
        {
            InitializeComponent();

            LstModels.ItemsSource = _models;

            Loaded += async (_, __) => await LoadInstalledModelsAsync();
        }

        private void SetStatus(string text, Brush color)
        {
            Dispatcher.Invoke(() =>
            {
                TxtStatus.Text = text;
                TxtStatus.Foreground = color;
            });
        }

        private async Task LoadInstalledModelsAsync()
        {
            LstModels.IsEnabled = false;
            try
            {
                SetStatus("Verbindung zu Ollama prüfen...", Brushes.Gray);
                var installed = await QueryOllamaInstalledModelsAsync();
                Debug.WriteLine("Ollama installed models: " + string.Join(", ", installed));

                _models.Clear();

                var candidates = installed.Length > 0
                    ? installed.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                    : DefaultCandidates;

                foreach (var id in candidates)
                {
                    bool isInstalled = installed.Any(s => !string.IsNullOrEmpty(s) && s.IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0);
                    _models.Add(new ModelItem { Id = id, Installed = isInstalled });
                }

                if (installed.Length > 0)
                {
                    SetStatus($"Ollama läuft — {installed.Length} Modell(e) gefunden", Brushes.Green);
                }
                else
                {
                    SetStatus("Ollama erreichbar, aber keine installierten Modelle gefunden (zeige Default-Kandidaten)", Brushes.Orange);
                }

                if (_models.Count == 0)
                {
                    foreach (var id in DefaultCandidates)
                    {
                        _models.Add(new ModelItem { Id = id, Installed = false });
                    }
                }

                LstModels.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fehler beim Laden installierter Modelle: " + ex);
                SetStatus("Ollama nicht erreichbar: " + ex.Message, Brushes.Red);

                _models.Clear();
                foreach (var id in DefaultCandidates)
                {
                    _models.Add(new ModelItem { Id = id, Installed = false });
                }

                LstModels.SelectedIndex = 0;
            }
            finally
            {
                LstModels.IsEnabled = true;
            }
        }

        private async Task<string[]> QueryOllamaInstalledModelsAsync()
        {
            // Liste bekannter/vernünftiger Endpunkte ausprobieren
            var endpoints = new[]
            {
                "http://localhost:11434/models",
                "http://localhost:11434/v1/models",
                "http://localhost:11434/api/models"
            };

            foreach (var url in endpoints)
            {
                try
                {
                    Debug.WriteLine($"Versuche Ollama-Endpunkt: {url}");
                    var resp = await _http.GetAsync(url);

                    // 404: probiere den nächsten Endpunkt
                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        var body = await resp.Content.ReadAsStringAsync();
                        Debug.WriteLine($"404 von {url}. Body: {body}");
                        continue;
                    }

                    // andere Fehler: protokollieren und weiterwerfen, damit LoadInstalledModelsAsync den Fehler anzeigt
                    if (!resp.IsSuccessStatusCode)
                    {
                        var body = await resp.Content.ReadAsStringAsync();
                        Debug.WriteLine($"Fehler von {url}: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
                        throw new HttpRequestException($"Ollama antwortete mit {(int)resp.StatusCode} {resp.ReasonPhrase} für {url}");
                    }

                    // Erfolg: parse und zurückgeben
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
                    // bei Netzwerkfehlern abbrechen, da es kein 404 ist (z. B. Verbindung, Timeout)
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
            }

            // Alle Endpunkte lieferten 404 -> leeres Ergebnis (LoadInstalledModelsAsync zeigt Fallback und Status)
            Debug.WriteLine("Alle bekannten Endpunkte gaben 404 zurück; benutze DefaultCandidates.");
            return Array.Empty<string>();
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

        private void BtnUseCustom_Click(object sender, RoutedEventArgs e)
        {
            var custom = TxtCustomModel.Text?.Trim();
            if (string.IsNullOrEmpty(custom)) return;

            var existing = _models.FirstOrDefault(m => string.Equals(m.Id, custom, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                _models.Insert(0, new ModelItem { Id = custom, Installed = false });
                LstModels.SelectedIndex = 0;
            }
            else
            {
                LstModels.SelectedItem = existing;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (LstModels.SelectedItem is not ModelItem mi) return;

            Application.Current.Properties["SelectedModel"] = mi.Id;

            var main = new MainWindow();
            main.Show();
            Close();
        }

        private class ModelItem
        {
            public string Id { get; set; } = string.Empty;
            public bool Installed { get; set; }
        }
    }

}
