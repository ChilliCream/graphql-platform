namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Registers, heartbeats, and tears down the live presence row for one
/// running unified Nitro agent TUI: resolves the workspace once, binds the
/// given durable human
/// mail actor as the operator participant on a <see
/// cref="AgentSessionHarness.NitroBoard"/> session, and reports process
/// liveness through <see cref="AgentSessionEndpointKind.DbWatch"/> rather
/// than a routable transport endpoint.
/// </summary>
internal interface IBoardSessionLifecycle
{
    /// <summary>
    /// Binds <paramref name="actor"/> as the operator on a session row for
    /// this process's own generation (host, this process's pid, and its raw
    /// kernel start ticks), deriving a session id deterministic in that
    /// generation: a duplicate call for the exact same running process
    /// resolves to the same row instead of registering a second one, while a
    /// distinct process - even one started by the same actor - always
    /// resolves to a distinct row. Throws <see cref="ExitException"/> when
    /// no agent workspace resolves from the current directory, or this
    /// process's own start ticks cannot be read.
    /// </summary>
    Task<AgentSessionGeneration> StartAsync(string actor, CancellationToken cancellationToken);

    /// <summary>
    /// Advances <paramref name="generation"/>'s <c>last_beat_at</c> so
    /// <c>lastHeardAt</c> stays meaningful while the board remains open. A
    /// generation already ended or superseded is a no-op.
    /// </summary>
    Task TouchAsync(AgentSessionGeneration generation, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the live presence row for <paramref name="generation"/> on
    /// normal quit, cancellation, or failure. The durable actor identity and
    /// mail history are untouched; only this invocation's live row is
    /// removed. A generation already ended or superseded is a no-op.
    /// </summary>
    Task<bool> EndAsync(AgentSessionGeneration generation, CancellationToken cancellationToken);
}

internal sealed class BoardSessionLifecycle(
    IFileSystem fileSystem,
    IAgentSessionRegistry sessionRegistry,
    INitroInstanceIdProvider instanceIdProvider,
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider,
    IProcessInfoProvider processInfoProvider) : IBoardSessionLifecycle
{
    /// <summary>
    /// The mutable participant role every board session binds, normalized
    /// the same way <see cref="AgentRole.Normalize"/> normalizes any other
    /// role. Never inferred or promoted automatically for any other kind of
    /// participant: only a board's own session binds it.
    /// </summary>
    public const string OperatorRole = "operator";

    /// <summary>
    /// The fixed <c>endpoint_addr</c> every db-watch session carries.
    /// Unlike a Claude peer name or a Codex thread id, there is no
    /// per-session route to address - delivery is the shared workspace
    /// database file itself, already committed by the time a board's
    /// watcher observes it - but the table's cross-column CHECK still
    /// requires a non-empty address whenever <c>endpoint_kind</c> is not
    /// <c>none</c>.
    /// </summary>
    private const string EndpointAddr = "local";

    public async Task<AgentSessionGeneration> StartAsync(string actor, CancellationToken cancellationToken)
    {
        var cwd = fileSystem.GetCurrentDirectory();
        var workspacePath = AgentWorkspace.Find(fileSystem, cwd)
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        var pid = Environment.ProcessId;
        var procStart = processInfoProvider.GetStartTicks(pid)
            ?? throw new ExitException($"Could not read this board process's own start time (pid {pid}).");
        var host = await instanceIdProvider.GetIdAsync(
            globalConfigDirectoryProvider.GetDirectory(), cancellationToken);
        var generation = new AgentSessionGeneration(
            AgentSessionHarness.NitroBoard, DeriveSessionId(host, pid, procStart), host, pid, procStart);

        // envActor binds the row (binding_kind = 'env') and, inside
        // StartAsync, upserts actor's durable identity as implicit only if
        // it does not already exist - an existing identity's own role and
        // client are left exactly as they are. This session's own role,
        // set explicitly below, is a separate column from the durable
        // identity's role: an independently configured task-audit identity
        // for the same actor name is never silently relabeled.
        await sessionRegistry.StartAsync(
            generation, cwd, workspacePath, AgentSessionEndpointKind.DbWatch, EndpointAddr,
            envActor: actor, cancellationToken);

        await sessionRegistry.SetRoleAsync(generation, OperatorRole, cancellationToken);
        await sessionRegistry.RecordHarnessVersionAsync(generation, NitroCliVersion.Current, cancellationToken);

        return generation;
    }

    public Task TouchAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => sessionRegistry.TouchAsync(generation, cancellationToken);

    public Task<bool> EndAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => sessionRegistry.EndAsync(generation, cancellationToken);

    private static string DeriveSessionId(string host, int pid, string procStart) => $"board:{host}:{pid}:{procStart}";
}
