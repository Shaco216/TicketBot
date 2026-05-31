using Microsoft.Data.SqlClient;
using System.Data;
using OolamaCommunication.Data;
using OolamaCommunication.Repositories;
using OolamaCommunication.Services;

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration.GetConnectionString("DefaultConnection") 
           ?? throw new InvalidOperationException("Connection string 'DefaultConnection' fehlt.");

// IDbConnection pro Aufruf (Transient)
builder.Services.AddTransient<IDbConnection>(_ => new SqlConnection(conn));

// Repository + Categorizer
builder.Services.AddScoped<ICategoryRepository, DapperCategoryRepository>();
builder.Services.AddScoped<ITicketCategorizer, OllamaTicketCategorizer>();

// Optional: OllamaService registrieren, falls vorhanden
// builder.Services.AddSingleton<OllamaService>(_ => new OllamaService("llama3"));

builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();