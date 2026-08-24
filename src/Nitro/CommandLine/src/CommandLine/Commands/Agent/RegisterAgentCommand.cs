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
    /// gives its session id directly; Codex and Copilot give only an
    /// ancestor pid, so the session already recorded for that exact
    /// (host, pid, process-start) is looked up instead. Returns null when no
    /// harness ancestor is found at all (register then falls back to
    /// identity-only registration). Throws <see cref="ExitException"/> when
    /// an ancestor IS found but its process is no longer running, its
    /// session cannot be resolved to exactly one row, or this process's own
    /// workspace disagrees with the session's: a detected harness context
    /// that fails to resolve is a real problem, not a silent fallback.
    /// </summary>
    private static async Task<AgentSessionGeneration?> TryResolveGenerationAsync(
        IFileSystem fileSystem,
        IProcessInfoProvider processInfoProvider,
        IClaudeAncestorSessionResolver claudeAncestorResolver,
        ICodexAncestorSessionResolver codexAncestorResolver,
        ICopilotAncestorSessionResolver copilotAncestorResolver,
        INitroInstanceIdProvider instanceIdProvider,
        IGlobalConfigDirectoryProvider globalConfigDirectoryProvider,
        IAgentSessionRegistry sessions,
        CancellationToken cancellationToken)
    {
        var claudeAncestor = claudeAncestorResolver.Resolve();
        var codexAncestor = claudeAncestor is null ? codexAncestorResolver.Resolve() : null;
        var copilotAncestor = claudeAncestor is null && codexAncestor is null
            ? copilotAncestorResolver.Resolve()
            : null;

        if (claudeAncestor is null && codexAncestor is null && copilotAncestor is null)
        {
            return null;
        }

        var cwdWorkspacePath = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        var host = await instanceIdProvider.GetIdAsync(
            globalConfigDirectoryProvider.GetDirectory(), cancellationToken);

        if (claudeAncestor is not null)
        {
            var procStart = processInfoProvider.GetStartTime(claudeAncestor.Pid)
                ?? throw new ExitException(
                    $"Process {claudeAncestor.Pid} for the detected Claude Code session is no longer running.");

            var ancestorWorkspacePath = AgentWorkspace.Find(fileSystem, claudeAncestor.Cwd)
                ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

            RequireMatchingWorkspace(cwdWorkspacePath, ancestorWorkspacePath);

            return new AgentSessionGeneration(
                AgentSessionHarness.ClaudeCode, claudeAncestor.SessionId, host, claudeAncestor.Pid, procStart);
        }

        if (codexAncestor is not null)
        {
            return await ResolveByProcessAsync(
                AgentSessionHarness.Codex, codexAncestor.Pid, host, cwdWorkspacePath,
                processInfoProvider, sessions, cancellationToken);
        }

        return await ResolveByProcessAsync(
            AgentSessionHarness.Copilot, copilotAncestor!.Pid, host, cwdWorkspacePath,
            processInfoProvider, sessions, cancellationToken);
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
        var procStart = processInfoProvider.GetStartTime(pid)
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
