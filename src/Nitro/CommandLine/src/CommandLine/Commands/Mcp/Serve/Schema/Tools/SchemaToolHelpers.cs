using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Tools;

internal static class SchemaToolHelpers
{
    public static async Task<(string? Sdl, string? ETag)> FetchSchemaAsync(
        NitroApiService apiService,
        string apiId,
        string stage,
        CancellationToken cancellationToken)
    {
        var downloadResult = await apiService.DownloadSchemaAsync(apiId, stage, cancellationToken);

        if (!downloadResult.IsSuccess)
        {
            throw SchemaThrowHelper.SchemaDownloadFailed(downloadResult.ErrorMessage);
        }

        return (downloadResult.Sdl, null);
    }

    public static string FormatError(string message, JsonTypeInfo<GetSchemaError> jsonTypeInfo)
    {
        var error = new GetSchemaError { Error = message };
        return JsonSerializer.Serialize(error, jsonTypeInfo);
    }
}
