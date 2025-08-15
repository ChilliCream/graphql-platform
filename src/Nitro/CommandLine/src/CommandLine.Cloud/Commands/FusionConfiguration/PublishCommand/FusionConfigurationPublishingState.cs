namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.FusionConfiguration;

public static class FusionConfigurationPublishingState
{
    public static async ValueTask<string?> GetRequestId(CancellationToken cancellationToken)
    {
        var file = GetCacheFile();
        if (file.Exists)
        {
            return await File.ReadAllTextAsync(file.FullName, cancellationToken);
        }

        return null;
    }

    public static async Task SetRequestId(string requestId, CancellationToken cancellationToken)
    {
        var file = GetCacheFile();
        await File.WriteAllTextAsync(file.FullName, requestId, cancellationToken);
    }

    private static FileInfo GetCacheFile()
        => new(Path.Combine(Path.GetTempPath(), "fusion.configuration.publishing.state"));
}
