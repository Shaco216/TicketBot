using Dapper;
using Microsoft.Data.SqlClient;
using OolamaCommunication.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace OolamaCommunication.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly IDbConnection _db;

    public CategoryRepository(IDbConnection db)
    {
        _db = db;
    }

    // Erstellt die Tabelle, falls sie nicht existiert
    public async Task CreateTable()
    {
        const string sql = @"IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL
    );
END;";
        if (_db.State != ConnectionState.Open) await ((SqlConnection)_db).OpenAsync();
        await _db.ExecuteAsync(sql);
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        const string sql = "SELECT Id, Name, Description FROM Categories ORDER BY Name";
        if (_db.State != ConnectionState.Open) await ((SqlConnection)_db).OpenAsync();
        return await _db.QueryAsync<Category>(sql);
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        const string sql = "SELECT Id, Name, Description FROM Categories WHERE Id = @Id";
        if (_db.State != ConnectionState.Open) await ((SqlConnection)_db).OpenAsync();
        return await _db.QuerySingleOrDefaultAsync<Category>(sql, new { Id = id });
    }

    public async Task<Category> CreateAsync(Category category)
    {
        const string sql = @"
INSERT INTO Categories (Name, Description)
VALUES (@Name, @Description);
SELECT CAST(SCOPE_IDENTITY() AS int);";

        if (_db.State != ConnectionState.Open) await ((SqlConnection)_db).OpenAsync();
        var id = await _db.QuerySingleAsync<int>(sql, new { category.Name, category.Description });
        category.Id = id;
        return category;
    }

    public async Task UpdateAsync(Category category)
    {
        const string sql = @"
UPDATE Categories
SET Name = @Name, Description = @Description
WHERE Id = @Id;";
        if (_db.State != ConnectionState.Open) await ((SqlConnection)_db).OpenAsync();
        await _db.ExecuteAsync(sql, category);
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Categories WHERE Id = @Id";
        if (_db.State != ConnectionState.Open) await ((SqlConnection)_db).OpenAsync();
        await _db.ExecuteAsync(sql, new { Id = id });
    }
}