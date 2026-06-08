using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Graph.Models;

namespace ClientTicketBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SubscriptionService _subscriptionService;
        private readonly SettingsStore _settingsStore;
        private IList<User>? _users;

        public MainWindow()
        {
            InitializeComponent();

            // Credentials aus Umgebungsvariablen (sichere Konfiguration empfohlen)
            var tenant = Environment.GetEnvironmentVariable("GRAPH_TENANT_ID");
            var clientId = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("GRAPH_CLIENT_SECRET");

            if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                StatusTextBlock.Text = "GRAPH_TENANT_ID / GRAPH_CLIENT_ID / GRAPH_CLIENT_SECRET nicht gesetzt.";
                return;
            }

            _subscriptionService = new SubscriptionService(tenant, clientId, clientSecret);
            _settingsStore = new SettingsStore();

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();

            var settings = await _settingsStore.LoadAsync();
            if (!string.IsNullOrWhiteSpace(settings.SelectedUserId) && _users != null)
            {
                var user = _users.FirstOrDefault(u => u.Id == settings.SelectedUserId);
                if (user != null)
                {
                    UsersComboBox.SelectedValue = user.Id;
                    // Notification URL aus Umgebung
                    var notifyUrl = Environment.GetEnvironmentVariable("GRAPH_NOTIFICATION_URL");
                    if (!string.IsNullOrWhiteSpace(notifyUrl))
                    {
                        try
                        {
                            await _subscriptionService.CreateMailSubscriptionAsync(user.Id!, notifyUrl);
                            StatusTextBlock.Text = $"Subscription für '{user.DisplayName ?? user.UserPrincipalName}' erstellt.";
                        }
                        catch (Exception ex)
                        {
                            StatusTextBlock.Text = $"Fehler beim Erstellen der Subscription: {ex.Message}";
                        }
                    }
                    else
                    {
                        StatusTextBlock.Text = "GRAPH_NOTIFICATION_URL nicht gesetzt — Subscription nicht erstellt.";
                    }
                }
            }
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                StatusTextBlock.Text = "Lade Benutzer...";
                _users = await _subscriptionService.GetUsersAsync();
                UsersComboBox.ItemsSource = _users;
                StatusTextBlock.Text = $"Benutzer geladen: {_users.Count}";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Fehler beim Laden der Benutzer: {ex.Message}";
            }
        }

        private async void UsersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsersComboBox.SelectedValue is string id)
            {
                await _settingsStore.SaveAsync(new UserSettings { SelectedUserId = id });
                StatusTextBlock.Text = $"Ausgewählt und gespeichert: {id}";
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
        }
    }
}