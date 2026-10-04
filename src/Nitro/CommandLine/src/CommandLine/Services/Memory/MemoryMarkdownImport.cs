using System.Data.Common;
using Dapper;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Imports legacy markdown memories and journal entries into the workspace database
/// without modifying the source files.
/// </summary>
internal static class MemoryMarkdownImport
{
    /// <summary>
    /// Imports readable, valid memory markdown files and returns the number of inserted
    /// entries. Existing ids or promotion links are retained without overwriting their records.
    /// </summary>
    public static async Task<int> ImportAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        string workspaceDirectory,
        CancellationToken cancellationToken)
    {
        var memoryDirectory = AgentWorkspace.GetMemoryDirectory(workspaceDirectory);

        if (!Directory.Exists(memoryDirectory))
        {
            return 0;
        }

        var imported = 0;
        imported += await ImportCuratedAsync(connection, transaction, memoryDirectory, cancellationToken);
        imported += await ImportJournalAsync(connection, transaction, memoryDirectory, cancellationToken);

        return imported;
    }

    private static async Task<int> ImportCuratedAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        string memoryDirectory,
        CancellationToken cancellationToken)
    {
        var curatedDirectory = AgentWorkspace.GetMemoryCuratedDirectory(memoryDirectory);

        if (!Directory.Exists(curatedDirectory))
        {
            return 0;
        }

        var imported = 0;

        foreach (var path in Directory.EnumerateFiles(curatedDirectory, "*.md").Order(StringComparer.Ordinal))
        {
            var id = Path.GetFileNameWithoutExtension(path);

            if (ReadAllText(path) is not { } content
                || !MemoryFrontmatterParser.TryParse(content, id, out var frontmatter, out _))
            {
                continue;
            }

            var rows = await connection.ExecuteAsync(
                """
                INSERT OR IGNORE INTO memory_curated (
                    id, type, body, created_at, updated_at, created_by, promoted_from
                ) VALUES (
                    @id, @type, @body, @createdAt, @updatedAt, @createdBy, @promotedFrom
                );
                """,
                new
                {
                    id = frontmatter.Id,
                    type = frontmatter.Type,
                    body = frontmatter.Body,
                    createdAt = frontmatter.CreatedAt,
                    updatedAt = frontmatter.UpdatedAt,
                    createdBy = frontmatter.CreatedBy,
                    promotedFrom = frontmatter.PromotedFrom,
                    cancellationToken
                },
                transaction);

            if (rows == 0)
            {
                continue;
            }

            imported++;

            foreach (var tag in frontmatter.Tags)
            {
                await connection.ExecuteAsync(
                    "INSERT OR IGNORE INTO memory_curated_tags (id, tag) VALUES (@id, @tag);",
                    new { id = frontmatter.Id, tag, cancellationToken },
                    transaction);
            }
        }

        return imported;
    }

    private static async Task<int> ImportJournalAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        string memoryDirectory,
        CancellationToken cancellationToken)
    {
        var journalDirectory = AgentWorkspace.GetMemoryJournalDirectory(memoryDirectory);

        if (!Directory.Exists(journalDirectory))
        {
            return 0;
        }

        var imported = 0;

        // Journal files are imported recursively from the journal directory.
        foreach (var path in Directory
            .EnumerateFiles(journalDirectory, "*.md", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal))
        {
            var id = Path.GetFileNameWithoutExtension(path);

            if (ReadAllText(path) is not { } content
                || !MemoryJournalFrontmatterParser.TryParse(content, id, out var frontmatter, out _))
            {
                continue;
            }

            imported += await connection.ExecuteAsync(
                """
                INSERT OR IGNORE INTO memory_journal (id, body, created_at, created_by)
                VALUES (@id, @body, @createdAt, @createdBy);
                """,
                new
                {
                    id = frontmatter.Id,
                    body = frontmatter.Body,
                    createdAt = frontmatter.CreatedAt,
                    createdBy = frontmatter.CreatedBy,
                    cancellationToken
                },
                transaction);
        }

        return imported;
    }

    private static string? ReadAllText(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
