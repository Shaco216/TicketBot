using Microsoft.Data.SqlClient;
using OolamaCommunication;
using OolamaCommunication.Repositories;
using System.Data;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddOllamaServiceFromConfig(builder.Configuration);

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var connString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' fehlt.");

        // Beispiel 1: DB bereits an Instance angehängt (Name Ticketbot)
        builder.Services.AddTransient<IDbConnection>(_ => new SqlConnection(connString));

        // Repository
        builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
        builder.Services.AddScoped<IOllamaEmailDtoRepository, OllamaEmailDtoRepository>();

        var app = builder.Build();
        
        // Beim Start: Tabelle anlegen (falls nicht vorhanden)
        // Achtung: Hier wird synchron/awaited ausgeführt — sicherstellen, dass CreateTable async ist
        using (var scope = app.Services.CreateScope())
        {
            ICategoryRepository CategoryRepo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
            IOllamaEmailDtoRepository OllamaEmailRepo = scope.ServiceProvider.GetRequiredService<IOllamaEmailDtoRepository>();
            // Wenn Methode CreateTable async heißt: await repo.CreateTable();
            // Beispiel:
            await CategoryRepo.CreateTable();
            await OllamaEmailRepo.CreateTable();
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

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
END
ELSE
BEGIN
    PRINT N'Datenbank {dbName} existiert bereits.';
END;";

            using var cmd = masterConn.CreateCommand();
            cmd.CommandText = checkAndCreate;
            cmd.CommandTimeout = 600;
            Console.WriteLine($"Erstelle Datenbank '{dbName}' auf Instanz '{instance}'...");
            await cmd.ExecuteNonQueryAsync();
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
        #endregion
        app.Run();
    }
}