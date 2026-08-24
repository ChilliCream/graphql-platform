using System.Globalization;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

/// <summary>
/// Diagnostics for the hook and session presence layer. Static and free:
/// schema version, orphaned/unclaimed session rows, dead-generation rows
/// pending reap, mixed-instance rows stranded by an instance id
/// regeneration (with an explicit, opt-in cleanup for those), and, per
/// harness, whether its installed hook entries are current and its sidecar
/// record agrees with what is actually on disk. Explicit opt-in and not
/// free: <c>--probe claude</c> runs a live round-trip (register a scratch
/// actor, claim this process's own live session, send it mail, verify the
/// digest/gate delivery-ledger claims, fire the ping) against a session
/// this process's own Claude Code ancestor provides.
/// </summary>
internal sealed class DoctorAgentCommand : Command
{
    public DoctorAgentCommand() : base("doctor")
    {
        Description = "Check the agent workspace's schema and session presence for problems.";

        Options.Add(Opt<CleanMixedInstanceAgentOption>.Instance);
        Options.Add(Opt<ProbeHarnessDoctorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent doctor", "agent doctor --clean-mixed-instance", "agent doctor --probe claude");

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
        var claudeHooksInstaller = services.GetRequiredService<IClaudeHooksInstallerService>();
        var claudeHooksSidecarStore = services.GetRequiredService<IClaudeHooksSidecarStore>();
        var copilotHooksInstaller = services.GetRequiredService<ICopilotHooksInstallerService>();
        var copilotHooksSidecarStore = services.GetRequiredService<ICopilotHooksSidecarStore>();
        var agentRegistry = services.GetRequiredService<IAgentRegistry>();
        var sessionRegistry = services.GetRequiredService<IAgentSessionRegistry>();
        var mailStore = services.GetRequiredService<IMailStore>();
        var deliveryLedger = services.GetRequiredService<ISessionDeliveryLedger>();
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var cleanMixedInstance = parseResult.GetValue(Opt<CleanMixedInstanceAgentOption>.Instance);
        var probeHarness = parseResult.GetValue(Opt<ProbeHarnessDoctorOption>.Instance);

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
                       workspace_path AS WorkspacePath, last_ping_result AS LastPingResult
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
                           workspace_path AS WorkspacePath, last_ping_result AS LastPingResult
                    FROM agent_sessions
                    WHERE host != @currentInstanceId
                    ORDER BY harness, session_id;
                    """,
                    new { currentInstanceId }))
                    .ToArray();
            }

            mixedInstanceSessions = mixedInstanceRows.Select(ToDoctorRow).ToArray();
        }

        // Hooks/sidecar checks are independent of the agent_sessions schema
        // (they read the harness's own config file and this CLI's sidecar,
        // never the workspace database), so they always run, current schema
        // or not. Each returns null when that harness was never installed
        // here: opting out is not a doctor finding.
        var claudeUserHooks = await DoctorHooksCheck.CheckClaudeAsync(
            claudeHooksInstaller, claudeHooksSidecarStore, HookInstallScopes.User, cancellationToken);
        var claudeProjectHooks = await DoctorHooksCheck.CheckClaudeAsync(
            claudeHooksInstaller, claudeHooksSidecarStore, HookInstallScopes.Project, cancellationToken);
        var copilotHooks = await DoctorHooksCheck.CheckCopilotAsync(
            copilotHooksInstaller, copilotHooksSidecarStore, cancellationToken);

        var hooksConsistent = (claudeUserHooks?.Consistent ?? true)
            && (claudeProjectHooks?.Consistent ?? true)
            && (copilotHooks?.Consistent ?? true);

        ClaudeProbeResult? probe = null;

        if (probeHarness == "claude")
        {
            if (!schemaCurrent)
            {
                throw new ExitException(
                    "`--probe claude` requires the current schema; run `nitro agent init` first.");
            }

            probe = await new ClaudeRoundTripProbe(agentRegistry, sessionRegistry, mailStore, deliveryLedger, timeProvider)
                .RunAsync(cancellationToken);
        }

        var healthy = schemaCurrent && mixedInstanceSessions.Count == 0 && hooksConsistent
            && (probe is null || probe.Success);

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
                claudeUserHooks,
                claudeProjectHooks,
                copilotHooks,
                probe,
                healthy)));

            return healthy ? ExitCodes.Success : ExitCodes.Error;
        }

        console.WriteLine($"Workspace: {workspaceDirectory}");
        console.WriteLine($"Schema: v{version} ({DescribeSchemaStatus(schemaStatus, version)})");
        console.WriteLine();

        WriteCheck(console, "Schema version", schemaCurrent, schemaCurrent
            ? []
            : [DescribeSchemaStatus(schemaStatus, version)]);

        WriteHooksCheck(console, "Claude hooks (user)", claudeUserHooks);
        WriteHooksCheck(console, "Claude hooks (project)", claudeProjectHooks);
        WriteHooksCheck(console, "Copilot hooks", copilotHooks);

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

        if (probe is not null)
        {
            console.WriteLine();
            WriteCheck(
                console,
                "Round-trip probe (claude)",
                probe.Success,
                probe.Success
                    ? []
                    : ["Digest or gate delivery-ledger claim did not reserve the probe message."]);

            console.WriteLine($"  Scratch actor: {probe.ScratchActor.EscapeMarkup()}");
            console.WriteLine(
                $"  Session: {probe.Harness.EscapeMarkup()} {probe.SessionId.EscapeMarkup()} "
                + $"endpoint={probe.EndpointKind.EscapeMarkup()}");
            console.WriteLine(
                $"  Ledger: digest={(probe.DigestLedgerClaimed ? "claimed" : "NOT claimed")}, "
                + $"gate={(probe.GateLedgerClaimed ? "claimed" : "NOT claimed")}");
            console.WriteLine($"  Ping: {probe.PingResult.EscapeMarkup()}");
        }

        return healthy ? ExitCodes.Success : ExitCodes.Error;
    }

    private static void WriteHooksCheck(INitroConsole console, string name, HookHarnessDoctorResult? result)
    {
        if (result is null)
        {
            return;
        }

        WriteCheck(console, name, result.Consistent, result.Issues);
    }

    private static DateTimeOffset ParseProcStart(string procStart)
        => DateTimeOffset.Parse(procStart, CultureInfo.InvariantCulture);

    private static AgentSessionDoctorRow ToDoctorRow(SessionDoctorRow row) => new(
        row.Harness, row.SessionId, row.AgentName, row.BindingKind, row.Host, row.Pid,
        ParseProcStart(row.ProcStart), row.WorkspacePath, row.LastPingResult);

    // Distinguishes "no endpoint to ping at all" (no last_ping_result ever
    // written) from "an endpoint the notifier has no transport for"
    // (last_ping_result 'unsupported', e.g. claude-peer) from an ordinary
    // ping outcome, the same diagnostic signal `session list` surfaces.
    private static string DescribeRow(AgentSessionDoctorRow row)
        => $"{row.Harness} {row.SessionId} host={row.Host} pid={row.Pid}"
            + (row.AgentName is { Length: > 0 } ? $" claimed-by={row.AgentName}" : "")
            + (row.LastPingResult is { Length: > 0 } lastPingResult ? $" last-ping={lastPingResult}" : "");

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
        public string? LastPingResult { get; init; }
    }

    public sealed record AgentSessionDoctorRow(
        string Harness,
        string SessionId,
        string? AgentName,
        string BindingKind,
        string Host,
        int Pid,
        DateTimeOffset ProcStart,
        string WorkspacePath,
        string? LastPingResult);

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
        HookHarnessDoctorResult? ClaudeUserHooks,
        HookHarnessDoctorResult? ClaudeProjectHooks,
        HookHarnessDoctorResult? CopilotHooks,
        ClaudeProbeResult? Probe,
        bool Healthy);

    /// <summary>
    /// One managed hook event's <see cref="HookStatusOutcome"/>, by
    /// name, as reported by <see cref="DoctorHooksCheck"/>.
    /// </summary>
    public sealed record HookEventDoctorResult(string Event, string Outcome);

    /// <summary>
    /// A harness's hooks doctor check: every managed event's status, whether
    /// the sidecar record agrees with what is actually installed
    /// (<see cref="Consistent"/>), and, when it does not, the specific
    /// issues found. Returned only when this harness has some Nitro-managed
    /// state to check; a harness that was never installed here reports no
    /// result at all rather than an empty one.
    /// </summary>
    public sealed record HookHarnessDoctorResult(
        string Path,
        IReadOnlyList<HookEventDoctorResult> Events,
        bool Consistent,
        IReadOnlyList<string> Issues);
}
