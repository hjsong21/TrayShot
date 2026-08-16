using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TrayShot.Search;
using Xunit;

namespace TrayShot.Tests.Search;

public class SqliteDbTests : IDisposable
{
    private readonly string _tempDbPath;

    public SqliteDbTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"trayshot_test_{Guid.NewGuid():N}.sqlite");
    }

    [Fact]
    public async Task SqliteDb_ExecutesAndQueriesDataCorrectly()
    {
        using var db = new SqliteDb(_tempDbPath);

        await db.ExecuteAsync("CREATE TABLE items (id INTEGER PRIMARY KEY, name TEXT);");
        await db.ExecuteAsync("INSERT INTO items (name) VALUES (@name);", new Dictionary<string, object?> { ["@name"] = "Screenshot1" });

        var results = await db.QueryAsync("SELECT name FROM items WHERE id = @id;", r => r.GetString(0), new Dictionary<string, object?> { ["@id"] = 1 });

        Assert.Single(results);
        Assert.Equal("Screenshot1", results[0]);
    }

    [Fact]
    public async Task SqliteDb_TransactionCommitsAndRollbacksCorrectly()
    {
        using var db = new SqliteDb(_tempDbPath);

        await db.ExecuteAsync("CREATE TABLE items (id INTEGER PRIMARY KEY, name TEXT);");

        bool commitResult = await db.TransactionAsync(d =>
        {
            d.ExecuteSyncInternal("INSERT INTO items (name) VALUES ('Test1');");
            return true;
        });

        Assert.True(commitResult);

        bool rollbackResult = await db.TransactionAsync(d =>
        {
            d.ExecuteSyncInternal("INSERT INTO items (name) VALUES ('Test2');");
            return false; // Force rollback
        });

        Assert.False(rollbackResult);

        var items = await db.QueryAsync("SELECT name FROM items;", r => r.GetString(0));
        Assert.Single(items);
        Assert.Equal("Test1", items[0]);
    }

    public void Dispose()
    {
        if (File.Exists(_tempDbPath))
        {
            try { File.Delete(_tempDbPath); } catch { }
        }
    }
}
