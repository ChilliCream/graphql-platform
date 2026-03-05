namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema;

internal static class SchemaThrowHelper
{
    public static InvalidOperationException StageNotFound(string stageName, string apiId)
    {
        return new InvalidOperationException(
            $"Stage '{stageName}' not found for API '{apiId}'. Verify the stage name is correct.");
    }

    public static InvalidOperationException SchemaDownloadFailed(string? errorMessage)
    {
        return new InvalidOperationException(errorMessage);
    }
}
