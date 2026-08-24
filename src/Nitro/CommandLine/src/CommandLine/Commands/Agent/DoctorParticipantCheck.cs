using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

/// <summary>
/// Read-only, per-harness end-to-end diagnosis: whether this process has a
/// detectable Claude Code, Codex, or Copilot ancestor, whether that
/// harness's hooks are installed and current, whether a matching live
/// participant row exists, and, when it does, that row's binding, role,
/// endpoint, last-heard age, process-scope observability, and recorded
/// harness version compared against a freshly resolved live one. Never
/// mutates anything.
/// </summary>
internal static class DoctorParticipantCheck
{
    public static async Task<DoctorAgentCommand.HarnessParticipantDoctorResult> CheckClaudeAsync(
        IClaudeAncestorSessionResolver ancestorResolver,
        IClaudeHarnessVersionResolver versionResolver,
        IAgentSessionRegistry sessions,
        IProcessInfoProvider processInfoProvider,
        TimeProvider timeProvider,
        string host,
        DoctorAgentCommand.HookHarnessDoctorResult? hooks,
        CancellationToken cancellationToken)
    {
        var ancestor = ancestorResolver.Resolve();

        if (ancestor is null)
        {
            return NoAncestorDetected(AgentSessionHarness.ClaudeCode, hooks);
        }

        var procStart = processInfoProvider.GetStartTicks(ancestor.Pid);

        if (procStart is null)
        {
            return ProcessGone(AgentSessionHarness.ClaudeCode, ancestor.Pid, hooks);
        }

        var liveVersion = versionResolver.Resolve(ancestor.Pid);

        var session = await sessions.FindByGenerationAsync(
            new AgentSessionGeneration(
                AgentSessionHarness.ClaudeCode, ancestor.SessionId, host, ancestor.Pid, procStart),
            cancellationToken);

        return Compose(
            AgentSessionHarness.ClaudeCode, ancestor.Pid, hooks, ambiguous: false, session, liveVersion,
            processInfoProvider, timeProvider);
    }

    public static Task<DoctorAgentCommand.HarnessParticipantDoctorResult> CheckCodexAsync(
        ICodexAncestorSessionResolver ancestorResolver,
        ICodexHarnessVersionResolver versionResolver,
        IAgentSessionRegistry sessions,
        IProcessInfoProvider processInfoProvider,
        TimeProvider timeProvider,
        string host,
        DoctorAgentCommand.HookHarnessDoctorResult? hooks,
        CancellationToken cancellationToken)
    {
        var ancestor = ancestorResolver.Resolve();

        return ancestor is null
            ? Task.FromResult(NoAncestorDetected(AgentSessionHarness.Codex, hooks))
            : CheckByProcessAsync(
                AgentSessionHarness.Codex, ancestor.Pid, hooks, sessions, processInfoProvider, timeProvider,
                host, versionResolver.Resolve, cancellationToken);
    }

    public static Task<DoctorAgentCommand.HarnessParticipantDoctorResult> CheckCopilotAsync(
        ICopilotAncestorSessionResolver ancestorResolver,
        ICopilotHarnessVersionResolver versionResolver,
        IAgentSessionRegistry sessions,
        IProcessInfoProvider processInfoProvider,
        TimeProvider timeProvider,
        string host,
        DoctorAgentCommand.HookHarnessDoctorResult? hooks,
        CancellationToken cancellationToken)
    {
        var ancestor = ancestorResolver.Resolve();

        return ancestor is null
            ? Task.FromResult(NoAncestorDetected(AgentSessionHarness.Copilot, hooks))
            : CheckByProcessAsync(
                AgentSessionHarness.Copilot, ancestor.Pid, hooks, sessions, processInfoProvider, timeProvider,
                host, versionResolver.Resolve, cancellationToken);
    }

    /// <summary>
    /// Resolves a harness's own live session by (host, pid, process-start)
    /// rather than a session id, for a harness whose ancestor process
    /// exposes no session file to read one from directly (Codex, Copilot).
    /// </summary>
    private static async Task<DoctorAgentCommand.HarnessParticipantDoctorResult> CheckByProcessAsync(
        string harness,
        int pid,
        DoctorAgentCommand.HookHarnessDoctorResult? hooks,
        IAgentSessionRegistry sessions,
        IProcessInfoProvider processInfoProvider,
        TimeProvider timeProvider,
        string host,
        Func<string, int, string> resolveLiveVersion,
        CancellationToken cancellationToken)
    {
        var procStart = processInfoProvider.GetStartTicks(pid);

        if (procStart is null)
        {
            return ProcessGone(harness, pid, hooks);
        }

        var candidates = await sessions.FindByProcessAsync(harness, host, pid, procStart, cancellationToken);

        if (candidates.Count > 1)
        {
            return Compose(
                harness, pid, hooks, ambiguous: true, session: null, liveVersion: "",
                processInfoProvider, timeProvider);
        }

        var session = candidates.Count == 1 ? candidates[0] : null;
        var liveVersion = session is not null ? resolveLiveVersion(session.SessionId, pid) : "";

        return Compose(harness, pid, hooks, ambiguous: false, session, liveVersion, processInfoProvider, timeProvider);
    }

    private static DoctorAgentCommand.HarnessParticipantDoctorResult NoAncestorDetected(
        string harness, DoctorAgentCommand.HookHarnessDoctorResult? hooks)
        => new(
            harness, AncestorDetected: false, AncestorPid: null, hooks,
            SessionRowFound: false, SessionAmbiguous: false, SessionId: null, AgentName: null,
            BindingKind: null, Role: null, EndpointKind: null, LastPingResult: null,
            RecordedHarnessVersion: null, LiveHarnessVersion: null, ProcessScopeObservable: null,
            LastHeardSeconds: null, Healthy: true, Remediation: []);

