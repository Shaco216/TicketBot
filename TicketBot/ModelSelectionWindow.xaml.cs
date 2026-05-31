using System;
using System.Collections.ObjectModel;
using System.Linq;
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
            // Dispatcher nicht nötig beim normalen Loaded/Await-Flow, aber sicherheitshalber:
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

                // Wenn Ollama Modelle liefert, zeige diese (dynamisch). Sonst DefaultCandidates.
                var candidates = installed.Length > 0
                    ? installed.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                    : DefaultCandidates;

                foreach (var id in candidates)
                {
                    // Wenn candidates aus installed stammen, ist Installed true. Für DefaultCandidates locker prüfen.
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

                // Fallback
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
            try
            {
                var url = "http://localhost:11434/models";
                var resp = await _http.GetAsync(url);
                resp.EnsureSuccessStatusCode();

                using var stream = await resp.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);

                // Flexible Extraktion: Array an Root oder nested unter "models"/"data"
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

                    // Versuch: wenn Objekt selbst enthält id/model/name
                    var single = TryExtractStringFromObject(doc.RootElement);
                    if (!string.IsNullOrEmpty(single)) return new[] { single };
                }

                return Array.Empty<string>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("QueryOllamaInstalledModelsAsync failed: " + ex);
                throw;
            }
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

            // Falls Objekt untypisch ist, versuche, erste string-Property zu nehmen
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
