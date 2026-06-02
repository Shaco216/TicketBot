using Dapper;
using Microsoft.Data.SqlClient;
using OolamaCommunication.Models;
using System.Data;

namespace OolamaCommunication.Repositories;

public class OllamaEmailDtoRepository : IOllamaEmailDtoRepository
{
    private readonly IDbConnection _db;

    public OllamaEmailDtoRepository(IDbConnection Db)
    {
        _db = Db;
    }

    public async Task CreateTable()
    {
        const string sql = @"IF OBJECT_ID(N'dbo.OllamaEmailDtos', N'U') IS NULL"
                         + @"BEGIN
                                CREATE TABLE dbo.OllamaEmailDtos (
                                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                                    From NVARCHAR(MAX) NOT NULL,
                                    To NVARCHAR(MAX) NOT NULL,
                                    Subject NVARCHAR(500) NOT NULL,
                                    Body NVARCHAR(MAX) NOT NULL,
                                    ReceivedAt DATETIME2 NOT NULL DEFAULT GETDATE()
                                );
                            END;";
        if (_db.State != ConnectionState.Open) await ((SqlConnection)_db).OpenAsync();
        await _db.ExecuteAsync(sql);
    }

    public async Task InsertAsync(string sender, string receiver, string subject, string body)
    {
        const string sql = @"Insert Into dbo.OllamaEmailDtos (Id, From, To, Subject, Body, ReceivedAt)"
                         + "Values (@Id, @From, @To, @Subject, @Body, @ReceivedAt)";
        if (_db.State != ConnectionState.Open) await ((SqlConnection)_db).OpenAsync();
        await _db.ExecuteAsync(sql);
    }

    public async Task<IEnumerable<ReceivedEmailDto>> GetAllAsync()
    {
        const string sql = "SELECT Id, From, To, Subject, Body, ReceivedAt FROM dbo.OllamaEmailDtos ORDER BY ReceivedAt DESC";
        if (_db.State != ConnectionState.Open) await ((SqlConnection)_db).OpenAsync();
        return await _db.QueryAsync<ReceivedEmailDto>(sql);
    }

    public async Task<IEnumerable<ReceivedEmailDto>> GetBySenderAsync(string sender)
    {
        const string sql = "SELECT Id, From, To, Subject, Body, ReceivedAt FROM dbo.OllamaEmailDtos WHERE From = @Sender ORDER BY ReceivedAt DESC";
        if (_db.State != ConnectionState.Open) await ((SqlConnection)_db).OpenAsync();
        return await _db.QueryAsync<ReceivedEmailDto>(sql, new { Sender = sender });
    }

    public async Task<IEnumerable<ReceivedEmailDto>> GetByReceiverAsync(string receiver)
    {
        const string sql = "SELECT Id, From, To, Subject, Body, ReceivedAt FROM dbo.OllamaEmailDtos WHERE To = @Receiver ORDER BY ReceivedAt DESC";
        if (_db.State != ConnectionState.Open) await ((SqlConnection)_db).OpenAsync();
        return await _db.QueryAsync<ReceivedEmailDto>(sql, new { Receiver = receiver });
    }

}
