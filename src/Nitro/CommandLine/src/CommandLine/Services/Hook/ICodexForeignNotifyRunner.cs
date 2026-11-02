namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Runs the configured foreign notify command with inherited standard streams and
/// working directory, and waits for its exit.
/// </summary>
internal interface ICodexForeignNotifyRunner
{
    /// <summary>
    /// Runs <paramref name="foreignArgv"/> with <paramref name="payloadJson"/> appended
    /// as the final argument. Returns the exit code, or null for an empty command or
    /// a failure to start or wait for the process.
    /// </summary>
    Task<int?> RunAsync(IReadOnlyList<string> foreignArgv, string payloadJson, CancellationToken cancellationToken);
}
