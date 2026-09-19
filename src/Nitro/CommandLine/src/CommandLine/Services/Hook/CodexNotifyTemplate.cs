namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The desired Codex CLI <c>config.toml</c> <c>notify</c> program this installer
/// writes: an argv array, always exactly this CLI's launch descriptor followed by
/// <c>agent hook codex notify</c>. Wrapping a foreign notify value never changes
/// this array; the wrapping happens inside the installed <c>notify</c> command.
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
