using Dapper;
using Microsoft.Data.Sqlite;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Builds, self-heals, and queries the disposable per-scope FTS5 search
/// index at <c>.local/index.db</c>. A read validates the index's stored
/// fingerprint against the curated directory's current fingerprint and
/// rebuilds automatically when the index is missing, corrupt, or stale. A
/// rebuild is written to a temp file in the same directory and moved into
/// place only after every curated file parses, so a failed rebuild (a
/// malformed file) never replaces the last valid index.
/// </summary>
/// <remarks>
/// Takes <see cref="IFileSystem"/> for content and enumeration operations,
/// but the atomic index replacement (<see cref="File.Move(string, string, bool)"/>,
/// <see cref="File.Delete(string)"/>) deliberately reads and writes the real
/// filesystem: <see cref="IFileSystem"/> covers only content and
/// enumeration operations.
/// </remarks>
internal static class MemoryFtsIndex
{
    static MemoryFtsIndex() => SQLitePCL.Batteries_V2.Init();

    private static readonly TimeSpan s_abandonedTempFileAge = TimeSpan.FromHours(1);

    /// <summary>
    /// Returns the ids of curated memories in one scope's index whose body
    /// matches <paramref name="matchQuery"/> (an FTS5 MATCH expression built
    /// by <see cref="MemoryFtsQuery"/>), narrowed by the given type, tags
    /// (AND), and minimum updated-at timestamp, ordered by FTS rank, then
    /// updated_at descending, then id. Ensures the index is fresh first,
    /// rebuilding it when necessary.
    /// </summary>
    public static async Task<IReadOnlyList<string>> SearchAsync(
        IFileSystem fileSystem,
        string curatedDirectory,
        string localDirectory,
        string matchQuery,
        string? type,
        IReadOnlyList<string> tags,
        DateTimeOffset? since,
        CancellationToken cancellationToken)
    {
        await using var connection = await EnsureFreshAsync(
            fileSystem, curatedDirectory, localDirectory, cancellationToken);

        return await QueryAsync(connection, matchQuery, type, tags, since, cancellationToken);
    }

    /// <summary>
    /// Unconditionally rebuilds the given scope's index from its curated
    /// directory and returns the number of memories indexed. Used by the
    /// <c>reindex</c> repair command; normal reads rebuild automatically
    /// only when the index is missing, corrupt, or stale.
    /// </summary>
    public static async Task<int> RebuildAsync(
        IFileSystem fileSystem,
        string curatedDirectory,
        string localDirectory,
        CancellationToken cancellationToken)
    {
        await using var connection = await BuildAsync(
            fileSystem, curatedDirectory, localDirectory, cancellationToken);

        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM curated;");
    }

