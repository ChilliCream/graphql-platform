using System.Globalization;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

/// <summary>
/// Static, free diagnostics for the hook and session presence layer: schema
/// version, orphaned/unclaimed session rows, dead-generation rows pending
/// reap, and mixed-instance rows stranded by an instance id regeneration
/// (with an explicit, opt-in cleanup for those). Round-trip probes (register
/// a scratch actor, send mail, verify a ledger claim and a ping result) are
/// out of scope here; they need a live claimed session and land later as
/// <c>doctor --probe</c>.
/// </summary>
internal sealed class DoctorAgentCommand : Command
{
    public DoctorAgentCommand() : base("doctor")
    {
        Description = "Check the agent workspace's schema and session presence for problems.";

        Options.Add(Opt<CleanMixedInstanceAgentOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent doctor", "agent doctor --clean-mixed-instance");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var database = services.GetRequiredService<AgentDatabase>();
        var instanceIdProvider = services.GetRequiredService<INitroInstanceIdProvider>();
        var globalConfigDirectoryProvider = services.GetRequiredService<IGlobalConfigDirectoryProvider>();
        var processInfoProvider = services.GetRequiredService<IProcessInfoProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var cleanMixedInstance = parseResult.GetValue(Opt<CleanMixedInstanceAgentOption>.Instance);

        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        // Reads the stamped version without the strict equality check
        // AgentDatabase.ConnectAsync enforces: doctor's whole purpose is to
        // report a mismatch, not throw on one.
        var version = await database.ReadVersionAsync(workspaceDirectory, cancellationToken);
        var schemaStatus = ClassifySchemaVersion(version);
        var schemaCurrent = schemaStatus == SchemaStatus.Current;

        string? currentInstanceId = null;
        IReadOnlyList<AgentSessionDoctorRow> unclaimedSessions = [];
        IReadOnlyList<AgentSessionDoctorRow> deadGenerationSessions = [];
        IReadOnlyList<AgentSessionDoctorRow> mixedInstanceSessions = [];
        var mixedInstanceSessionsCleaned = 0;

        if (schemaCurrent)
        {
            // The agent_sessions table only exists once the schema is
            // current (v4); querying it against an un-upgraded v2/v3
            // database would fail on a missing table, so every session
            // check below is gated on schemaCurrent.
            currentInstanceId = await instanceIdProvider.GetIdAsync(
                globalConfigDirectoryProvider.GetDirectory(), cancellationToken);

            await using var connection = await database.ConnectAsync(workspaceDirectory, cancellationToken);

            var rows = (await connection.QueryAsync<SessionDoctorRow>(
                """
                SELECT harness AS Harness, session_id AS SessionId, agent_name AS AgentName,
                       binding_kind AS BindingKind, host AS Host, pid AS Pid, proc_start AS ProcStart,
                       workspace_path AS WorkspacePath
                FROM agent_sessions
                ORDER BY harness, session_id;
                """))
                .ToArray();

            unclaimedSessions = rows
                .Where(row => row.BindingKind == AgentSessionBindingKind.None)
                .Select(ToDoctorRow)
                .ToArray();

            deadGenerationSessions = rows
                .Where(row => row.Host == currentInstanceId
                    && !processInfoProvider.IsAlive(row.Pid, ParseProcStart(row.ProcStart)))
                .Select(ToDoctorRow)
                .ToArray();

            var mixedInstanceRows = rows.Where(row => row.Host != currentInstanceId).ToArray();

            if (cleanMixedInstance && mixedInstanceRows.Length > 0)
            {
                mixedInstanceSessionsCleaned = await CleanMixedInstanceRowsAsync(
                    connection, mixedInstanceRows, cancellationToken);

                // Re-select rather than trust the delete count against the
                // pre-cleanup snapshot: a full-generation predicate can
                // legitimately match nothing (the row moved on between the
                // SELECT above and the DELETE), and the report must reflect
                // what is actually left in the database, not what this
                // command merely attempted.
                mixedInstanceRows = (await connection.QueryAsync<SessionDoctorRow>(
                    """
                    SELECT harness AS Harness, session_id AS SessionId, agent_name AS AgentName,
                           binding_kind AS BindingKind, host AS Host, pid AS Pid, proc_start AS ProcStart,
                           workspace_path AS WorkspacePath
                    FROM agent_sessions
                    WHERE host != @currentInstanceId
                    ORDER BY harness, session_id;
                    """,
                    new { currentInstanceId }))
                    .ToArray();
            }

            mixedInstanceSessions = mixedInstanceRows.Select(ToDoctorRow).ToArray();
        }

        var healthy = schemaCurrent && mixedInstanceSessions.Count == 0;

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new AgentDoctorResult(
                workspaceDirectory,
                version,
                schemaStatus.ToString(),
                schemaCurrent,
                currentInstanceId,
                unclaimedSessions,
                deadGenerationSessions,
                mixedInstanceSessions,
                mixedInstanceSessionsCleaned,
                healthy)));

