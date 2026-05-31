using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TicketBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatService;

        public MainWindow()
        {
            InitializeComponent();

            // 1. Verbindung zu lokalem Ollama aufbauen (Standardport ist 11434)
            var builder = Kernel.CreateBuilder();
            builder.AddOllamaChatCompletion(
                modelId: "llama3",              // Ersetzen Sie dies durch Ihr installiertes Modell
                endpoint: new Uri("http://localhost:11434")
            );

            _kernel = builder.Build();

            // 2. Chat-Dienst aus dem Kernel extrahieren
            _chatService = _kernel.GetRequiredService<IChatCompletionService>();
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            string userInput = TxtInput.Text.Trim();
            if (string.IsNullOrEmpty(userInput)) return;

            // UI-Elemente während der Generierung vorbereiten
            BtnSend.IsEnabled = false;
            TxtInput.Clear();
            TxtOutput.Text = "Künstliche Intelligenz denkt nach...\n\n";

            try
            {
                // 3. Verwende Streaming, um Wort für Wort Daten zu empfangen
                var responseStream = _chatService.GetStreamingChatMessageContentsAsync(userInput, kernel: _kernel);

                TxtOutput.Clear();

                await foreach (var chunk in responseStream)
                {
                    // Ergänzt das UI-Textfeld fortlaufend im Haupt-UI-Thread
                    TxtOutput.AppendText(chunk.Content);

                    // Automatisch nach unten scrollen, wenn neuer Text generiert wird
                    TxtOutput.ScrollToEnd();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei der Verbindung zu Ollama: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtOutput.Text = "Fehler: Konnte keine Antwort generieren. Läuft Ollama im Hintergrund?";
            }
            finally
            {
                BtnSend.IsEnabled = true;
            }
        }
    }
}