    // The WHERE clause here is assembled at runtime from the given filters,
    // so the SQL text is never a call-site literal; Dapper.AOT can only
    // intercept calls whose SQL it can read at compile time. Reading through
    // plain ADO.NET instead of Dapper's reflection fallback keeps this path
    // free of runtime code generation, mirroring TaskStore.ExecuteTaskQueryAsync.
    private static async Task<IReadOnlyList<string>> QueryAsync(
        SqliteConnection connection,
        string matchQuery,
        string? type,
        IReadOnlyList<string> tags,
        DateTimeOffset? since,
        CancellationToken cancellationToken)
    {
        var conditions = new List<string> { "curated_fts MATCH @matchQuery" };
        var parameters = new Dictionary<string, object?> { ["matchQuery"] = matchQuery };

        if (type is not null)
        {
            conditions.Add("c.type = @type");
            parameters["type"] = type;
        }

        if (since is { } value)
        {
            conditions.Add("c.updated_at >= @since");
            parameters["since"] = MemoryDates.Format(value);
        }

        for (var i = 0; i < tags.Count; i++)
        {
            var parameterName = $"tag{i}";
            conditions.Add($"c.id IN (SELECT id FROM curated_tags WHERE tag = @{parameterName})");
            parameters[parameterName] = tags[i];
        }

        var sql = $"""
            SELECT c.id
            FROM curated_fts
            JOIN curated c ON c.id = curated_fts.id
            WHERE {string.Join(" AND ", conditions)}
            ORDER BY bm25(curated_fts) ASC, c.updated_at DESC, c.id ASC;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, parameterValue) in parameters)
        {
            command.Parameters.AddWithValue("@" + name, parameterValue ?? DBNull.Value);
        }

        var ids = new List<string>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetString(0));
        }

        return ids;
    }

    private static async Task<SqliteConnection> EnsureFreshAsync(
        IFileSystem fileSystem,
        string curatedDirectory,
        string localDirectory,
        CancellationToken cancellationToken)
    {
        var indexPath = AgentWorkspace.GetMemoryIndexDatabasePath(localDirectory);
        var fingerprint = MemoryIndexFingerprint.Compute(fileSystem, curatedDirectory);

        return await TryOpenValidAsync(indexPath, fingerprint, cancellationToken)
            ?? await BuildAsync(fileSystem, curatedDirectory, localDirectory, cancellationToken);
    }

    private static async Task<SqliteConnection?> TryOpenValidAsync(
        string indexPath, string fingerprint, CancellationToken cancellationToken)
    {
        if (!File.Exists(indexPath))
        {
            return null;
        }

        SqliteConnection? connection = null;

        try
        {
            connection = new SqliteConnection($"Data Source={indexPath};Pooling=False");
            await connection.OpenAsync(cancellationToken);

            var quickCheck = await connection.QueryFirstOrDefaultAsync<string>("PRAGMA quick_check;");

            if (quickCheck != "ok")
            {
                await connection.DisposeAsync();
                return null;
            }

            var storedFingerprint = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT value FROM meta WHERE key = @key;",
                new { key = MemoryIndexSchema.FingerprintKey });

            if (storedFingerprint != fingerprint)
            {
                await connection.DisposeAsync();
                return null;
            }

            return connection;
        }
        catch (SqliteException)
        {
            if (connection is not null)
            {
                await connection.DisposeAsync();
            }

            return null;
        }
    }

    /// <summary>
    /// Builds a fresh index in a temp file in <paramref name="localDirectory"/>
    /// and moves it into place only once every curated file has parsed, so a
    /// failed build (a malformed file) leaves the previous index, if any,
    /// untouched. Throws <see cref="ExitException"/> on a malformed file
    /// rather than silently skipping it or serving stale results.
    /// </summary>
    private static async Task<SqliteConnection> BuildAsync(
        IFileSystem fileSystem,
        string curatedDirectory,
        string localDirectory,
        CancellationToken cancellationToken)
    {
        if (!fileSystem.DirectoryExists(localDirectory))
        {
            fileSystem.CreateDirectory(localDirectory);
        }

        fileSystem.CleanupAbandonedTempFiles(localDirectory, s_abandonedTempFileAge);

        var indexPath = AgentWorkspace.GetMemoryIndexDatabasePath(localDirectory);
        var tempPath = Path.Combine(
            localDirectory,
            $".{AgentWorkspace.MemoryIndexDatabaseFileName}.nitro-tmp-{Guid.NewGuid():N}");
        var fingerprint = MemoryIndexFingerprint.Compute(fileSystem, curatedDirectory);

        try
        {
            await using (var connection = new SqliteConnection($"Data Source={tempPath};Pooling=False"))
            {
                await connection.OpenAsync(cancellationToken);
                await connection.ExecuteAsync(MemoryIndexSchema.Create);

                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

                if (fileSystem.DirectoryExists(curatedDirectory))
                {
                    foreach (var path in fileSystem
                        .GetFiles(curatedDirectory, "*.md", SearchOption.TopDirectoryOnly)
                        .OrderBy(path => path, StringComparer.Ordinal))
                    {
                        var id = Path.GetFileNameWithoutExtension(path);
                        var content = await fileSystem.ReadAllTextAsync(path, cancellationToken);

                        if (!MemoryFrontmatterParser.TryParse(content, id, out var frontmatter, out var failure))
                        {
                            throw new ExitException(
                                $"Memory '{id}' has malformed frontmatter: {failure.Message}");
                        }

                        await connection.ExecuteAsync(
                            """
                            INSERT INTO curated (id, type, created_at, updated_at, created_by, promoted_from, path)
                            VALUES (@Id, @Type, @CreatedAt, @UpdatedAt, @CreatedBy, @PromotedFrom, @Path);
                            """,
                            new
                            {
                                Id = id,
                                frontmatter.Type,
                                CreatedAt = MemoryDates.Format(frontmatter.CreatedAt),
                                UpdatedAt = MemoryDates.Format(frontmatter.UpdatedAt),
                                frontmatter.CreatedBy,
                                frontmatter.PromotedFrom,
                                Path = path
                            },
                            transaction);

                        foreach (var tag in frontmatter.Tags)
                        {
                            await connection.ExecuteAsync(
                                "INSERT INTO curated_tags (id, tag) VALUES (@Id, @Tag);",
                                new { Id = id, Tag = tag },
                                transaction);
                        }

                        await connection.ExecuteAsync(
                            "INSERT INTO curated_fts (id, body) VALUES (@Id, @Body);",
                            new { Id = id, Body = frontmatter.Body },
                            transaction);
                    }
                }

                await connection.ExecuteAsync(
                    "INSERT INTO meta (key, value) VALUES (@Key, @Value);",
                    new { Key = MemoryIndexSchema.FingerprintKey, Value = fingerprint },
                    transaction);

                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            TryDeleteFile(tempPath);
            throw;
        }

        File.Move(tempPath, indexPath, overwrite: true);

        var opened = new SqliteConnection($"Data Source={indexPath};Pooling=False");
        await opened.OpenAsync(cancellationToken);
        return opened;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
