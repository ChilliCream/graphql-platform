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

        // A framework-dependent .NET global tool runs through its `nitro`
        // shim while argv[0] points at the package's internal .store DLL.
        // The shim already selects that DLL, so appending argv[0] would pass
        // it to Nitro as a user argument and break every installed hook.
        // Store the stable command name instead; it also survives tool
        // updates that replace the versioned .store directory.
        if (string.Equals(processName, "nitro", StringComparison.OrdinalIgnoreCase)
            && arg0?.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) == true)
        {
            return new LaunchDescriptor("nitro", []);
        }

        // Framework-dependent invocation ("dotnet nitro.dll ..."): the
        // running process is the dotnet muxer, and argv[0] is the managed
        // assembly path, distinct from the muxer executable itself.
        if (arg0?.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) == true
            && string.Equals(processName, "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            return new LaunchDescriptor(processPath, [Path.GetFullPath(arg0)]);
        }

        return new LaunchDescriptor(processPath, []);
    }
}
