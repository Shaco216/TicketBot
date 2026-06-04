using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text.Json;
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
                var installed = await OllamaService.QueryOllamaInstalledModelsAsync();
                Debug.WriteLine("Ollama installed models: " + string.Join(", ", installed));

                _models.Clear();

                // Änderung: Zeige alle möglichen Kandidaten (DefaultCandidates) UND zusätzlich alle
                // von Ollama zurückgelieferten Modelle. So werden nicht-installierte, aber mögliche Modelle angezeigt.
                var candidates = DefaultCandidates
                    .Concat(installed)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                foreach (var id in candidates)
                {
                    bool isInstalled = installed.Any(s => !string.IsNullOrEmpty(s) && s.IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0);
                    _models.Add(new ModelItem { Id = id, Installed = isInstalled });
                }

                if (installed.Length > 0)
                {
                    SetStatus($"Ollama läuft — {installed.Length} installierte Modell(e); zeige {candidates.Length} Kandidat(en)", Brushes.Green);
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

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            if (LstModels.SelectedItem is not ModelItem mi) return;
            await InstallModelAsync(mi);
        }

        private async Task InstallModelAsync(ModelItem model)
        {
            // Deaktivieren UI
            LstModels.IsEnabled = false;
            BtnInstall.IsEnabled = false;
            BtnStart.IsEnabled = false;

            SetStatus($"Installiere {model.Id} …", Brushes.Gray);

            try
            {
                // Versuche Ollama-CLI zu verwenden: "ollama pull <model>"
                var psi = new ProcessStartInfo("ollama", $"pull {model.Id}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                    throw new InvalidOperationException("Konnte den Installationsprozess nicht starten.");

                var outTask = proc.StandardOutput.ReadToEndAsync();
                var errTask = proc.StandardError.ReadToEndAsync();

                await proc.WaitForExitAsync();

                var stdout = await outTask;
                var stderr = await errTask;

                Debug.WriteLine($"ollama pull stdout: {stdout}");
                Debug.WriteLine($"ollama pull stderr: {stderr}");

                if (proc.ExitCode == 0)
                {
                    model.Installed = true; // löst PropertyChanged aus
                    SetStatus($"Installation von {model.Id} abgeschlossen", Brushes.Green);
                }
                else
                {
                    SetStatus($"Installation fehlgeschlagen: {stderr ?? "(keine Meldung)"}", Brushes.Red);
                }
            }
            catch (System.ComponentModel.Win32Exception w32ex)
            {
                // Executable nicht gefunden
                Debug.WriteLine("Win32Exception beim Starten von ollama: " + w32ex);
                SetStatus("Konnte 'ollama' nicht finden. Bitte Ollama-CLI installieren oder in PATH legen.", Brushes.Red);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("InstallModelAsync error: " + ex);
                SetStatus("Fehler bei der Installation: " + ex.Message, Brushes.Red);
            }
            finally
            {
                LstModels.IsEnabled = true;
                BtnInstall.IsEnabled = true;
                BtnStart.IsEnabled = true;
            }
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

        private class ModelItem : INotifyPropertyChanged
        {
            private string _id = string.Empty;
            private bool _installed;

            public string Id
            {
                get => _id;
                set
                {
                    if (_id == value) return;
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }

            public bool Installed
            {
                get => _installed;
                set
                {
                    if (_installed == value) return;
                    _installed = value;
                    OnPropertyChanged(nameof(Installed));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

}