            return healthy ? ExitCodes.Success : ExitCodes.Error;
        }

        console.WriteLine($"Workspace: {workspaceDirectory}");
        console.WriteLine($"Schema: v{version} ({DescribeSchemaStatus(schemaStatus, version)})");
        console.WriteLine();

        WriteCheck(console, "Schema version", schemaCurrent, schemaCurrent
            ? []
            : [DescribeSchemaStatus(schemaStatus, version)]);

        if (!schemaCurrent)
        {
            console.WriteLine();
            console.WriteLine("Session checks skipped: the schema is not current.");

            return healthy ? ExitCodes.Success : ExitCodes.Error;
        }

        WriteCheck(
            console,
            "Mixed-instance sessions",
            mixedInstanceSessions.Count == 0,
            mixedInstanceSessions.Select(DescribeRow));

        if (mixedInstanceSessionsCleaned > 0)
        {
            console.WriteLine(
                $"  Cleaned {mixedInstanceSessionsCleaned} mixed-instance "
                + $"{(mixedInstanceSessionsCleaned == 1 ? "row" : "rows")}.");
        }
        else if (mixedInstanceSessions.Count > 0)
        {
            console.WriteLine("  Rerun with --clean-mixed-instance to delete these rows.");
        }

        if (unclaimedSessions.Count > 0)
        {
            console.WriteLine();
            console.WriteLine("WARN Unclaimed sessions (informational, no action needed):");

            foreach (var row in unclaimedSessions)
            {
                console.WriteLine($"  {DescribeRow(row)}");
            }
        }

        if (deadGenerationSessions.Count > 0)
        {
            console.WriteLine();
            console.WriteLine(
                "WARN Dead-generation sessions pending reap "
                + "(run `nitro agent session list` to clean up):");

            foreach (var row in deadGenerationSessions)
            {
                console.WriteLine($"  {DescribeRow(row)}");
            }
        }

        return healthy ? ExitCodes.Success : ExitCodes.Error;
    }

    private static DateTimeOffset ParseProcStart(string procStart)
        => DateTimeOffset.Parse(procStart, CultureInfo.InvariantCulture);

    private static AgentSessionDoctorRow ToDoctorRow(SessionDoctorRow row) => new(
        row.Harness, row.SessionId, row.AgentName, row.BindingKind, row.Host, row.Pid,
        ParseProcStart(row.ProcStart), row.WorkspacePath);

    private static string DescribeRow(AgentSessionDoctorRow row)
        => $"{row.Harness} {row.SessionId} host={row.Host} pid={row.Pid}"
            + (row.AgentName is { Length: > 0 } ? $" claimed-by={row.AgentName}" : "");

    /// <summary>
    /// Deletes each mixed-instance row through the same full-generation
    /// predicate every other lifecycle mutation in
    /// <see cref="AgentSessionRegistry"/> uses: a row that changed between
    /// the SELECT that found it and this DELETE (reclaimed under a new
    /// generation, or already cleaned by a concurrent doctor run) simply
    /// matches nothing rather than deleting the wrong generation.
    /// </summary>
    private static async Task<int> CleanMixedInstanceRowsAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        IReadOnlyList<SessionDoctorRow> rows,
        CancellationToken cancellationToken)
    {
        var cleaned = 0;

        foreach (var row in rows)
        {
            var rowsAffected = await connection.ExecuteAsync(
                "DELETE FROM agent_sessions WHERE harness = @harness AND session_id = @sessionId "
                + "AND host = @host AND pid = @pid AND proc_start = @procStart",
                new
                {
                    harness = row.Harness,
                    sessionId = row.SessionId,
                    host = row.Host,
                    pid = row.Pid,
                    procStart = ParseProcStart(row.ProcStart),
                    cancellationToken
                });

            cleaned += rowsAffected;
        }

        return cleaned;
    }

    private static SchemaStatus ClassifySchemaVersion(long version)
    {
        if (version == AgentDatabase.CurrentVersion)
        {
            return SchemaStatus.Current;
        }

        if (version > AgentDatabase.CurrentVersion)
        {
            return SchemaStatus.Newer;
        }

        if (AgentDatabase.IsUpgradableVersion(version))
        {
            return SchemaStatus.Upgradable;
        }

        return SchemaStatus.Unrecognized;
    }

    private static string DescribeSchemaStatus(SchemaStatus status, long version) => status switch
    {
        SchemaStatus.Current => "current",
        SchemaStatus.Upgradable => "upgradable; run `nitro agent init` to migrate",
        SchemaStatus.Newer => $"newer than this CLI supports (up to v{AgentDatabase.CurrentVersion}); update the CLI",
        _ => $"unrecognized (v{version})"
    };

    private enum SchemaStatus
    {
        Current,
        Upgradable,
        Newer,
        Unrecognized
    }

    private static void WriteCheck(
        INitroConsole console,
        string name,
        bool ok,
        IEnumerable<string> problems)
    {
        if (ok)
        {
            console.OkLine(name);
            return;
        }

        console.WriteLine($"FAIL {name}:");

        foreach (var problem in problems)
        {
            console.WriteLine($"  {problem}");
        }
    }

    // Internal, not private: Dapper.AOT's generated interceptors live
    // outside this command and cannot reference a private nested type,
    // mirroring AgentSessionRegistry.AgentSessionRow.
    internal sealed class SessionDoctorRow
    {
        public required string Harness { get; init; }
        public required string SessionId { get; init; }
        public string? AgentName { get; init; }
        public required string BindingKind { get; init; }
        public required string Host { get; init; }
        public required int Pid { get; init; }
        public required string ProcStart { get; init; }
        public required string WorkspacePath { get; init; }
    }

    public sealed record AgentSessionDoctorRow(
        string Harness,
        string SessionId,
        string? AgentName,
        string BindingKind,
        string Host,
        int Pid,
        DateTimeOffset ProcStart,
        string WorkspacePath);

    public sealed record AgentDoctorResult(
        string WorkspacePath,
        long SchemaVersion,
        string SchemaStatus,
        bool SchemaCurrent,
        string? CurrentInstanceId,
        IReadOnlyList<AgentSessionDoctorRow> UnclaimedSessions,
        IReadOnlyList<AgentSessionDoctorRow> DeadGenerationSessions,
        IReadOnlyList<AgentSessionDoctorRow> MixedInstanceSessions,
        int MixedInstanceSessionsCleaned,
        bool Healthy);
}
