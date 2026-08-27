using System.Data.Common;
using Dapper;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The v10-to-v11 one-way carry of a markdown memory store into the
/// workspace database. Memory used to be markdown files under the workspace
/// directory; the database is the source of truth now, so an upgrade reads
/// whatever is on disk once and inserts it, then leaves the files alone.
/// The files are never deleted: this import is the only thing that reads
/// them again, and leaving them in place means an upgrade can be undone by
/// checking out the older CLI without having lost anything.
/// </summary>
internal static class MemoryMarkdownImport
{
    /// <summary>
    /// Imports every curated memory and journal entry under
    /// <paramref name="workspaceDirectory"/>'s memory directory into the
    /// given connection, skipping ids the database already carries so a
    /// re-run cannot duplicate. A file whose frontmatter does not parse is
    /// skipped rather than failing the upgrade: refusing to open a
    /// workspace over one unreadable memory would be a far worse outcome
    /// than carrying the rest across.
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

        // Journal files sit one date-bucketed directory deep, which the
        // database has no equivalent of: created_at carries the date.
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
