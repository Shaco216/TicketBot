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

            // Unnötige lokale Binding-Erzeugung entfernt.

            LstModels.ItemsSource = _models;

            Loaded += async (_, __) => await LoadInstalledModelsAsync();
        }

        private async Task LoadInstalledModelsAsync()
        {
            try
            {
                var installed = await QueryOllamaInstalledModelsAsync();
                Debug.WriteLine("Ollama installed models: " + string.Join(", ", installed));

                _models.Clear();

                // Dynamische Kandidaten: Wenn Ollama Modelle liefert, nutze diese als Kandidaten.
                // Ansonsten auf DefaultCandidates zurückfallen.
                var candidates = installed.Length > 0
                    ? installed.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                    : DefaultCandidates;

                foreach (var id in candidates)
                {
                    // Wenn candidates aus installed stammen, ist Installed immer true.
                    // Für DefaultCandidates prüfen wir, ob ein installiertes Modell den Kandidaten enthält (locker matchen).
                    bool isInstalled = installed.Any(s => !string.IsNullOrEmpty(s) && s.IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0);
                    _models.Add(new ModelItem { Id = id, Installed = isInstalled });
                }

                // Falls keine Kandidaten vorhanden sind (sehr unwahrscheinlich), zeige Fallback
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

                // Falls Ollama nicht erreichbar: Zeige DefaultCandidates als nicht installiert
                _models.Clear();
                foreach (var id in DefaultCandidates)
                {
                    _models.Add(new ModelItem { Id = id, Installed = false });
                }

                LstModels.SelectedIndex = 0;
            }
        }

        private async Task<string[]> QueryOllamaInstalledModelsAsync()
        {
            var url = "http://localhost:11434/models";
            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var list = doc.RootElement.EnumerateArray()
                    .Select(el =>
                    {
                        if (el.ValueKind == JsonValueKind.Object)
                        {
                            if (el.TryGetProperty("id", out var idProp)) return idProp.GetString() ?? string.Empty;
                            if (el.TryGetProperty("model", out var mProp)) return mProp.GetString() ?? string.Empty;
                        }
                        if (el.ValueKind == JsonValueKind.String) return el.GetString() ?? string.Empty;
                        return string.Empty;
                    })
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();

                return list;
            }

            return Array.Empty<string>();
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
