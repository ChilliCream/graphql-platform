using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

internal sealed class RegisterAgentCommand : Command
{
    public RegisterAgentCommand() : base("register")
    {
        Description = "Ensure the current session has an actor, or update its actor and role.";

        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<RoleAgentOption>.Instance);
        Options.Add(Opt<ForceActorTakeoverOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent register", "agent register --role \"backend\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var sessions = services.GetRequiredService<IAgentSessionRegistry>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var processInfoProvider = services.GetRequiredService<IProcessInfoProvider>();
        var claudeAncestorResolver = services.GetRequiredService<IClaudeAncestorSessionResolver>();
        var codexAncestorResolver = services.GetRequiredService<ICodexAncestorSessionResolver>();
        var copilotAncestorResolver = services.GetRequiredService<ICopilotAncestorSessionResolver>();
        var instanceIdProvider = services.GetRequiredService<INitroInstanceIdProvider>();
        var globalConfigDirectoryProvider = services.GetRequiredService<IGlobalConfigDirectoryProvider>();
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var actorGiven = parseResult.GetResult(Opt<MailActorOption>.Instance) is { Implicit: false };
        var roleGiven = parseResult.GetResult(Opt<RoleAgentOption>.Instance) is { Implicit: false };
        var actor = parseResult.GetValue(Opt<MailActorOption>.Instance);
        var role = parseResult.GetValue(Opt<RoleAgentOption>.Instance);
        var force = parseResult.GetValue(Opt<ForceActorTakeoverOption>.Instance);

        if (force && !actorGiven)
        {
            throw new ExitException("Option '--force' requires '--actor'.");
        }

        var resolution = await TryResolveGenerationAsync(
            fileSystem,
            processInfoProvider,
            claudeAncestorResolver,
            codexAncestorResolver,
            copilotAncestorResolver,
            instanceIdProvider,
            globalConfigDirectoryProvider,
            environmentVariableProvider,
            sessions,
            cancellationToken);

        if (resolution.Generation is null)
        {
            throw new ExitException(
                "Could not identify the current Claude, Codex, or Copilot session: "
                + $"{resolution.NoLiveBindingReason}. Install Nitro hooks and retry from that session.");
        }

        var result = await sessions.RegisterAsync(
            resolution.Generation, actor, actorGiven, role, roleGiven, force, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(result)));
            return ExitCodes.Success;
        }

        console.OkLine(
            result.Session.Role.Length > 0
                ? $"Actor '{result.Agent.Name.EscapeMarkup()}', role '{result.Session.Role.EscapeMarkup()}'."
                : $"Actor '{result.Agent.Name.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }

    /// <summary>
    /// The outcome of <see cref="TryResolveGenerationAsync"/>: either the
    /// current live generation or the reason it could not be identified.
    /// </summary>
    private readonly record struct GenerationResolution(AgentSessionGeneration? Generation, string? NoLiveBindingReason)
    {
        public static GenerationResolution Bound(AgentSessionGeneration generation) => new(generation, null);

        public static GenerationResolution Unbound(string reason) => new(null, reason);
    }

    /// <summary>
    /// Resolves this process's own live harness session across Claude Code,
    /// Codex, and Copilot, in that order: Claude's ancestor session file
    /// gives its session id directly. A row already recorded for that
    /// session id is reused via its own recorded process generation when the
    /// ancestor's pid still matches it (recovering a row whose recomputed
    /// proc_start no longer agrees with what was recorded, for example a
    /// legacy wall-clock value), and a row missing entirely is bootstrapped
    /// on the spot (the SessionStart hook may never have fired for it); Codex
    /// and Copilot give only an ancestor pid, so the session already recorded
    /// for that exact (host, pid, process-start) is looked up instead. When no Codex ancestor pid can be walked
    /// (a sandboxed invocation, whose <c>/proc</c> ancestry does not reach
    /// the real Codex process), the authoritative
    /// <c>CODEX_SESSION_ID</c>/<c>CODEX_THREAD_ID</c> launch environment
    /// resolves the session by id instead. Returns an unbound resolution,
    /// carrying the reason, when no harness context is found at all, or when
    /// an authoritative session id has no matching row. Throws <see cref="ExitException"/> for the unsafe
    /// contradictions that remain real problems rather than a missing-session
    /// no-op: this process's own workspace disagreeing with the session's,
    /// more than one candidate row for the same process identity, or a
    /// detected ancestor process that is no longer running.
    /// </summary>
    private static async Task<GenerationResolution> TryResolveGenerationAsync(
        IFileSystem fileSystem,
        IProcessInfoProvider processInfoProvider,
        IClaudeAncestorSessionResolver claudeAncestorResolver,
        ICodexAncestorSessionResolver codexAncestorResolver,
        ICopilotAncestorSessionResolver copilotAncestorResolver,
        INitroInstanceIdProvider instanceIdProvider,
        IGlobalConfigDirectoryProvider globalConfigDirectoryProvider,
        IEnvironmentVariableProvider environmentVariableProvider,
        IAgentSessionRegistry sessions,
        CancellationToken cancellationToken)
    {
        var claudeAncestor = claudeAncestorResolver.Resolve();
        var codexAncestor = claudeAncestor is null ? codexAncestorResolver.Resolve() : null;
        var codexSessionId = claudeAncestor is null && codexAncestor is null
            ? ResolveCodexEnvSessionId(environmentVariableProvider)
            : null;
        var copilotAncestor = claudeAncestor is null && codexAncestor is null && codexSessionId is null
            ? copilotAncestorResolver.Resolve()
            : null;

        if (claudeAncestor is null && codexAncestor is null && codexSessionId is null && copilotAncestor is null)
        {
            return GenerationResolution.Unbound("no harness process or session id detected");
        }

        var cwd = fileSystem.GetCurrentDirectory();
        var cwdWorkspacePath = AgentWorkspace.Find(fileSystem, cwd)
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        var host = await instanceIdProvider.GetIdAsync(
            globalConfigDirectoryProvider.GetDirectory(), cancellationToken);

        if (claudeAncestor is not null)
        {
            var procStart = processInfoProvider.GetStartTicks(claudeAncestor.Pid)
                ?? throw new ExitException(
                    $"Process {claudeAncestor.Pid} for the detected Claude Code session is no longer running.");

            var ancestorWorkspacePath = AgentWorkspace.Find(fileSystem, claudeAncestor.Cwd)
                ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

            RequireMatchingWorkspace(cwdWorkspacePath, ancestorWorkspacePath);

            var generation = new AgentSessionGeneration(
                AgentSessionHarness.ClaudeCode, claudeAncestor.SessionId, host, claudeAncestor.Pid, procStart);

            // A current Claude Code session whose SessionStart hook never
            // fired (hooks not installed, or a race with this very command)
            // has no row yet: bootstrap it the same way SessionStart itself
            // would, so this one register call still creates and binds it
            // instead of requiring a separate SessionStart first. StartAsync
            // itself reconciles onto a provisional row already bound to this
            // exact process generation, if one exists, instead of creating a
            // second participant for it.
            if (await sessions.FindByGenerationAsync(generation, cancellationToken) is null)
            {
                // The recomputed proc_start can disagree with what a row
                // already recorded for this same session id, most often a
                // legacy wall-clock value from before proc_start moved to
                // raw ticks. Reusing the row's own recorded generation
                // recovers it instead of driving StartAsync's
                // generation-change branch, which would treat this as a
                // brand new process and reset the row's ledger, budget, and
                // heartbeat state.
                var recordedRow = await sessions.FindBySessionIdAsync(
                    AgentSessionHarness.ClaudeCode, host, claudeAncestor.SessionId, cancellationToken);

                if (recordedRow is not null && recordedRow.Pid == claudeAncestor.Pid)
                {
                    RequireMatchingWorkspace(cwdWorkspacePath, recordedRow.WorkspacePath);

                    return GenerationResolution.Bound(
                        new AgentSessionGeneration(
                            AgentSessionHarness.ClaudeCode, claudeAncestor.SessionId, host,
                            recordedRow.Pid, recordedRow.ProcStart));
                }

                var (endpointKind, endpointAddr) = EndpointAddress.IsValid(claudeAncestor.Name)
                    ? (AgentSessionEndpointKind.ClaudePeer, claudeAncestor.Name)
                    : (AgentSessionEndpointKind.None, string.Empty);

                await sessions.StartAsync(
                    generation, claudeAncestor.Cwd, ancestorWorkspacePath, endpointKind, endpointAddr,
                    envActor: null, cancellationToken);
            }

            return GenerationResolution.Bound(generation);
        }

        if (codexAncestor is not null)
        {
            return GenerationResolution.Bound(
                await ResolveByProcessAsync(
                    AgentSessionHarness.Codex, codexAncestor.Pid, host, cwdWorkspacePath,
                    processInfoProvider, sessions, cancellationToken));
        }

        if (codexSessionId is not null)
        {
            var generation = await ResolveBySessionIdAsync(
                AgentSessionHarness.Codex, codexSessionId, host, cwdWorkspacePath, sessions, cancellationToken);

            return generation is not null
                ? GenerationResolution.Bound(generation)
                : GenerationResolution.Unbound(
                    $"CODEX_SESSION_ID '{codexSessionId}' has no live session row on this host");
        }

        var copilotProcessStart = processInfoProvider.GetStartTicks(copilotAncestor!.Pid)
            ?? throw new ExitException(
                $"Process {copilotAncestor.Pid} for the detected Copilot session is no longer running.");
        var copilotSessionId = $"generated:{host}:{copilotAncestor.Pid}:{copilotProcessStart}";
        var copilotGeneration = new AgentSessionGeneration(
            AgentSessionHarness.Copilot,
            copilotSessionId,
            host,
            copilotAncestor.Pid,
            copilotProcessStart);
        var copilotRow = await sessions.FindByGenerationAsync(copilotGeneration, cancellationToken);

        if (copilotRow is null)
        {
            await sessions.StartAsync(
                copilotGeneration,
                cwd,
                cwdWorkspacePath,
                AgentSessionEndpointKind.CopilotExtension,
                "mail-watch",
                envActor: null,
                cancellationToken);
        }

        return GenerationResolution.Bound(copilotGeneration);
    }

    /// <summary>
    /// Resolves the authoritative Codex session id from its launch
    /// environment (<c>CODEX_SESSION_ID</c>, falling back to
    /// <c>CODEX_THREAD_ID</c>), for a sandboxed Codex invocation whose
    /// <c>/proc</c> ancestry cannot be walked to find the real Codex process.
    /// Returns null when neither is set.
    /// </summary>
    internal static string? ResolveCodexEnvSessionId(IEnvironmentVariableProvider environmentVariables)
        => environmentVariables.GetEnvironmentVariable("CODEX_SESSION_ID")
            ?? environmentVariables.GetEnvironmentVariable("CODEX_THREAD_ID");

    /// <summary>
    /// Resolves a harness's own live session by its session id alone (no
    /// process identity to walk to), reading the (pid, proc_start) an
    /// earlier, authoritative SessionStart already recorded for it. Returns
    /// null when no row is recorded for that session id.
    /// </summary>
    private static async Task<AgentSessionGeneration?> ResolveBySessionIdAsync(
        string harness,
        string sessionId,
        string host,
        string cwdWorkspacePath,
        IAgentSessionRegistry sessions,
        CancellationToken cancellationToken)
    {
        var row = await sessions.FindBySessionIdAsync(harness, host, sessionId, cancellationToken);

        if (row is null)
        {
            return null;
        }

        RequireMatchingWorkspace(cwdWorkspacePath, row.WorkspacePath);

        return new AgentSessionGeneration(harness, sessionId, host, row.Pid, row.ProcStart);
    }

    /// <summary>
    /// Resolves a harness's own live session by (host, pid, process-start)
    /// rather than a session id, for a harness whose ancestor process
    /// exposes no session file to read one from directly. A hook-created live
    /// row is required so a process alone can never create a second identity.
    /// </summary>
    private static async Task<AgentSessionGeneration> ResolveByProcessAsync(
        string harness,
        int pid,
        string host,
        string cwdWorkspacePath,
        IProcessInfoProvider processInfoProvider,
        IAgentSessionRegistry sessions,
        CancellationToken cancellationToken)
    {
        var procStart = processInfoProvider.GetStartTicks(pid)
            ?? throw new ExitException($"Process {pid} for the detected {harness} session is no longer running.");

        var candidates = await sessions.FindByProcessAsync(harness, host, pid, procStart, cancellationToken);

        if (candidates.Count == 0)
        {
            throw new ExitException(
                $"No authoritative {harness} session is registered for pid {pid}. "
                + $"Run `nitro agent hooks {HooksInstallCommandName(harness)} install` and start a new session.");
        }

        if (candidates.Count > 1)
        {
            throw new ExitException(
                $"Found {candidates.Count} ambiguous {harness} sessions for pid {pid} on this host.");
        }

        RequireMatchingWorkspace(cwdWorkspacePath, candidates[0].WorkspacePath);

        return new AgentSessionGeneration(harness, candidates[0].SessionId, host, pid, procStart);
    }

    private static void RequireMatchingWorkspace(string cwdWorkspacePath, string sessionWorkspacePath)
    {
        if (cwdWorkspacePath != sessionWorkspacePath)
        {
            throw new ExitException(
                $"This process's workspace ('{cwdWorkspacePath}') does not match the session's "
                + $"workspace ('{sessionWorkspacePath}'). Run `nitro agent register` from the session's "
                + "workspace.");
        }
    }

    private static string HooksInstallCommandName(string harness)
        => harness == AgentSessionHarness.ClaudeCode ? "claude" : harness;

    private static AgentRegisterResult ToResult(AgentSessionRegisterResult result) => new(
        result.Agent.Name,
        result.Session.Role,
        result.Session.Harness,
        result.Session.SessionId,
        result.Session.HarnessVersion,
        result.Changed,
        Connected: true);

    public sealed record AgentRegisterResult(
        string Actor,
        string Role,
        string Harness,
        string SessionId,
        string HarnessVersion,
        bool Changed,
        bool Connected);
}
