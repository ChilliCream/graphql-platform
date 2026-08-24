using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Session.Options;
using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
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
        Description = "Register the resolved actor as an agent, with an optional role. "
            + "--actor is per invocation; set NITRO_MAIL_ACTOR to persist an identity.";

        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<RoleAgentOption>.Instance);
        Options.Add(Opt<ClientAgentOption>.Instance);
        Options.Add(Opt<ForceRebindSessionOption>.Instance);
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
        var registry = services.GetRequiredService<IAgentRegistry>();
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

        var actor = MailActor.Resolve(
            parseResult.GetValue(Opt<MailActorOption>.Instance), environmentVariableProvider);
        var role = parseResult.GetValue(Opt<RoleAgentOption>.Instance) ?? "";
        var client = parseResult.GetValue(Opt<ClientAgentOption>.Instance)
            ?? DetectClient(environmentVariableProvider)
            ?? "";
        var forceRebind = parseResult.GetValue(Opt<ForceRebindSessionOption>.Instance);

        var generation = await TryResolveGenerationAsync(
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

        if (generation is null)
        {
            // No Claude Code, Codex, or Copilot ancestor detected: the
            // original identity-only registration this command has always
            // supported outside a harness, unchanged.
            var agent = await registry.RegisterAsync(actor, role, client, cancellationToken);

            if (!console.IsHumanReadable)
            {
                resultHolder.SetResult(
                    new ObjectResult(
                        new AgentRegisterResult(
                            agent.Name, agent.Role, agent.Client, agent.RegisteredAt, agent.LastSeenAt,
                            Harness: "", SessionId: "", HarnessVersion: "", BindingKind: "", Changed: false)));
                return ExitCodes.Success;
            }

            console.OkLine(
                agent.Role.Length > 0
                    ? $"Registered '{agent.Name.EscapeMarkup()}' as '{agent.Role.EscapeMarkup()}'."
                    : $"Registered '{agent.Name.EscapeMarkup()}'.");

            return ExitCodes.Success;
        }

        var result = await sessions.RegisterAsync(
            generation, actor, role, client, forceRebind, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(result)));
            return ExitCodes.Success;
        }

        console.OkLine(
            result.Session.Role.Length > 0
                ? $"Registered '{result.Agent.Name.EscapeMarkup()}' as '{result.Session.Role.EscapeMarkup()}', "
                    + $"bound to {result.Session.Harness.EscapeMarkup()} session "
                    + $"'{result.Session.SessionId.EscapeMarkup()}'."
                : $"Registered '{result.Agent.Name.EscapeMarkup()}', bound to "
                    + $"{result.Session.Harness.EscapeMarkup()} session '{result.Session.SessionId.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }

    /// <summary>
    /// Resolves this process's own live harness session across Claude Code,
    /// Codex, and Copilot, in that order: Claude's ancestor session file
    /// gives its session id directly, and a row missing for it is bootstrapped
    /// on the spot (the SessionStart hook may never have fired for it); Codex
    /// and Copilot give only an ancestor pid, so the session already recorded
    /// for that exact (host, pid, process-start) is looked up instead. When
    /// no Codex ancestor pid can be walked (a sandboxed invocation, whose
    /// <c>/proc</c> ancestry does not reach the real Codex process), the
    /// authoritative <c>CODEX_SESSION_ID</c>/<c>CODEX_THREAD_ID</c> launch
    /// environment resolves the session by id instead. Returns null when no
    /// harness context is found at all (register then falls back to
    /// identity-only registration). Throws <see cref="ExitException"/> when a
    /// harness context IS found but its process is no longer running, its
    /// session cannot be resolved to exactly one row, or this process's own
    /// workspace disagrees with the session's: a detected harness context
    /// that fails to resolve is a real problem, never a silent fallback to
    /// identity-only success.
    /// </summary>
    private static async Task<AgentSessionGeneration?> TryResolveGenerationAsync(
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
            return null;
        }

        var cwdWorkspacePath = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
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
            // instead of requiring a separate SessionStart first.
            if (await sessions.FindByGenerationAsync(generation, cancellationToken) is null)
            {
                var (endpointKind, endpointAddr) = EndpointAddress.IsValid(claudeAncestor.Name)
                    ? (AgentSessionEndpointKind.ClaudePeer, claudeAncestor.Name)
                    : (AgentSessionEndpointKind.None, string.Empty);

                await sessions.StartAsync(
                    generation, claudeAncestor.Cwd, ancestorWorkspacePath, endpointKind, endpointAddr,
                    envActor: null, cancellationToken);
            }

            return generation;
        }

        if (codexAncestor is not null)
        {
            return await ResolveByProcessAsync(
                AgentSessionHarness.Codex, codexAncestor.Pid, host, cwdWorkspacePath,
                processInfoProvider, sessions, cancellationToken);
        }

        if (codexSessionId is not null)
        {
            return await ResolveBySessionIdAsync(
                AgentSessionHarness.Codex, codexSessionId, host, cwdWorkspacePath, sessions, cancellationToken);
        }

        return await ResolveByProcessAsync(
            AgentSessionHarness.Copilot, copilotAncestor!.Pid, host, cwdWorkspacePath,
            processInfoProvider, sessions, cancellationToken);
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
    /// earlier, authoritative SessionStart already recorded for it.
    /// </summary>
    private static async Task<AgentSessionGeneration> ResolveBySessionIdAsync(
        string harness,
        string sessionId,
        string host,
        string cwdWorkspacePath,
        IAgentSessionRegistry sessions,
        CancellationToken cancellationToken)
    {
        var row = await sessions.FindBySessionIdAsync(harness, host, sessionId, cancellationToken)
            ?? throw new ExitException(
                $"No {harness} session found for session id '{sessionId}' on this host. If hooks were "
                + $"never installed, run `nitro agent hooks {harness} install` and start a new {harness} "
                + "session; otherwise it may have ended or been reaped.");

        RequireMatchingWorkspace(cwdWorkspacePath, row.WorkspacePath);

        return new AgentSessionGeneration(harness, sessionId, host, row.Pid, row.ProcStart);
    }

    /// <summary>
    /// Resolves a harness's own live session by (host, pid, process-start)
    /// rather than a session id, for a harness whose ancestor process
    /// exposes no session file to read one from directly.
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
                $"No {harness} session found for pid {pid} on this host. If hooks were never "
                + $"installed, run `nitro agent hooks {harness} install` and start a new {harness} "
                + "session; otherwise it may have ended or been reaped.");
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

    /// <summary>
    /// Detects the CLI's client program from environment markers, used as
    /// the fallback when <c>--client</c> is not given. Only markers
    /// confirmed present in a real session of the corresponding tool are
    /// checked; an unconfirmed tool is left undetected rather than guessed,
    /// so its identity can only be recorded via <c>--client</c>.
    ///
    /// | Marker present | Detected as   |
    /// |-----------------|---------------|
    /// | <c>CLAUDECODE</c> | <c>claude-code</c> |
    /// </summary>
    internal static string? DetectClient(IEnvironmentVariableProvider environmentVariables)
        => environmentVariables.GetEnvironmentVariable("CLAUDECODE") is not null
            ? "claude-code"
            : null;

    private static AgentRegisterResult ToResult(AgentSessionRegisterResult result) => new(
        result.Agent.Name,
        result.Session.Role,
        result.Agent.Client,
        result.Agent.RegisteredAt,
        result.Agent.LastSeenAt,
        result.Session.Harness,
        result.Session.SessionId,
        result.Session.HarnessVersion,
        result.Session.BindingKind,
        result.Changed);

    public sealed record AgentRegisterResult(
        string Name,
        string Role,
        string Client,
        DateTimeOffset RegisteredAt,
        DateTimeOffset LastSeenAt,
        string Harness,
        string SessionId,
        string HarnessVersion,
        string BindingKind,
        bool Changed);
}
