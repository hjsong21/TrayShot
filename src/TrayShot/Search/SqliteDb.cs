using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace TrayShot.Search;

public sealed class SqliteDb : IDisposable
{
    private readonly string _dbPath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private SqliteConnection? _connection;
    private bool _disposed;

    public string Path => _dbPath;

    public SqliteDb(string dbPath, int busyTimeoutMs = 5000)
    {
        _dbPath = dbPath;
        var directory = System.IO.Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            DefaultTimeout = busyTimeoutMs
        };

        _connection = new SqliteConnection(builder.ConnectionString);
        _connection.Open();

        ExecutePragmas();
    }

    private void ExecutePragmas()
    {
        using var cmd1 = _connection!.CreateCommand();
        cmd1.CommandText = "PRAGMA journal_mode=WAL;";
        cmd1.ExecuteNonQuery();

        using var cmd2 = _connection.CreateCommand();
        cmd2.CommandText = "PRAGMA synchronous=NORMAL;";
        cmd2.ExecuteNonQuery();
    }

    public async Task<int> ExecuteAsync(string sql, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            EnsureConnection();
            using var cmd = CreateCommand(sql, parameters);
            return await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<T>> QueryAsync<T>(string sql, Func<SqliteDataReader, T> mapper, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            EnsureConnection();
            using var cmd = CreateCommand(sql, parameters);
            using var reader = await cmd.ExecuteReaderAsync();
            var results = new List<T>();
            while (await reader.ReadAsync())
            {
                results.Add(mapper(reader));
            }
            return results;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> TransactionAsync(Func<SqliteDb, bool> action)
    {
        await _semaphore.WaitAsync();
        try
        {
            EnsureConnection();
            using var tx = _connection!.BeginTransaction();
            try
            {
                bool success = action(this);
                if (success)
                {
                    await tx.CommitAsync();
                    return true;
                }
                else
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public int ExecuteSyncInternal(string sql, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        EnsureConnection();
        using var cmd = CreateCommand(sql, parameters);
        return cmd.ExecuteNonQuery();
    }

    private SqliteCommand CreateCommand(string sql, IReadOnlyDictionary<string, object?>? parameters)
    {
        var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        if (parameters != null)
        {
            foreach (var kvp in parameters)
            {
                cmd.Parameters.AddWithValue(kvp.Key, kvp.Value ?? DBNull.Value);
            }
        }
        return cmd;
    }

    private void EnsureConnection()
    {
        if (_disposed || _connection == null)
        {
            throw new ObjectDisposedException(nameof(SqliteDb));
        }
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            _connection.Open();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _connection?.Close();
        _connection?.Dispose();
        _connection = null;
        _semaphore.Dispose();
    }
}