    private static DoctorAgentCommand.HarnessParticipantDoctorResult ProcessGone(
        string harness, int pid, DoctorAgentCommand.HookHarnessDoctorResult? hooks)
        => new(
            harness, AncestorDetected: true, AncestorPid: pid, hooks,
            SessionRowFound: false, SessionAmbiguous: false, SessionId: null, AgentName: null,
            BindingKind: null, Role: null, EndpointKind: null, LastPingResult: null,
            RecordedHarnessVersion: null, LiveHarnessVersion: null, ProcessScopeObservable: null,
            LastHeardSeconds: null, Healthy: false,
            Remediation: [$"Process {pid} for the detected {harness} session is no longer running."]);

    private static DoctorAgentCommand.HarnessParticipantDoctorResult Compose(
        string harness,
        int ancestorPid,
        DoctorAgentCommand.HookHarnessDoctorResult? hooks,
        bool ambiguous,
        AgentSessionRecord? session,
        string liveVersion,
        IProcessInfoProvider processInfoProvider,
        TimeProvider timeProvider)
    {
        var remediation = new List<string>();
        var healthy = true;

        if (hooks is null)
        {
            healthy = false;
            remediation.Add(
                $"Hooks were never installed for {harness}. Run `nitro agent hooks "
                + $"{HooksInstallCommandName(harness)} install`, then start a new {harness} session.");
        }
        else if (!hooks.Consistent)
        {
            healthy = false;
            remediation.Add(
                $"Hooks for {harness} are installed but outdated or inconsistent. Rerun `nitro "
                + $"agent hooks {HooksInstallCommandName(harness)} install`, then start a new "
                + $"{harness} session.");
            remediation.AddRange(hooks.Issues);
        }

        if (ambiguous)
        {
            healthy = false;
            remediation.Add(
                $"Found more than one {harness} session for this process; this reader cannot pick "
                + "one to diagnose.");

            return new DoctorAgentCommand.HarnessParticipantDoctorResult(
                harness, AncestorDetected: true, ancestorPid, hooks,
                SessionRowFound: false, SessionAmbiguous: true, SessionId: null, AgentName: null,
                BindingKind: null, Role: null, EndpointKind: null, LastPingResult: null,
                RecordedHarnessVersion: null, LiveHarnessVersion: NonEmpty(liveVersion),
                ProcessScopeObservable: null, LastHeardSeconds: null, healthy, remediation);
        }

        if (session is null)
        {
            if (hooks is not null)
            {
                healthy = false;
                remediation.Add(
                    $"Hooks are installed, but no {harness} session row was found for this "
                    + "process. Start a new session (the current one may predate the hook "
                    + "install), or it may have already ended.");
            }

            return new DoctorAgentCommand.HarnessParticipantDoctorResult(
                harness, AncestorDetected: true, ancestorPid, hooks,
                SessionRowFound: false, SessionAmbiguous: false, SessionId: null, AgentName: null,
                BindingKind: null, Role: null, EndpointKind: null, LastPingResult: null,
                RecordedHarnessVersion: null, LiveHarnessVersion: NonEmpty(liveVersion),
                ProcessScopeObservable: null, LastHeardSeconds: null, healthy, remediation);
        }

        var observable = processInfoProvider.Observe(
                session.Pid, session.ProcStart, session.ProcStartLegacy, session.ProcessScope)
            != ProcessObservationResult.Unobservable;

        if (!observable)
        {
            healthy = false;
            remediation.Add(
                $"This reader cannot verify the {harness} session's process (a different process "
                + "observation scope than the one that started it). Run doctor from the process "
                + "or PID-namespace scope that started the session.");
        }

        if (session.AgentName is null)
        {
            remediation.Add(
                "This session is not yet bound to an agent identity. Run `nitro agent register` "
                + "to bind it.");
        }
        else if (session.Role.Length == 0)
        {
            remediation.Add(
                "This session has no role yet. Run `nitro agent register --role <role>` to "
                + "promote it.");
        }

        if (session.HarnessVersion.Length == 0)
        {
            remediation.Add($"No {harness} version was recorded for this session.");
        }
        else if (liveVersion.Length > 0 && liveVersion != session.HarnessVersion)
        {
            remediation.Add(
                $"The recorded {harness} version ('{session.HarnessVersion}') does not match the "
                + $"currently running version ('{liveVersion}').");
        }

        var lastHeardSeconds = (timeProvider.GetUtcNow() - session.LastBeatAt).TotalSeconds;

        return new DoctorAgentCommand.HarnessParticipantDoctorResult(
            harness, AncestorDetected: true, ancestorPid, hooks,
            SessionRowFound: true, SessionAmbiguous: false, session.SessionId, session.AgentName,
            session.BindingKind, NonEmpty(session.Role), session.EndpointKind, session.LastPingResult,
            NonEmpty(session.HarnessVersion), NonEmpty(liveVersion), observable, lastHeardSeconds,
            healthy, remediation);
    }

    private static string? NonEmpty(string value) => value.Length > 0 ? value : null;

    /// <summary>
    /// Maps an <see cref="AgentSessionHarness"/> value to the harness name
    /// its <c>hooks</c> subcommand group uses, which differs from the
    /// harness value only for Claude Code (<c>claude-code</c> installs under
    /// <c>claude</c>).
    /// </summary>
    private static string HooksInstallCommandName(string harness) => harness switch
    {
        AgentSessionHarness.ClaudeCode => "claude",
        _ => harness
    };
}
