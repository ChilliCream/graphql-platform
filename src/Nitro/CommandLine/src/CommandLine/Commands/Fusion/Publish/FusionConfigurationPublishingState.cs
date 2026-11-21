using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.Publish;

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

    /// <summary>
    /// Removes the stored request ID if it is <paramref name="requestId"/>.
    /// </summary>
    internal static async Task ClearRequestId(
        IFileSystem fileSystem,
        string requestId,
        CancellationToken cancellationToken)
    {
        var storedRequestId = await GetRequestId(fileSystem, cancellationToken);

        if (string.Equals(storedRequestId, requestId, StringComparison.Ordinal))
        {
            fileSystem.DeleteFile(GetCacheFile());
        }
    }

    private static string GetCacheFile()
        => Path.Combine(Path.GetTempPath(), "fusion.configuration.publishing.state");
}
