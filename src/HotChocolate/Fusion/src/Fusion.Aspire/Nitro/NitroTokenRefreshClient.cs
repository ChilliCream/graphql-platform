using System.Globalization;
using System.Net;
using System.Text.Json;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Renews a Nitro CLI access token through the OpenID Connect authority stored in the session.
/// </summary>
internal sealed class NitroTokenRefreshClient(HttpClient httpClient)
{
    public async Task<NitroTokenRefreshResult> RefreshAsync(
        Uri identityServer,
        string clientId,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identityServer);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var discoveryEndpoint = new Uri(
            $"{identityServer.AbsoluteUri.TrimEnd('/')}/.well-known/openid-configuration");
        using var discoveryResponse = await httpClient.GetAsync(
            discoveryEndpoint,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!discoveryResponse.IsSuccessStatusCode)
        {
            return NitroTokenRefreshResult.Failed(
                $"identity discovery returned HTTP {(int)discoveryResponse.StatusCode}");
        }

        Uri tokenEndpoint;
        try
        {
            await using var discoveryStream = await discoveryResponse.Content
                .ReadAsStreamAsync(cancellationToken);
            using var discovery = await JsonDocument.ParseAsync(
                discoveryStream,
                cancellationToken: cancellationToken);

            if (!discovery.RootElement.TryGetProperty("issuer", out var issuerValue)
                || issuerValue.ValueKind is not JsonValueKind.String
                || !Uri.TryCreate(
                    issuerValue.GetString(),
                    UriKind.Absolute,
                    out var discoveredIssuer)
                || !SameEndpoint(identityServer, discoveredIssuer)
                || !discovery.RootElement.TryGetProperty(
                    "token_endpoint",
                    out var tokenEndpointValue)
                || tokenEndpointValue.ValueKind is not JsonValueKind.String
                || !Uri.TryCreate(
                    tokenEndpointValue.GetString(),
                    UriKind.Absolute,
                    out var discoveredTokenEndpoint)
                || !IsHttp(discoveredTokenEndpoint)
                || !SameEndpoint(identityServer, discoveredTokenEndpoint))
            {
                return NitroTokenRefreshResult.Failed(
                    "identity discovery returned no valid token endpoint");
            }

            tokenEndpoint = discoveredTokenEndpoint;
        }
        catch (JsonException)
        {
            return NitroTokenRefreshResult.Failed(
                "identity discovery returned an invalid document");
        }

        using var content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("client_id", clientId)
        ]);
        using var tokenResponse = await httpClient.PostAsync(
            tokenEndpoint,
            content,
            cancellationToken);

        JsonDocument responseDocument;
        try
        {
            await using var tokenStream = await tokenResponse.Content
                .ReadAsStreamAsync(cancellationToken);
            responseDocument = await JsonDocument.ParseAsync(
                tokenStream,
                cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            return NitroTokenRefreshResult.Failed(
                $"the token endpoint returned HTTP {(int)tokenResponse.StatusCode} with an "
                + "invalid response");
        }

        using (responseDocument)
        {
            var root = responseDocument.RootElement;
            if (!tokenResponse.IsSuccessStatusCode)
            {
                return NitroTokenRefreshResult.Failed(
                    ReadError(root, tokenResponse.StatusCode));
            }

            var accessToken = ReadString(root, "access_token");
            if (string.IsNullOrWhiteSpace(accessToken)
                || !TryReadExpiresIn(root, out var expiresIn))
            {
                return NitroTokenRefreshResult.Failed(
                    "the token endpoint returned no usable access token");
            }

            return NitroTokenRefreshResult.Succeeded(
                accessToken,
                ReadString(root, "id_token"),
                ReadString(root, "refresh_token"),
                expiresIn);
        }
    }

    private static bool IsHttp(Uri uri)
        => string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    private static bool SameEndpoint(Uri left, Uri right)
        => string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.IdnHost, right.IdnHost, StringComparison.OrdinalIgnoreCase)
            && left.Port == right.Port;

    private static bool TryReadExpiresIn(JsonElement root, out long expiresIn)
    {
        expiresIn = 0;
        if (!root.TryGetProperty("expires_in", out var value))
        {
            return false;
        }

        if (value.ValueKind is JsonValueKind.Number)
        {
            return value.TryGetInt64(out expiresIn) && expiresIn > 0;
        }

        return value.ValueKind is JsonValueKind.String
            && long.TryParse(
                value.GetString(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out expiresIn)
            && expiresIn > 0;
    }

    private static string? ReadString(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var value)
            && value.ValueKind is JsonValueKind.String
                ? value.GetString()
                : null;

    private static string ReadError(JsonElement root, HttpStatusCode statusCode)
    {
        var error = ReadString(root, "error");
        var description = ReadString(root, "error_description");

        if (!string.IsNullOrWhiteSpace(error) && !string.IsNullOrWhiteSpace(description))
        {
            return $"the token endpoint rejected the refresh token ({error}: {description})";
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            return $"the token endpoint rejected the refresh token ({error})";
        }

        return $"the token endpoint returned HTTP {(int)statusCode}";
    }
}

internal sealed record NitroTokenRefreshResult(
    bool IsSuccess,
    string? AccessToken,
    string? IdentityToken,
    string? RefreshToken,
    long ExpiresIn,
    string? Error)
{
    public static NitroTokenRefreshResult Succeeded(
        string accessToken,
        string? identityToken,
        string? refreshToken,
        long expiresIn)
        => new(
            IsSuccess: true,
            accessToken,
            identityToken,
            refreshToken,
            expiresIn,
            Error: null);

    public static NitroTokenRefreshResult Failed(string error)
        => new(
            IsSuccess: false,
            AccessToken: null,
            IdentityToken: null,
            RefreshToken: null,
            ExpiresIn: 0,
            error);
}
