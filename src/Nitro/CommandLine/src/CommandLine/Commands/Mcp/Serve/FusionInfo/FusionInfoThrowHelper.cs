namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.FusionInfo;

internal static class FusionInfoThrowHelper
{
    public static InvalidOperationException FusionConfigurationNotFound(string apiId, string stage)
    {
        return new InvalidOperationException($"No fusion configuration found for API '{apiId}' on stage '{stage}'.");
    }

    public static InvalidOperationException FusionArchiveDownloadFailed(string apiId, string stage)
    {
        return new InvalidOperationException(
            $"Could not download fusion configuration for API '{apiId}' on stage '{stage}'.");
    }

    public static InvalidOperationException FusionArchiveExceedsMaxSize()
    {
        return new InvalidOperationException("Archive exceeds maximum allowed size (100 MB).");
    }
}
