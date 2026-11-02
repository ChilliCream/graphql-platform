using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Resolves where the Nitro API is and which credential authorizes requests against it.
/// </summary>
/// <remarks>
/// The API URL precedence is the <c>NITRO_CLOUD_URL</c> environment variable, then the
/// <c>api_url</c> claim of the selected session access token, then the configured default. The
/// credential precedence is the <c>NITRO_API_KEY</c> environment variable, then the access token
/// of the session file. Access tokens are refreshed before they expire.
/// </remarks>
internal sealed class NitroConnectionResolver
{
    private readonly NitroSessionManager _sessionManager;
    private readonly INitroEnvironment _environment;
    private readonly Uri _defaultApiUrl;

    /// <summary>
    /// Initializes a new instance of <see cref="NitroConnectionResolver"/>.
    /// </summary>
    /// <param name="sessionManager">
    /// The manager for the Nitro CLI session.
    /// </param>
    /// <param name="environment">
    /// The accessor for the environment variables that configure the integration.
    /// </param>
    /// <param name="defaultApiUrl">
    /// The Nitro API URL that is used when neither the environment nor the selected access
    /// token configures one.
    /// </param>
    public NitroConnectionResolver(
        NitroSessionManager sessionManager,
        INitroEnvironment environment,
        Uri defaultApiUrl)
    {
        ArgumentNullException.ThrowIfNull(sessionManager);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(defaultApiUrl);

        if (!defaultApiUrl.IsAbsoluteUri)
        {
            throw new ArgumentException(
                "The default Nitro API URL must be an absolute URL.",
                nameof(defaultApiUrl));
        }

        _sessionManager = sessionManager;
        _environment = environment;
        _defaultApiUrl = defaultApiUrl;
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

        NitroSessionReadResult session;
        NitroCredential credential;
        var apiKey = _environment.GetVariable(NitroEnvironmentVariables.ApiKey);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            session = new NitroSessionReadResult(
                NitroSessionStatus.Missing,
                Session: null,
                Message: null);
            credential = NitroCredential.FromApiKey(apiKey);
        }
        else
        {
            session = await _sessionManager.GetSessionAsync(logger, cancellationToken);
            credential = ResolveCredential(session);
        }

        var apiUrl = ResolveApiUrl(session, credential, logger);

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

    private Uri ResolveApiUrl(
        NitroSessionReadResult session,
        NitroCredential credential,
        ILogger logger)
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

        var accessToken = credential.Kind is NitroCredentialKind.ApiKey
            ? null
            : session.Session?.Tokens?.AccessToken;

        if (accessToken is not null
            && NitroAccessToken.TryGetApiUrl(accessToken, out var tokenApiUrl)
            && NitroApiUrl.TryNormalize(tokenApiUrl, out var fromAccessToken))
        {
            return fromAccessToken!;
        }

        return _defaultApiUrl;
    }

    private NitroCredential ResolveCredential(NitroSessionReadResult session)
    {
        if (session.Session?.Tokens is not { AccessToken: { } accessToken, ExpiresAt: not null })
        {
            var reason = session.Message
                ?? $"The Nitro session file '{_sessionManager.SessionFilePath}' carries no usable "
                + "access token.";

            return NitroCredential.Unavailable(
                session.Status is NitroSessionStatus.Missing
                    ? NitroCredentialUnavailableReason.SessionMissing
                    : NitroCredentialUnavailableReason.SessionUnusable,
                $"{reason} Run 'nitro login' to sign in, or set the "
                + $"{NitroEnvironmentVariables.ApiKey} environment variable.");
        }

        if (session.Status is NitroSessionStatus.Expired)
        {
            return NitroCredential.Unavailable(
                NitroCredentialUnavailableReason.SessionExpired,
                session.Message!);
        }

        return NitroCredential.FromAccessToken(accessToken);
    }

    private string DescribeCredentialSource(NitroCredential credential)
        => credential.Kind switch
        {
            NitroCredentialKind.ApiKey => NitroEnvironmentVariables.ApiKey,
            NitroCredentialKind.AccessToken => _sessionManager.SessionFilePath,
            _ => "none"
        };
}
