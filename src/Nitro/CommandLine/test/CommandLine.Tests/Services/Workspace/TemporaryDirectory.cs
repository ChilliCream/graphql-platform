namespace ChilliCream.Nitro.CommandLine.Tests.HookRuntime;

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Root = Directory.CreateTempSubdirectory("nitro-directory-tests");
    }

    public DirectoryInfo Root { get; }

    public void Dispose()
    {
        Root.Delete(recursive: true);
    }
}
