namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The desired Codex CLI <c>config.toml</c> <c>notify</c> program this
/// installer writes: an argv array (not a single shell command string like
/// <see cref="CodexHooksTemplate"/> - Codex invokes <c>notify</c> directly,
/// no shell), always exactly this CLI's launch descriptor followed by
/// <c>agent hook codex notify</c>. Wrapping a foreign notify value never
/// changes this array itself: the wrapping happens INSIDE the installed
/// <c>notify</c> command, which execs the foreign program (recorded in the
/// sidecar) after doing its own work.
/// </summary>
internal static class CodexNotifyTemplate
{
    public static IReadOnlyList<string> BuildArgv(LaunchDescriptor descriptor)
    {
        var argv = new List<string>(1 + descriptor.ArgumentPrefix.Count + 3)
        {
            descriptor.Executable
        };

        argv.AddRange(descriptor.ArgumentPrefix);
        argv.AddRange(["agent", "hook", "codex", "notify"]);

        return argv;
    }
}
