using System.Globalization;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// What is currently using one or more agent workspace databases: the agents that are online or
/// idle, the unexpired mail wake daemon lease with the longest expiry (or null when none holds
/// one), and the databases that could not be read, whose activity is unknown.
/// </summary>
internal sealed record AgentWorkspaceActivity(
    IReadOnlyList<string> ActiveAgents,
    AgentWorkspaceActivity.MailWakeLease? ActiveMailWakeLease,
    IReadOnlyList<string> UnreadableDatabases)
{
    /// <summary>
    /// An unexpired mail wake daemon lease and the workspace directory whose database holds it.
    /// </summary>
    public sealed record MailWakeLease(string WorkspaceDirectory, DateTimeOffset ExpiresAt);

    /// <summary>
    /// Reads the given workspace directories' databases without modifying them. A directory without
    /// a database contributes no activity, and a database that cannot be read as a current registry
    /// is listed in <see cref="UnreadableDatabases"/>.
    /// </summary>
    public static async Task<AgentWorkspaceActivity> InspectAsync(
        IEnumerable<string> workspaceDirectories,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var agents = new SortedSet<string>(StringComparer.Ordinal);
        var unreadable = new List<string>();
        MailWakeLease? activeLease = null;

        foreach (var workspaceDirectory in workspaceDirectories)
        {
            var databasePath = AgentWorkspace.GetDatabasePath(workspaceDirectory);

            if (!File.Exists(databasePath))
            {
                continue;
            }

            await using var connection = new SqliteConnection(
                new SqliteConnectionStringBuilder
                {
                    DataSource = databasePath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ToString());

            try
            {
                await connection.OpenAsync(cancellationToken);

                foreach (var row in await ReadAgentsAsync(connection, cancellationToken))
                {
                    if (AgentStateResolver.Resolve(row, now) is AgentState.Online or AgentState.Idle)
                    {
                        agents.Add(row.Name);
                    }
                }

                var expiresAt = await ReadMailWakeLeaseAsync(connection, cancellationToken);

                if (expiresAt is { } lease
                    && lease > now
                    && (activeLease is null || lease > activeLease.ExpiresAt))
                {
                    activeLease = new MailWakeLease(workspaceDirectory, lease);
                }
            }
            catch (Exception ex) when (ex is SqliteException or FormatException or InvalidCastException)
            {
                unreadable.Add(databasePath);
            }
        }

        return new AgentWorkspaceActivity(agents.ToArray(), activeLease, unreadable);
    }

    private static async Task<List<AgentRow>> ReadAgentsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {AgentRow.Columns} FROM agents WHERE deleted_at IS NULL;";

        var rows = new List<AgentRow>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(AgentRow.ReadFrom(reader));
        }

        return rows;
    }

    private static async Task<DateTimeOffset?> ReadMailWakeLeaseAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT expires_at FROM mail_wake_daemons WHERE id = 1;";

        return await command.ExecuteScalarAsync(cancellationToken) is string expiresAt
            ? DateTimeOffset.Parse(expiresAt, CultureInfo.InvariantCulture)
            : null;
    }
}
