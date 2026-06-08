using System.IO;
using System.Text.Json;

namespace ClientTicketBot;

public class UserSettings
{
    public string? SelectedUserId { get; set; }
}

public class SettingsStore
{
    private readonly string _filePath;

    public SettingsStore()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TicketBot");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
    }

    public async Task<UserSettings> LoadAsync()
    {
        try
        {
            if (!File.Exists(_filePath)) return new UserSettings();
            var txt = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<UserSettings>(txt) ?? new UserSettings();
        }
        catch
        {
            return new UserSettings();
        }
    }

    public async Task SaveAsync(UserSettings settings)
    {
        var tmp = _filePath + ".tmp";
        var txt = JsonSerializer.Serialize(settings);
        await File.WriteAllTextAsync(tmp, txt);
        File.Move(tmp, _filePath, true);
    }
}