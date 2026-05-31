using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Windows;

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

            // Modell aus der Auswahl entnehmen (Fallback: "llama3")
            var selectedModel = Application.Current.Properties["SelectedModel"] as string ?? "llama3";

            var builder = Kernel.CreateBuilder();
            builder.AddOllamaChatCompletion(
                modelId: selectedModel,
                endpoint: new Uri("http://localhost:11434")
            );

            _kernel = builder.Build();

            _chatService = _kernel.GetRequiredService<IChatCompletionService>();
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            string userInput = TxtInput.Text.Trim();
            if (string.IsNullOrEmpty(userInput)) return;

            BtnSend.IsEnabled = false;
            TxtInput.Clear();
            TxtOutput.Text = "Künstliche Intelligenz denkt nach...\n\n";

            try
            {
                var responseStream = _chatService.GetStreamingChatMessageContentsAsync(userInput, kernel: _kernel);

                TxtOutput.Clear();

                await foreach (var chunk in responseStream)
                {
                    TxtOutput.AppendText(chunk.Content);
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