namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Builds the notify command arguments from the launch descriptor followed by
/// <c>agent hook codex notify</c>.
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
