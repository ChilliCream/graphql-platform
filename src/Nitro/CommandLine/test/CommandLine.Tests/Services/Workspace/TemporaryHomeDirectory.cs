namespace ChilliCream.Nitro.CommandLine.Tests.HookRuntime;

internal sealed class TemporaryHomeDirectory : IDisposable
{
    private readonly string? _home;
    private readonly string? _userProfile;

    public TemporaryHomeDirectory()
    {
        Root = Directory.CreateTempSubdirectory("nitro-home-tests");
        _home = Environment.GetEnvironmentVariable("HOME");
        _userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
        Environment.SetEnvironmentVariable("HOME", Root.FullName);
        Environment.SetEnvironmentVariable("USERPROFILE", Root.FullName);
    }

    public DirectoryInfo Root { get; }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("HOME", _home);
        Environment.SetEnvironmentVariable("USERPROFILE", _userProfile);
        Root.Delete(recursive: true);
    }
}
