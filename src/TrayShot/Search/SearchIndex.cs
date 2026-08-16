using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TrayShot.Infrastructure;

namespace TrayShot.Search;

public sealed class SearchIndex : IDisposable
{
    private readonly SqliteDb _db;

    public SearchIndex(string dbPath)
    {
        _db = new SqliteDb(dbPath);
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        _db.ExecuteSyncInternal(@"
            CREATE TABLE IF NOT EXISTS files (
                path TEXT PRIMARY KEY,
                mtime REAL NOT NULL,
                size INTEGER NOT NULL,
                ocr_done INTEGER NOT NULL DEFAULT 0
            );

            CREATE VIRTUAL TABLE IF NOT EXISTS ocr USING fts5(
                path,
                text,
                tokenize='trigram'
            );

            CREATE TABLE IF NOT EXISTS semantic (
                path TEXT NOT NULL,
                model TEXT NOT NULL,
                slot INTEGER NOT NULL,
                PRIMARY KEY (path, model)
            );

            CREATE TABLE IF NOT EXISTS semantic_free (
                model TEXT NOT NULL,
                slot INTEGER NOT NULL,
                PRIMARY KEY (model, slot)
            );
        ");
        Log.Search.Info("SearchIndex schema initialized with FTS5 trigram");
    }

    public async Task IndexOcrTextAsync(string path, string text)
    {
        var fi = new FileInfo(path);
        if (!fi.Exists) return;

        double mtime = new DateTimeOffset(fi.LastWriteTimeUtc).ToUnixTimeSeconds();
        long size = fi.Length;

        var parameters = new Dictionary<string, object?>
        {
            ["@path"] = path,
            ["@mtime"] = mtime,
            ["@size"] = size,
            ["@text"] = text
        };

        await _db.TransactionAsync(db =>
        {
            db.ExecuteSyncInternal("INSERT OR REPLACE INTO files (path, mtime, size, ocr_done) VALUES (@path, @mtime, @size, 1);", parameters);
            db.ExecuteSyncInternal("DELETE FROM ocr WHERE path = @path;", parameters);
            if (!string.IsNullOrWhiteSpace(text))
            {
                db.ExecuteSyncInternal("INSERT INTO ocr (path, text) VALUES (@path, @text);", parameters);
            }
            return true;
        });

        Log.Search.Debug($"Indexed OCR text for {path} len={text.Length}");
    }

    public async Task<List<string>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<string>();

        string trimmed = query.Trim();
        var parameters = new Dictionary<string, object?>
        {
            ["@query"] = trimmed,
            ["@likeQuery"] = $"%{trimmed}%"
        };

        if (trimmed.Length >= 3)
        {
            try
            {
                // FTS5 Trigram Match
                return await _db.QueryAsync(
                    "SELECT path FROM ocr WHERE text MATCH @query;",
                    r => r.GetString(0),
                    parameters);
            }
            catch (Exception ex)
            {
                Log.Search.Warn($"FTS5 match failed, falling back to LIKE: {ex.Message}");
            }
        }

        // LIKE query fallback
        return await _db.QueryAsync(
            "SELECT path FROM ocr WHERE text LIKE @likeQuery;",
            r => r.GetString(0),
            parameters);
    }

    public async Task RemoveFileAsync(string path)
    {
        var parameters = new Dictionary<string, object?> { ["@path"] = path };
        await _db.TransactionAsync(db =>
        {
            db.ExecuteSyncInternal("DELETE FROM files WHERE path = @path;", parameters);
            db.ExecuteSyncInternal("DELETE FROM ocr WHERE path = @path;", parameters);
            return true;
        });
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
