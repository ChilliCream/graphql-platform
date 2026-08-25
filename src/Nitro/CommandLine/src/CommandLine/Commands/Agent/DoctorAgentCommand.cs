using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
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
        var codexHooksInstaller = services.GetRequiredService<ICodexHooksInstallerService>();
        var codexHooksSidecarStore = services.GetRequiredService<ICodexHooksSidecarStore>();
        var claudeAncestorResolver = services.GetRequiredService<IClaudeAncestorSessionResolver>();
        var codexAncestorResolver = services.GetRequiredService<ICodexAncestorSessionResolver>();
        var copilotAncestorResolver = services.GetRequiredService<ICopilotAncestorSessionResolver>();
        var claudeVersionResolver = services.GetRequiredService<IClaudeHarnessVersionResolver>();
        var codexVersionResolver = services.GetRequiredService<ICodexHarnessVersionResolver>();
        var copilotVersionResolver = services.GetRequiredService<ICopilotHarnessVersionResolver>();
        var agentRegistry = services.GetRequiredService<IAgentRegistry>();
        var sessionRegistry = services.GetRequiredService<IAgentSessionRegistry>();
        var mailStore = services.GetRequiredService<IMailStore>();
        var deliveryLedger = services.GetRequiredService<ISessionDeliveryLedger>();
        var pingSessionExecutor = services.GetRequiredService<IPingSessionExecutor>();
        var pingLeaseStore = services.GetRequiredService<IPingLeaseStore>();
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
        var mailWake = DoctorMailWakeCheck.ForSchemaNotCurrent(version);

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
                       workspace_path AS WorkspacePath, last_ping_result AS LastPingResult,
                       process_scope AS ProcessScope, proc_start_legacy AS ProcStartLegacy
                FROM agent_sessions
                ORDER BY harness, session_id;
                """))
                .ToArray();

            unclaimedSessions = rows
                .Where(row => row.BindingKind == AgentSessionBindingKind.None)
                .Select(ToDoctorRow)
                .ToArray();

            // Observe (not the raw IsAlive check) so a row this reader
            // cannot verify (typically a different PID namespace than the
            // row's writer recorded) is never reported as dead-pending-reap:
            // ReapAsync will never delete such a row either.
            deadGenerationSessions = rows
                .Where(row => row.Host == currentInstanceId
                    && processInfoProvider.Observe(row.Pid, row.ProcStart, row.ProcStartLegacy, row.ProcessScope)
                        == ProcessObservationResult.Dead)
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
                           workspace_path AS WorkspacePath, last_ping_result AS LastPingResult,
                           process_scope AS ProcessScope, proc_start_legacy AS ProcStartLegacy
                    FROM agent_sessions
                    WHERE host != @currentInstanceId
                    ORDER BY harness, session_id;
                    """,
                    new { currentInstanceId }))
                    .ToArray();
            }

            mixedInstanceSessions = mixedInstanceRows.Select(ToDoctorRow).ToArray();

            mailWake = await DoctorMailWakeCheck.CheckAsync(
                connection, currentInstanceId!, timeProvider.GetUtcNow(), cancellationToken);
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
        var codexHooks = await DoctorHooksCheck.CheckCodexAsync(
            codexHooksInstaller, codexHooksSidecarStore, cancellationToken);

        var hooksConsistent = (claudeUserHooks?.Consistent ?? true)
            && (claudeProjectHooks?.Consistent ?? true)
            && (copilotHooks?.Consistent ?? true)
            && (codexHooks?.Consistent ?? true);

        // The end-to-end participant chain (ancestor, hooks, live session
        // row, binding, role, endpoint, heartbeat, process-scope
        // observability) needs the agent_sessions table, so it is gated on
        // schemaCurrent the same as the session checks above.
        IReadOnlyList<HarnessParticipantDoctorResult> participants = [];

        if (schemaCurrent)
        {
            participants =
            [
                await DoctorParticipantCheck.CheckClaudeAsync(
                    claudeAncestorResolver, claudeVersionResolver, sessionRegistry, processInfoProvider,
                    timeProvider, currentInstanceId!, claudeProjectHooks ?? claudeUserHooks, cancellationToken),
                await DoctorParticipantCheck.CheckCodexAsync(
                    codexAncestorResolver, codexVersionResolver, sessionRegistry, processInfoProvider,
                    timeProvider, currentInstanceId!, codexHooks, cancellationToken),
                await DoctorParticipantCheck.CheckCopilotAsync(
                    copilotAncestorResolver, copilotVersionResolver, sessionRegistry, processInfoProvider,
                    timeProvider, currentInstanceId!, copilotHooks, cancellationToken)
            ];
        }

        var participantsHealthy = participants.All(p => p.Healthy);

        ClaudeProbeResult? probe = null;

        if (probeHarness == "claude")
        {
            if (!schemaCurrent)
            {
                throw new ExitException(
                    "`--probe claude` requires the current schema; run `nitro agent init` first.");
            }

            probe = await new ClaudeRoundTripProbe(
                agentRegistry,
                sessionRegistry,
                mailStore,
                deliveryLedger,
                pingSessionExecutor,
                pingLeaseStore,
                timeProvider)
                .RunAsync(cancellationToken);
        }

        var healthy = schemaCurrent && mixedInstanceSessions.Count == 0 && hooksConsistent
            && participantsHealthy && mailWake.Healthy && (probe is null || probe.Success);

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
                codexHooks,
                participants,
                mailWake,
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
        WriteHooksCheck(console, "Codex hooks", codexHooks);

        WriteMailWakeCheck(console, mailWake);

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

        foreach (var participant in participants)
        {
            console.WriteLine();
            WriteParticipantCheck(console, participant);
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

    /// <summary>
    /// Prints the mail-wake daemon's read-only diagnosis. Unlike a plain
    /// <see cref="WriteCheck"/> call, the leader and work summary lines print
    /// unconditionally, healthy or not, the same unconditional-detail
    /// convention <see cref="WriteParticipantCheck"/> uses for its Session
    /// and Version lines: this is a status report, not just a pass/fail
    /// check. Skipped entirely (beyond the check line itself) when the
    /// schema does not support the mail-wake tables, since there is nothing
    /// further to report.
    /// </summary>
    private static void WriteMailWakeCheck(INitroConsole console, MailWakeDoctorResult mailWake)
    {
        WriteCheck(console, "Mail-wake", mailWake.Healthy, mailWake.Remediation);

        if (!mailWake.SchemaCurrent)
        {
            return;
        }

        console.WriteLine(
            $"  Leader: {mailWake.LeaderState}"
            + (mailWake.Epoch is { } epoch
                ? $" (epoch={epoch}, {DescribeLease(mailWake.LeaderState, mailWake.LeaseExpiresInSeconds)})"
                : ""));
        console.WriteLine(
            $"  Work: pending={mailWake.PendingActorCount} accepted={mailWake.AcceptedActorCount} "
            + $"deferred={mailWake.DeferredActorCount}"
            + (mailWake.OldestPendingAgeSeconds is { } age ? $", oldest due {age:0}s ago" : ""));

        if (mailWake.LastError is { Length: > 0 } lastError)
        {
            console.WriteLine($"  Last daemon error: {lastError.EscapeMarkup()}");
        }
    }

    private static string DescribeLease(string leaderState, double? leaseExpiresInSeconds) =>
        leaderState == "ready"
            ? $"lease expires in {leaseExpiresInSeconds:0}s"
            : $"lease expired {-leaseExpiresInSeconds:0}s ago";

    /// <summary>
    /// Prints one harness's end-to-end participant diagnosis. A harness
    /// with no detected ancestor is not run under right now, so it is noted
    /// and skipped rather than reported as a failure.
    /// </summary>
    private static void WriteParticipantCheck(INitroConsole console, HarnessParticipantDoctorResult result)
    {
        if (!result.AncestorDetected)
        {
            console.WriteLine($"- {result.Harness}: no ancestor process detected here, skipped.");
            return;
        }

        if (result.Healthy)
        {
            console.OkLine($"{result.Harness} participant");
        }
        else
        {
            console.WriteLine($"FAIL {result.Harness} participant:");
        }

        foreach (var note in result.Remediation)
        {
            console.WriteLine($"  {note}");
        }

        if (result.SessionRowFound)
        {
            console.WriteLine(
                $"  Session: {result.SessionId} bound={result.AgentName ?? "(none)"} "
                + $"role={result.Role ?? "(none)"} endpoint={result.EndpointKind} "
                + $"last-ping={result.LastPingResult ?? "(none)"}");
            console.WriteLine(
                $"  Version: recorded={result.RecordedHarnessVersion ?? "(none)"} "
                + $"live={result.LiveHarnessVersion ?? "(unknown)"}, last heard "
                + $"{result.LastHeardSeconds:0}s ago, process-scope "
                + $"observable={result.ProcessScopeObservable}");
        }
    }

    private static AgentSessionDoctorRow ToDoctorRow(SessionDoctorRow row) => new(
        row.Harness, row.SessionId, row.AgentName, row.BindingKind, row.Host, row.Pid,
        row.ProcStart, row.WorkspacePath, row.LastPingResult, row.ProcessScope);

    // Distinguishes "no endpoint to ping at all" (no last_ping_result ever
    // written) from "an endpoint the notifier has no transport for"
    // (last_ping_result 'unsupported', e.g. copilot-extension) from an ordinary
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
                    procStart = row.ProcStart,
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
        public required string ProcessScope { get; init; }
        public required bool ProcStartLegacy { get; init; }
    }

    /// <summary>
    /// Part of the public shape of <c>agent doctor --output json</c>.
    /// Schema v6 breaking change: <see cref="ProcStart"/> is now the
    /// process's raw kernel start-tick count as a digit string (see
    /// <see cref="ChilliCream.Nitro.CommandLine.Services.Workspace.ProcStat.ReadStartTicks(int)"/>),
    /// not a DateTimeOffset.
    /// </summary>
    public sealed record AgentSessionDoctorRow(
        string Harness,
        string SessionId,
        string? AgentName,
        string BindingKind,
        string Host,
        int Pid,
        string ProcStart,
        string WorkspacePath,
        string? LastPingResult,
        string ProcessScope);

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
        HookHarnessDoctorResult? CodexHooks,
        IReadOnlyList<HarnessParticipantDoctorResult> Participants,
        MailWakeDoctorResult MailWake,
        ClaudeProbeResult? Probe,
        bool Healthy);

    /// <summary>
    /// One harness's end-to-end participant diagnosis, as
    /// <see cref="DoctorParticipantCheck"/> returns it. <see cref="Healthy"/>
    /// is false only for a genuinely actionable problem (hooks missing or
    /// outdated, no session row despite installed hooks, an ambiguous match,
    /// or an unobservable process); an unbound or roleless session that is
    /// otherwise fine is reported in <see cref="Remediation"/> without
    /// failing health.
    /// </summary>
    public sealed record HarnessParticipantDoctorResult(
        string Harness,
        bool AncestorDetected,
        int? AncestorPid,
        HookHarnessDoctorResult? Hooks,
        bool SessionRowFound,
        bool SessionAmbiguous,
        string? SessionId,
        string? AgentName,
        string? BindingKind,
        string? Role,
        string? EndpointKind,
        string? LastPingResult,
        string? RecordedHarnessVersion,
        string? LiveHarnessVersion,
        bool? ProcessScopeObservable,
        double? LastHeardSeconds,
        bool Healthy,
        IReadOnlyList<string> Remediation);

    /// <summary>
    /// Part of the public shape of <c>agent doctor --output json</c>. The
    /// mail-wake daemon's read-only diagnosis for this Nitro instance, from
    /// <see cref="DoctorMailWakeCheck"/>. <see cref="LeaderState"/> is
    /// <c>"unknown"</c> when <see cref="SchemaCurrent"/> is false (the
    /// mail-wake tables were never queried), <c>"none"</c> when no
    /// <c>mail_wake_daemons</c> row exists for this instance, <c>"ready"</c>
    /// when one exists with an unexpired lease, and <c>"expired"</c>
    /// otherwise. <see cref="Epoch"/> and <see cref="LeaseExpiresInSeconds"/>
    /// are null exactly when <see cref="LeaderState"/> is <c>"none"</c> or
    /// <c>"unknown"</c>; <see cref="LeaseExpiresInSeconds"/> is positive
    /// while the lease is still live and negative once it has expired. Never
    /// carries the leader's owner id. <see cref="PendingActorCount"/> counts
    /// actors with due, unclaimed generation work, <see cref="AcceptedActorCount"/>
    /// those with a live claimed batch already working it, and
    /// <see cref="DeferredActorCount"/> those whose retry is scheduled for
    /// later; <see cref="OldestPendingAgeSeconds"/> is the age of the oldest
    /// due-but-unsettled generation across the pending and accepted actors
    /// combined. <see cref="AccessDeniedPendingTargets"/> counts only targets
    /// on the actor's latest, still-unsettled batch durably stuck on a
    /// Claude access-denied handoff, the read-only signal for a degraded
    /// dashboard. <see cref="Healthy"/> is false for a schema
    /// mismatch, any access-denied target, or pending work with no ready
    /// leader to claim it.
    /// </summary>
    public sealed record MailWakeDoctorResult(
        bool SchemaCurrent,
        string LeaderState,
        long? Epoch,
        double? LeaseExpiresInSeconds,
        string? LastError,
        int AccessDeniedPendingTargets,
        int PendingActorCount,
        int AcceptedActorCount,
        int DeferredActorCount,
        double? OldestPendingAgeSeconds,
        bool Healthy,
        IReadOnlyList<string> Remediation);

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
