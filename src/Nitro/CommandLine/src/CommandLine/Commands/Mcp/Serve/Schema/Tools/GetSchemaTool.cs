using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using HotChocolate.Utilities.Introspection;
using ModelContextProtocol.Server;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Tools;

[McpServerToolType]
internal sealed class GetSchemaTool
{
    private static readonly StringComparison PathComparison =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? PathComparison
            : StringComparison.Ordinal;

    [McpServerTool(Name = "get_schema")]
    [Description(
        "Downloads the GraphQL schema (SDL) and saves it to a local file."
            + " Returns the file path so you can read the schema."
            + " Use this before writing queries, exploring types, or"
            + " checking field availability."
            + " Provide either a 'url' to fetch from a GraphQL endpoint directly,"
            + " or use 'api' / 'stage' to download from the Nitro cloud.")]
    public static async Task<string> ExecuteAsync(
        NitroMcpContext mcpContext,
        ISessionService sessionService,
        NitroApiService apiService,
        IHttpClientFactory httpClientFactory,
        [Description(
            "The URL of the GraphQL endpoint to download the schema from"
                + " (e.g. 'https://my-service.example.com/graphql')."
                + " When provided, the tool first tries to fetch the SDL from"
                + " {url}/schema.graphql, and falls back to an introspection query."
                + " No Nitro authentication is required when using a URL.")]
            string? url = null,
        [Description(
            "The API to download the schema for. Can be a Nitro API ID"
                + " (base64, e.g. 'QXBpOmFiYzEyMw==') or omitted if --api-id"
                + " was set at CLI startup. Ignored when 'url' is provided.")]
            string? api = null,
        [Description(
            "The stage name to download the schema from"
                + " (e.g. 'dev', 'staging', 'production')."
                + " Ignored when 'url' is provided.")]
            string stage = "production",
        [Description(
            "Optional file path to save the schema. Defaults to"
                + " /tmp/nitro-schema-{apiName}-{stage}.graphql")]
            string? outputPath = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            return await ExecuteFromUrlAsync(
                httpClientFactory, url, outputPath, cancellationToken);
        }

