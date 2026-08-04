using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Reads the Nitro CLI session and renews its access token before it expires.
/// </summary>
internal sealed class NitroSessionManager
{
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly NitroSessionReader _sessionReader;
    private readonly NitroTokenRefreshClient _refreshClient;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _accessTokenExpiryGrace;

    public NitroSessionManager(
        NitroSessionReader sessionReader,
        NitroTokenRefreshClient refreshClient,
        TimeProvider timeProvider,
        TimeSpan accessTokenExpiryGrace)
    {
        ArgumentNullException.ThrowIfNull(sessionReader);
        ArgumentNullException.ThrowIfNull(refreshClient);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (accessTokenExpiryGrace < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(accessTokenExpiryGrace),
                accessTokenExpiryGrace,
                "The access token expiry grace must not be negative.");
        }

        _sessionReader = sessionReader;
        _refreshClient = refreshClient;
        _timeProvider = timeProvider;
        _accessTokenExpiryGrace = accessTokenExpiryGrace;
    }

    public string SessionFilePath => _sessionReader.SessionFilePath;

    public async Task<NitroSessionReadResult> GetSessionAsync(
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var current = await _sessionReader.ReadAsync(cancellationToken);
        if (!RequiresRefresh(current))
        {
            return current;
        }

        await _refreshGate.WaitAsync(cancellationToken);
        try
        {
            // Another Aspire operation or the Nitro CLI may have refreshed the file while this
            // operation waited for the gate.
            current = await _sessionReader.ReadAsync(cancellationToken);
            if (!RequiresRefresh(current))
            {
                return current;
            }

            var session = current.Session!;
            var tokens = session.Tokens!;
            if (!TryCreateRefreshRequest(session, out var identityServer, out var clientId))
            {
                return Expired(current, reason: null);
            }

            NitroTokenRefreshResult refreshed;
            try
            {
                refreshed = await _refreshClient.RefreshAsync(
                    identityServer!,
                    clientId!,
                    tokens.RefreshToken!,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException exception)
            {
                return Expired(current, exception.Message);
            }
            catch (Exception exception) when (
                exception is HttpRequestException or IOException or InvalidOperationException)
            {
                return Expired(current, exception.Message);
            }

            if (!refreshed.IsSuccess)
            {
                return Expired(current, refreshed.Error);
            }

            DateTimeOffset expiresAt;
            try
            {
                expiresAt = _timeProvider.GetUtcNow().AddSeconds(refreshed.ExpiresIn);
            }
            catch (ArgumentOutOfRangeException)
            {
                return Expired(current, "the token endpoint returned an invalid expiry");
            }

            var refreshedTokens = new NitroSessionTokens
            {
                AccessToken = refreshed.AccessToken,
                IdToken = refreshed.IdentityToken ?? tokens.IdToken,
                RefreshToken = refreshed.RefreshToken ?? tokens.RefreshToken,
                ExpiresAt = expiresAt
            };
            session.Tokens = refreshedTokens;

            try
            {
                await _sessionReader.WriteAsync(session, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                session.Tokens = tokens;
                return Expired(current, $"the refreshed session could not be saved: {exception.Message}");
            }

            logger.LogInformation(
                "Refreshed the Nitro session access token stored at {SessionFilePath}.",
                _sessionReader.SessionFilePath);

            return new NitroSessionReadResult(
                NitroSessionStatus.Available,
                session,
                Message: null);
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private bool RequiresRefresh(NitroSessionReadResult result)
        => result.Session?.Tokens?.ExpiresAt is { } expiresAt
            && expiresAt - _accessTokenExpiryGrace <= _timeProvider.GetUtcNow();

    private static bool TryCreateRefreshRequest(
        NitroSession session,
        out Uri? identityServer,
        out string? clientId)
    {
        identityServer = null;
        clientId = null;

        return !string.IsNullOrWhiteSpace(session.Tokens?.RefreshToken)
            && !string.IsNullOrWhiteSpace(session.Tokens.IdToken)
            && Uri.TryCreate(session.IdentityServer, UriKind.Absolute, out identityServer)
            && (string.Equals(
                    identityServer.Scheme,
                    Uri.UriSchemeHttp,
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    identityServer.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase))
            && NitroAccessToken.TryGetAudience(session.Tokens.IdToken, out clientId);
    }

    private NitroSessionReadResult Expired(
        NitroSessionReadResult current,
        string? reason)
    {
        var expiresAt = current.Session!.Tokens!.ExpiresAt!.Value;
        var message = $"The Nitro session stored at '{_sessionReader.SessionFilePath}' expired at "
            + $"{expiresAt.ToUniversalTime():yyyy-MM-dd HH:mm:ss}Z";

        if (!string.IsNullOrWhiteSpace(reason))
        {
            message += $" and could not be refreshed because {reason}";
        }

        return new NitroSessionReadResult(
            NitroSessionStatus.Expired,
            current.Session,
            $"{message}. Run 'nitro login' to sign in again.");
    }
}
