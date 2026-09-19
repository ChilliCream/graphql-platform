namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Runs a previously-configured foreign <c>notify</c> program after this CLI's own
/// notify work runs, as a child process with the identical argv payload, inherited
/// stdio, and this process's own cwd, waiting for it to finish.
/// </summary>
internal interface ICodexForeignNotifyRunner
{
    /// <summary>
    /// Runs <paramref name="foreignArgv"/> with <paramref name="payloadJson"/>
    /// appended as its one argument, and returns its exit code, or null when the
    /// foreign program could not be spawned at all.
    /// </summary>
    Task<int?> RunAsync(IReadOnlyList<string> foreignArgv, string payloadJson, CancellationToken cancellationToken);
}
