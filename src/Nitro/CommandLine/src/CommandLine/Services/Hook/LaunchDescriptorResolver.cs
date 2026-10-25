namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class LaunchDescriptorResolver : ILaunchDescriptorResolver
{
    public LaunchDescriptor Resolve()
    {
        var processPath = Environment.ProcessPath
            ?? throw new ExitException(
                "Could not resolve this process's executable path; cannot install hooks without a "
                + "reliable launch descriptor.");

        var args = Environment.GetCommandLineArgs();
        var arg0 = args.Length > 0 ? args[0] : null;

        return Resolve(processPath, arg0);
    }

    internal static LaunchDescriptor Resolve(string processPath, string? arg0)
    {
        var processName = Path.GetFileNameWithoutExtension(processPath);

        // A Nitro tool invocation resolves to the command name without its internal DLL path.
        if (string.Equals(processName, "nitro", StringComparison.OrdinalIgnoreCase)
            && arg0?.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) == true)
        {
            return new LaunchDescriptor("nitro", []);
        }

        // Framework-dependent invocation: the running process is the dotnet muxer,
        // and argv[0] is the managed assembly path.
        if (arg0?.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) == true
            && string.Equals(processName, "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            return new LaunchDescriptor(processPath, [Path.GetFullPath(arg0)]);
        }

        return new LaunchDescriptor(processPath, []);
    }
}
