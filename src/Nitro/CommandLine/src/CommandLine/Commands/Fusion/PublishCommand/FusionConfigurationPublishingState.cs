using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal static class FusionConfigurationPublishingState
{
    internal static async ValueTask<string?> GetRequestId(
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var file = GetCacheFile();
        if (fileSystem.FileExists(file))
        {
            return await fileSystem.ReadAllTextAsync(file, cancellationToken);
        }

        return null;
    }

    internal static async Task SetRequestId(
        IFileSystem fileSystem,
        string requestId,
        CancellationToken cancellationToken)
    {
        var file = GetCacheFile();
        await fileSystem.WriteAllTextAsync(file, requestId, cancellationToken);
    }

    private static string GetCacheFile()
        => Path.Combine(Path.GetTempPath(), "fusion.configuration.publishing.state");
}
