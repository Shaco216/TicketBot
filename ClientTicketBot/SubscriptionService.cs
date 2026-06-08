using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Graph.Models;

namespace ClientTicketBot;

public class SubscriptionService
{
    private readonly GraphServiceClient _graph;

    public SubscriptionService(string tenantId, string clientId, string clientSecret)
    {
        var cred = new ClientSecretCredential(tenantId, clientId, clientSecret);
        _graph = new GraphServiceClient(cred);
    }

    public async Task<Subscription?> CreateMailSubscriptionAsync(string userId, string notificationUrl, string clientState = "secret")
    {
        var subscription = new Subscription
        {
            Resource = $"/users/{userId}/mailFolders('inbox')/messages",
            ChangeType = "created,updated,deleted",
            NotificationUrl = notificationUrl,
            ClientState = clientState,
            ExpirationDateTime = DateTimeOffset.UtcNow.AddHours(2)
        };

        // Korrigiert: Verwende PostAsync statt Request().AddAsync
        return await _graph.Subscriptions.PostAsync(subscription);
    }

    // Liefert Benutzer (erste Seite, Top max 999). Für große Tenants Paging ergänzen.
    public async Task<IList<User>> GetUsersAsync(int top = 999)
    {
        var users = new List<User>();
        var response = await _graph.Users.GetAsync(cfg =>
        {
            cfg.QueryParameters.Top = top;
            cfg.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName" };
        });

        if (response?.Value != null) users.AddRange(response.Value);
        return users;
    }
}
