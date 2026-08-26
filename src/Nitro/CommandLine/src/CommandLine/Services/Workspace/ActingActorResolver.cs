using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class ActingActorResolver(
    IFileSystem fileSystem,
    IEnvironmentVariableProvider environmentVariables,
    IProcessInfoProvider processInfoProvider,
    IClaudeAncestorSessionResolver claudeAncestorResolver,
    ICodexAncestorSessionResolver codexAncestorResolver,
    ICopilotAncestorSessionResolver copilotAncestorResolver,
    INitroInstanceIdProvider instanceIdProvider,
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider,
    IAgentSessionRegistry sessions) : IActingActorResolver
{
    public const string EnvironmentVariableName = "NITRO_ACTOR";

    public async Task<string> ResolveAsync(
        string? optionValue,
        CancellationToken cancellationToken)
    {
        var requested = optionValue
            ?? environmentVariables.GetEnvironmentVariable(EnvironmentVariableName);

        if (requested is not null)
        {
            return MailAgentName.Normalize(requested);
        }

        var session = await TryResolveSessionAsync(cancellationToken);

        if (session is null)
        {
            throw new ExitException(
                "Could not identify the current session. Pass '--actor <actor>' explicitly.");
        }

        var currentActor = session.AgentName
            ?? throw new ExitException(
                $"The current {session.Harness} session has no actor. Run `nitro agent register` and retry.");

        return currentActor;
    }

    private async Task<AgentSessionRecord?> TryResolveSessionAsync(CancellationToken cancellationToken)
    {
        var claudeAncestor = claudeAncestorResolver.Resolve();
        var codexAncestor = claudeAncestor is null ? codexAncestorResolver.Resolve() : null;
        var codexSessionId = claudeAncestor is null && codexAncestor is null
            ? environmentVariables.GetEnvironmentVariable("CODEX_SESSION_ID")
                ?? environmentVariables.GetEnvironmentVariable("CODEX_THREAD_ID")
            : null;
        var copilotAncestor = claudeAncestor is null && codexAncestor is null && codexSessionId is null
            ? copilotAncestorResolver.Resolve()
            : null;

        if (claudeAncestor is null
            && codexAncestor is null
            && codexSessionId is null
            && copilotAncestor is null)
        {
            return null;
        }

        var workspacePath = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");
        var host = await instanceIdProvider.GetIdAsync(
            globalConfigDirectoryProvider.GetDirectory(), cancellationToken);

        if (claudeAncestor is not null)
        {
            var processStart = processInfoProvider.GetStartTicks(claudeAncestor.Pid)
                ?? throw new ExitException(
                    $"Process {claudeAncestor.Pid} for the detected Claude Code session is no longer running.");
            var ancestorWorkspacePath = AgentWorkspace.Find(fileSystem, claudeAncestor.Cwd)
                ?? throw new ExitException("No agent workspace found for the detected Claude Code session.");
            RequireMatchingWorkspace(workspacePath, ancestorWorkspacePath);

            var generation = new AgentSessionGeneration(
                AgentSessionHarness.ClaudeCode,
                claudeAncestor.SessionId,
                host,
                claudeAncestor.Pid,
                processStart);
            var row = await sessions.FindBySessionIdAsync(
                generation.Harness, generation.Host, generation.SessionId, cancellationToken);

            if (row is null)
            {
                var (endpointKind, endpointAddress) = EndpointAddress.IsValid(claudeAncestor.Name)
                    ? (AgentSessionEndpointKind.ClaudePeer, claudeAncestor.Name)
                    : (AgentSessionEndpointKind.None, string.Empty);
                row = await sessions.StartAsync(
                    generation,
                    claudeAncestor.Cwd,
                    ancestorWorkspacePath,
                    endpointKind,
                    endpointAddress,
                    envActor: null,
                    cancellationToken);
            }

            RequireMatchingWorkspace(workspacePath, row.WorkspacePath);
            return row;
        }

        if (codexAncestor is not null)
        {
            var processStart = processInfoProvider.GetStartTicks(codexAncestor.Pid)
                ?? throw new ExitException(
                    $"Process {codexAncestor.Pid} for the detected Codex session is no longer running.");
            var candidates = await sessions.FindByProcessAsync(
                AgentSessionHarness.Codex, host, codexAncestor.Pid, processStart, cancellationToken);

            if (candidates.Count == 0)
            {
                throw new ExitException(
                    "Nitro detected Codex but could not resolve its session. "
                    + "Install Codex hooks, start a new session, and retry.");
            }

            if (candidates.Count > 1)
            {
                throw new ExitException(
                    $"Nitro found {candidates.Count} Codex sessions for the current process. "
                    + "Retry from a hook-initialized session.");
            }

            RequireMatchingWorkspace(workspacePath, candidates[0].WorkspacePath);
            return candidates[0];
        }

        if (copilotAncestor is not null)
        {
            var processStart = processInfoProvider.GetStartTicks(copilotAncestor.Pid)
                ?? throw new ExitException(
                    $"Process {copilotAncestor.Pid} for the detected Copilot session is no longer running.");
            var candidates = await sessions.FindByProcessAsync(
                AgentSessionHarness.Copilot, host, copilotAncestor.Pid, processStart, cancellationToken);

            if (candidates.Count != 1)
            {
                throw new ExitException(
                    "Nitro detected Copilot but could not resolve exactly one generated session. "
                    + "Restart the Copilot session or pass '--actor <actor>' explicitly.");
            }

            RequireMatchingWorkspace(workspacePath, candidates[0].WorkspacePath);
            return candidates[0];
        }

        var bySessionId = await sessions.FindBySessionIdAsync(
            AgentSessionHarness.Codex, host, codexSessionId!, cancellationToken)
            ?? throw new ExitException(
                $"Nitro detected Codex session '{codexSessionId}' but it has no live hook registration. "
                + "Install Codex hooks, start a new session, and retry.");
        RequireMatchingWorkspace(workspacePath, bySessionId.WorkspacePath);
        return bySessionId;
    }

    private static void RequireMatchingWorkspace(string currentWorkspacePath, string sessionWorkspacePath)
    {
        if (currentWorkspacePath != sessionWorkspacePath)
        {
            throw new ExitException(
                $"This process's workspace ('{currentWorkspacePath}') does not match the session's "
                + $"workspace ('{sessionWorkspacePath}').");
        }
    }
}
