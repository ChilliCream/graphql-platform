namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Builds the Nitro API URLs that the integration talks to. The normalization mirrors the Nitro
/// CLI so a session file written by the CLI resolves to the same host, and the download URL
/// mirrors the CLI's fusion configuration download request.
/// </summary>
internal static class NitroApiUrl
{
    private const string ArchiveFormat = "far";
    private const string GraphQLPath = "/graphql";

    /// <summary>
    /// Normalizes a configured Nitro API URL into an absolute base URL. The Nitro CLI stores the
    /// API URL without a scheme, so a missing scheme defaults to <c>https</c>. Path, query,
    /// fragment and user information are dropped.
    /// </summary>
    /// <param name="apiUrl">
    /// The configured API URL, with or without a scheme.
    /// </param>
    /// <param name="normalized">
    /// The normalized base URL.
    /// </param>
    /// <returns>
    /// <c>true</c> when <paramref name="apiUrl"/> could be normalized; otherwise <c>false</c>.
    /// </returns>
    public static bool TryNormalize(string? apiUrl, out Uri? normalized)
    {
        normalized = null;

        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            return false;
        }

        apiUrl = apiUrl.Trim();

        if (!apiUrl.StartsWith(Uri.UriSchemeHttps + "://", StringComparison.OrdinalIgnoreCase)
            && !apiUrl.StartsWith(Uri.UriSchemeHttp + "://", StringComparison.OrdinalIgnoreCase))
        {
            apiUrl = $"{Uri.UriSchemeHttps}://{apiUrl}";
        }

        try
        {
            var builder = new UriBuilder(apiUrl)
            {
                Path = "/",
                Query = string.Empty,
                Fragment = string.Empty,
                UserName = string.Empty,
                Password = string.Empty
            };

            normalized = builder.Uri;

            return true;
        }
        catch (UriFormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Creates the GraphQL endpoint URL of the Nitro API.
    /// </summary>
    public static Uri CreateGraphQLEndpoint(Uri apiUrl)
    {
        ArgumentNullException.ThrowIfNull(apiUrl);

        return new Uri(apiUrl, GraphQLPath);
    }

    /// <summary>
    /// Creates the URL that downloads the latest fusion archive of an api for a stage.
    /// </summary>
    /// <param name="apiUrl">
    /// The normalized Nitro API base URL.
    /// </param>
    /// <param name="apiId">
    /// The id of the Nitro api that carries the fusion configuration.
    /// </param>
    /// <param name="stage">
    /// The name of the stage whose fusion configuration is downloaded.
    /// </param>
    /// <param name="fusionVersion">
    /// The gateway format version that the downloaded archive must support.
    /// </param>
    public static Uri CreateFusionConfigurationDownloadUrl(
        Uri apiUrl,
        string apiId,
        string stage,
        Version fusionVersion)
    {
        ArgumentNullException.ThrowIfNull(apiUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        ArgumentNullException.ThrowIfNull(fusionVersion);

        var path = $"/api/v1/apis/{Uri.EscapeDataString(apiId)}/fusion/configurations/latest/download"
            + $"?stage={Uri.EscapeDataString(stage)}"
            + $"&format={ArchiveFormat}"
            + $"&fusionVersion={Uri.EscapeDataString(fusionVersion.ToString())}";

        return new Uri(apiUrl, path);
    }
}
