using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace TicketBot
{
    /// <summary>
    /// Interaktionslogik für ModelSelectionWindow.xaml
    /// </summary>
    public partial class ModelSelectionWindow : Window
    {
        private readonly ObservableCollection<ModelItem> _models = new();
        private static readonly string[] CandidateModels = new[] { "llama3", "llama2", "gpt-4o", "falcon", "llama_cpp" };
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

        public ModelSelectionWindow()
        {
            InitializeComponent();

            // Converter-Registrierung (kleiner, lokaler Ersatz statt separater Resource-Datei)
            var boolToBrush = new System.Windows.Data.Binding
            {
                Converter = new BoolToBrushConverter()
            };

            LstModels.ItemsSource = _models;

            Loaded += async (_, __) => await LoadInstalledModelsAsync();
        }

        private async Task LoadInstalledModelsAsync()
        {
            try
            {
                var installed = await QueryOllamaInstalledModelsAsync();
                _models.Clear();

                foreach (var id in CandidateModels.Distinct())
                {
                    _models.Add(new ModelItem { Id = id, Installed = installed.Contains(id, StringComparer.OrdinalIgnoreCase) });
                }

                // Falls keine Kandidaten installiert sind, Zeige trotzdem die Liste und markiere nichts
                if (_models.Count == 0)
                {
                    foreach (var id in CandidateModels)
                    {
                        _models.Add(new ModelItem { Id = id, Installed = false });
                    }
                }

                LstModels.SelectedIndex = 0;
            }
            catch
            {
                // Falls Ollama nicht erreichbar: Zeige Kandidaten als nicht installiert
                _models.Clear();
                foreach (var id in CandidateModels)
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
