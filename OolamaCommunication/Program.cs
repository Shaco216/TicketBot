using Microsoft.Data.SqlClient;
using OolamaCommunication.Repositories;
using System.Data;
using OolamaCommunication.Services;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using OolamaCommunication.Erweiterungen;
using Microsoft.OpenApi.Models;

namespace OolamaCommunication;
internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        //builder.Services.AddOllamaServiceFromConfig(builder.Configuration);

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var connString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' fehlt.");

        // IDbConnection
        builder.Services.AddTransient<IDbConnection>(_ => new SqlConnection(connString));

        // Repository
        builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
        builder.Services.AddScoped<IOllamaEmailDtoRepository, OllamaEmailDtoRepository>();

        // Registriere den Kategorizer (stellt sicher, dass ICategoryRepository injiziert wird)
        // Wenn OllamaService ebenfalls per AddOllamaServiceFromConfig registriert ist, wird es injiziert.
        builder.Services.AddScoped<IOllamaTicketCategorizer, OllamaTicketCategorizer>();

        // ---------- Dynamische Bindung: IP und Gerätename ----------
        // Optional per Umgebungsvariable setzen (z. B. BIND_HOST=my-host.local)
        var bindHost = Environment.GetEnvironmentVariable("BIND_HOST");
        var bindPort = int.TryParse(Environment.GetEnvironmentVariable("BIND_PORT"), out var p) ? p : 5000;


        var candidateAddresses = GetCandidateIPv4Addresses(bindHost).ToList();

        if (candidateAddresses.Any())
        {
            builder.WebHost.ConfigureKestrel(options =>
            {
                // Binde an alle eindeutigen gefundene IPs
                foreach (var ip in candidateAddresses)
                {
                    options.Listen(ip, bindPort);
                }
            });
        }
        else
        {
            // Fallback: an alle Interfaces binden
            builder.WebHost.UseUrls($"http://0.0.0.0:{bindPort}");
        }
        // ----------------------------------------------------------

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            // Swagger JSON so anpassen, dass "servers" dynamisch auf die aktuelle Host:Port Kombination gesetzt wird
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                {
                    var serverUrl = $"{httpReq.Scheme}://{httpReq.Host.Value}";
                    swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = serverUrl } };
                });
            });

            app.UseSwaggerUI();
        }

        //app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        #region Datenbank erstellen
        var instance = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
    ? args[0]
    : @"(localdb)\MSSQLLocalDB";
        var dbName = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1])
            ? args[1]
            : "Ticketbot";
        var folder = args.Length > 2 && !string.IsNullOrWhiteSpace(args[2])
            ? args[2]
            : @"C:\Ticketbot\DB";

        try
        {
            Console.WriteLine($"Erstelle Ordner: {folder}");
            Directory.CreateDirectory(folder);

            var mdf = Path.Combine(folder, $"{dbName}.mdf");
            var ldf = Path.Combine(folder, $"{dbName}_log.ldf");

            var masterConn = new SqlConnection($"Server={instance};Integrated Security=true;Database=master");
            await masterConn.OpenAsync();

            var checkAndCreate = $@"
IF DB_ID(N'{dbName}') IS NULL
BEGIN
    CREATE DATABASE [{dbName}]
    ON PRIMARY (NAME = N'{dbName}_Data', FILENAME = N'{mdf}')
    LOG ON (NAME = N'{dbName}_Log', FILENAME = N'{ldf}');
    SELECT 1; -- neu erstellt
END
ELSE
BEGIN
    SELECT 0; -- existierte bereits
END;";

            using var cmd = masterConn.CreateCommand();
            cmd.CommandText = checkAndCreate;
            cmd.CommandTimeout = 600;
            Console.WriteLine($"Erstelle Datenbank '{dbName}' auf Instanz '{instance}'...");
            var resultObj = await cmd.ExecuteScalarAsync();
            int result = resultObj is int i ? i : -1;
            Console.WriteLine(result == 1 ? "Datenbank neu erstellt." : result == 0 ? "Datenbank existierte bereits." : $"Unbekanntes Ergebnis: {result}");
            Console.WriteLine("Fertig.");
            await masterConn.CloseAsync();
        }
        catch (SqlException ex)
        {
            Console.Error.WriteLine($"SQL-Fehler: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fehler: {ex.Message}");
        }

        // Beim Start: Tabelle anlegen (falls nicht vorhanden)
        // Achtung: Hier wird synchron/awaited ausgef�hrt � sicherstellen, dass CreateTable async ist
        using (var scope = app.Services.CreateScope())
        {
            ICategoryRepository CategoryRepo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
            IOllamaEmailDtoRepository OllamaEmailRepo = scope.ServiceProvider.GetRequiredService<IOllamaEmailDtoRepository>();
            // Wenn Methode CreateTable async hei�t: await repo.CreateTable();
            // Beispiel:
            await CategoryRepo.CreateTable();
            await OllamaEmailRepo.CreateTable();
        }

        #endregion

        // Start & Logging der tatsächlich gebundenen Adressen
        await app.StartAsync();
        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses;
        var bound = addresses != null ? string.Join(", ", addresses) : "(keine Adressen verfügbar)";
        app.Logger.LogInformation("Kestrel bound to: {bound}", bound);

        await app.WaitForShutdownAsync();
    }

    #region ZusatzMethoden
    /// <summary>
    /// ermittelt eine sinnvolle lokale IPv4-Adresse oder null
    /// </summary>
    /// <returns>current IP Address</returns>
    static IPAddress? GetLocalIPv4Address()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                 n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        {
            var props = ni.GetIPProperties();
            foreach (var ua in props.UnicastAddresses)
            {
                if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    var ip = ua.Address;
                    // optional: Filter für Link-Local / APIPA
                    if (!ip.ToString().StartsWith("169.254")) return ip;
                }
            }
        }
        return null;
    }

    static IEnumerable<IPAddress> GetCandidateIPv4Addresses(string? preferredHost)
    {
        var found = new List<IPAddress>();

        // 1) Wenn ein BIND_HOST gesetzt ist: versuche Auflösung
        if (!string.IsNullOrWhiteSpace(preferredHost))
        {
            try
            {
                var addrs = Dns.GetHostAddresses(preferredHost)
                               .Where(a => a.AddressFamily == AddressFamily.InterNetwork);
                found.AddRange(addrs);
            }
            catch
            {
                // ignore
            }
        }

        // 2) Hostname des Geräts auflösen (Gerätename)
        try
        {
            var hostName = Dns.GetHostName();
            var hostAddrs = Dns.GetHostAddresses(hostName)
                               .Where(a => a.AddressFamily == AddressFamily.InterNetwork);
            found.AddRange(hostAddrs);
        }
        catch
        {
            // ignore
        }

        // 3) Netzwerkschnittstellen nach aktiven IPv4-Adressen durchsuchen
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces()
                         .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                     n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            {
                var props = ni.GetIPProperties();
                foreach (var ua in props.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var ip = ua.Address;
                        // Filter: keine APIPA (169.254.x.x)
                        if (!ip.ToString().StartsWith("169.254"))
                            found.Add(ip);
                    }
                }
            }
        }
        catch
        {
            // ignore
        }

        // Deduplicate und keine Loopback-Adresse zurückgeben
        return found
            .Where(a => !IPAddress.IsLoopback(a))
            .Distinct(new IPAddressComparer());
    }
    #endregion

}