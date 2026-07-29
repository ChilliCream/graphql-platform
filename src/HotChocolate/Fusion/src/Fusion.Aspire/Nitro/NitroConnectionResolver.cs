using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Resolves where the Nitro API is and which credential authorizes requests against it.
/// </summary>
/// <remarks>
/// The API URL precedence is the <c>NITRO_CLOUD_URL</c> environment variable, then the
/// <c>api_url</c> claim of the selected session access token, then the configured default. The
/// credential precedence is the <c>NITRO_API_KEY</c> environment variable, then the access token
/// of the session file while it has not expired.
/// </remarks>
internal sealed class NitroConnectionResolver
{
    private readonly NitroSessionReader _sessionReader;
    private readonly INitroEnvironment _environment;
    private readonly Uri _defaultApiUrl;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _accessTokenExpiryGrace;

    /// <summary>
    /// Initializes a new instance of <see cref="NitroConnectionResolver"/>.
    /// </summary>
    /// <param name="sessionReader">
    /// The reader for the Nitro CLI session file.
    /// </param>
    /// <param name="environment">
    /// The accessor for the environment variables that configure the integration.
    /// </param>
    /// <param name="defaultApiUrl">
    /// The Nitro API URL that is used when neither the environment nor the selected access
    /// token configures one.
    /// </param>
    /// <param name="timeProvider">
    /// The time source that decides whether an access token has expired.
    /// </param>
    /// <param name="accessTokenExpiryGrace">
    /// The window before the token expiry in which the token is already treated as expired.
    /// </param>
    public NitroConnectionResolver(
        NitroSessionReader sessionReader,
        INitroEnvironment environment,
        Uri defaultApiUrl,
        TimeProvider timeProvider,
        TimeSpan accessTokenExpiryGrace)
    {
        ArgumentNullException.ThrowIfNull(sessionReader);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(defaultApiUrl);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (!defaultApiUrl.IsAbsoluteUri)
        {
            throw new ArgumentException(
                "The default Nitro API URL must be an absolute URL.",
                nameof(defaultApiUrl));
        }

        if (accessTokenExpiryGrace < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(accessTokenExpiryGrace),
                accessTokenExpiryGrace,
                "The access token expiry grace must not be negative.");
        }

        _sessionReader = sessionReader;
        _environment = environment;
        _defaultApiUrl = defaultApiUrl;
        _timeProvider = timeProvider;
        _accessTokenExpiryGrace = accessTokenExpiryGrace;
    }

    /// <summary>
    /// Resolves the connection to the Nitro API and logs the resolved API URL together with the
    /// selected credential source.
    /// </summary>
    /// <param name="logger">
    /// The logger that receives the resolved API URL and credential source.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    public async Task<NitroConnection> ResolveAsync(
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var session = await _sessionReader.ReadAsync(cancellationToken);
        var credential = ResolveCredential(session);
        var apiUrl = ResolveApiUrl(credential, logger);

        logger.LogInformation(
            "Using the Nitro API at {ApiUrl} with the credential source {CredentialSource}.",
            apiUrl,
            DescribeCredentialSource(credential));

        if (credential.Kind is NitroCredentialKind.None)
        {
            logger.LogDebug("{Message}", credential.UnavailableMessage);
        }

        return new NitroConnection(apiUrl, NitroApiUrl.CreateGraphQLEndpoint(apiUrl), credential);
    }

    private Uri ResolveApiUrl(NitroCredential credential, ILogger logger)
    {
        var configuredUrl = _environment.GetVariable(NitroEnvironmentVariables.CloudUrl);

        if (!string.IsNullOrWhiteSpace(configuredUrl))
        {
            if (NitroApiUrl.TryNormalize(configuredUrl, out var fromEnvironment))
            {
                return fromEnvironment!;
            }

            logger.LogWarning(
                "The value of the {Variable} environment variable is not a valid URL: {Value}.",
                NitroEnvironmentVariables.CloudUrl,
                configuredUrl);
        }

        if (credential is { Kind: NitroCredentialKind.AccessToken, Value: { } accessToken }
            && NitroAccessToken.TryGetApiUrl(accessToken, out var tokenApiUrl)
            && NitroApiUrl.TryNormalize(tokenApiUrl, out var fromAccessToken))
        {
            return fromAccessToken!;
        }

        return _defaultApiUrl;
    }

    private NitroCredential ResolveCredential(NitroSessionReadResult session)
    {
        var apiKey = _environment.GetVariable(NitroEnvironmentVariables.ApiKey);

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            return NitroCredential.FromApiKey(apiKey);
        }

        if (session.Session?.Tokens is not { AccessToken: { } accessToken, ExpiresAt: { } expiresAt })
        {
            var reason = session.Message
                ?? $"The Nitro session file '{_sessionReader.SessionFilePath}' carries no usable "
                + "access token.";

            return NitroCredential.Unavailable(
                session.Status is NitroSessionStatus.Missing
                    ? NitroCredentialUnavailableReason.SessionMissing
                    : NitroCredentialUnavailableReason.SessionUnusable,
                $"{reason} Run 'nitro login' to sign in, or set the "
                + $"{NitroEnvironmentVariables.ApiKey} environment variable.");
        }

        if (expiresAt - _accessTokenExpiryGrace <= _timeProvider.GetUtcNow())
        {
            return NitroCredential.Unavailable(
                NitroCredentialUnavailableReason.SessionExpired,
                $"The Nitro session stored at '{_sessionReader.SessionFilePath}' expired at "
                + $"{expiresAt.ToUniversalTime():yyyy-MM-dd HH:mm:ss}Z. Run 'nitro login' to "
                + "sign in again.");
        }

        return NitroCredential.FromAccessToken(accessToken);
    }

    private string DescribeCredentialSource(NitroCredential credential)
        => credential.Kind switch
        {
            NitroCredentialKind.ApiKey => NitroEnvironmentVariables.ApiKey,
            NitroCredentialKind.AccessToken => _sessionReader.SessionFilePath,
            _ => "none"
        };
}