        return await ExecuteFromNitroAsync(
            mcpContext, sessionService, apiService,
            api, stage, outputPath, cancellationToken);
    }

    private static async Task<string> ExecuteFromUrlAsync(
        IHttpClientFactory httpClientFactory,
        string url,
        string? outputPath,
        CancellationToken cancellationToken)
    {
        // Normalize the URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != "http" && baseUri.Scheme != "https"))
        {
            return FormatError("Invalid URL. Provide an absolute HTTP(S) URL"
                + " (e.g. 'https://my-service.example.com/graphql').");
        }

        var sdlUri = new Uri(baseUri.ToString().TrimEnd('/') + "/schema.graphql");
        string sdl;

        using var httpClient = httpClientFactory.CreateClient();

        // 1. Try fetching the SDL directly from {url}/schema.graphql
        try
        {
            using var sdlResponse = await httpClient.GetAsync(sdlUri, cancellationToken);
            if (sdlResponse.IsSuccessStatusCode)
            {
                var content = await sdlResponse.Content.ReadAsStringAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    sdl = content;
                    return await WriteSchemaAndReturnResultAsync(
                        sdl, outputPath, baseUri.Host, "url", cancellationToken);
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            // SDL endpoint not available — fall through to introspection.
        }

        // 2. Fall back to introspection
        try
        {
            using var introspectionClient = httpClientFactory.CreateClient();
            introspectionClient.BaseAddress = baseUri;

            var document = await IntrospectionClient.IntrospectServerAsync(
                introspectionClient, cancellationToken);

            sdl = document.ToString(indented: true);

            return await WriteSchemaAndReturnResultAsync(
                sdl, outputPath, baseUri.Host, "url", cancellationToken);
        }
        catch (Exception ex)
        {
            return FormatError(
                "Failed to download schema from '" + url + "'. "
                    + "Neither SDL endpoint nor introspection succeeded: " + ex.Message);
        }
    }

    private static async Task<string> ExecuteFromNitroAsync(
        NitroMcpContext mcpContext,
        ISessionService sessionService,
        NitroApiService apiService,
        string? api,
        string stage,
        string? outputPath,
        CancellationToken cancellationToken)
    {
        // 1. Ensure authenticated session
        var session = await sessionService.LoadSessionAsync(cancellationToken);
        if (session?.Tokens?.AccessToken is null)
        {
            return FormatError("Not authenticated. Run 'nitro login' first.");
        }

        // 2. Resolve API ID
        var resolver = new ApiResolver(mcpContext);
        var resolveResult = resolver.Resolve(api);
        if (!resolveResult.IsSuccess)
        {
            return FormatError(resolveResult.ErrorMessage!);
        }

        var apiId = resolveResult.ApiId;
        var apiName = resolveResult.ApiName;

        // 3. Download schema via REST
        var downloadResult = await apiService.DownloadSchemaAsync(apiId, stage, cancellationToken);

        if (!downloadResult.IsSuccess)
        {
            return FormatError(downloadResult.ErrorMessage!);
        }

        // 4. Determine output path and validate
        var filePath = ResolveOutputPath(outputPath, apiName, stage);
        var fullOutputPath = Path.GetFullPath(filePath);
        var workspaceRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
        if (outputPath is not null
            && !fullOutputPath.StartsWith(workspaceRoot, PathComparison)
            && !fullOutputPath.StartsWith(Path.GetFullPath(Path.GetTempPath()), PathComparison))
        {
            return FormatError("Output path must be within the workspace or temp directory.");
        }

        // 5. Write schema to file
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(filePath, downloadResult.Sdl, cancellationToken);
        }
        catch (IOException ex)
        {
            return FormatError("Failed to write schema to '" + filePath + "': " + ex.Message);
        }

        // 6. Return result
        var fileInfo = new FileInfo(filePath);
        var result = new GetSchemaResult
        {
            FilePath = filePath,
            ApiId = apiId,
            ApiName = apiName,
            Stage = stage,
            Size = fileInfo.Length
        };

        return JsonSerializer.Serialize(result, GetSchemaJsonContext.Default.GetSchemaResult);
    }

    private static async Task<string> WriteSchemaAndReturnResultAsync(
        string sdl,
        string? outputPath,
        string hostName,
        string stage,
        CancellationToken cancellationToken)
    {
        var filePath = ResolveOutputPath(outputPath, hostName, stage);
        var fullOutputPath = Path.GetFullPath(filePath);
        var workspaceRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
        if (outputPath is not null
            && !fullOutputPath.StartsWith(workspaceRoot, PathComparison)
            && !fullOutputPath.StartsWith(Path.GetFullPath(Path.GetTempPath()), PathComparison))
        {
            return FormatError("Output path must be within the workspace or temp directory.");
        }

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(filePath, sdl, cancellationToken);
        }
        catch (IOException ex)
        {
            return FormatError("Failed to write schema to '" + filePath + "': " + ex.Message);
        }

        var fileInfo = new FileInfo(filePath);
        var result = new GetSchemaResult
        {
            FilePath = filePath,
            ApiName = hostName,
            Stage = stage,
            Size = fileInfo.Length
        };

        return JsonSerializer.Serialize(result, GetSchemaJsonContext.Default.GetSchemaResult);
    }

    private static string ResolveOutputPath(string? customPath, string apiName, string stage)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            return customPath;
        }

        var sanitizedApi = Sanitize(apiName, 64);
        var sanitizedStage = Sanitize(stage, 32);
        return Path.Combine(Path.GetTempPath(), "nitro-schema-" + sanitizedApi + "-" + sanitizedStage + ".graphql");
    }

    private static string Sanitize(string value, int maxLength)
    {
        var len = Math.Min(value.Length, maxLength);
        return string.Create(len, (value, maxLength), static (span, state) =>
        {
            var pos = 0;
            foreach (var c in state.value)
            {
                if (pos >= span.Length)
                {
                    break;
                }

                if (char.IsAsciiLetterOrDigit(c) || c == '-')
                {
                    span[pos++] = char.ToLowerInvariant(c);
                }
                else if (c is '_' or '/')
                {
                    span[pos++] = '-';
                }
            }

            // Fill unused portion (filtered chars reduced length)
            span[pos..].Fill('\0');
        }).TrimEnd('\0').Trim('-');
    }

    private static string FormatError(string message)
        => SchemaToolHelpers.FormatError(message, GetSchemaJsonContext.Default.GetSchemaError);
}
