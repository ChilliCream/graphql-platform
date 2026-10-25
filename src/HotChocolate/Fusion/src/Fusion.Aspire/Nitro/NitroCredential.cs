namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// The kind of credential that authorizes requests against the Nitro API.
/// </summary>
internal enum NitroCredentialKind
{
    /// <summary>
    /// No credential is available.
    /// </summary>
    None,

    /// <summary>
    /// An API key, taken from the <c>NITRO_API_KEY</c> environment variable.
    /// </summary>
    ApiKey,

    /// <summary>
    /// An access token, taken from the Nitro CLI session file.
    /// </summary>
    AccessToken
}

/// <summary>
/// The reason why no credential is available.
/// </summary>
internal enum NitroCredentialUnavailableReason
{
    /// <summary>
    /// A credential is available.
    /// </summary>
    None,

    /// <summary>
    /// No session file exists and no API key is configured.
    /// </summary>
    SessionMissing,

    /// <summary>
    /// A session file exists but could not be used.
    /// </summary>
    SessionUnusable,

    /// <summary>
    /// The access token of the session file has expired.
    /// </summary>
    SessionExpired
}

/// <summary>
/// The credential that authorizes requests against the Nitro API.
/// </summary>
/// <param name="Kind">
/// The kind of the credential.
/// </param>
/// <param name="Value">
/// The credential value, set when <paramref name="Kind"/> is not
/// <see cref="NitroCredentialKind.None"/>.
/// </param>
/// <param name="UnavailableReason">
/// Why no credential is available.
/// </param>
/// <param name="UnavailableMessage">
/// A message that explains why no credential is available and what to do about it. It is
/// <c>null</c> when a credential is available.
/// </param>
internal sealed record NitroCredential(
    NitroCredentialKind Kind,
    string? Value,
    NitroCredentialUnavailableReason UnavailableReason,
    string? UnavailableMessage)
{
    /// <summary>
    /// Creates a credential that sends an API key.
    /// </summary>
    public static NitroCredential FromApiKey(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        return new NitroCredential(
            NitroCredentialKind.ApiKey,
            apiKey,
            NitroCredentialUnavailableReason.None,
            UnavailableMessage: null);
    }

    /// <summary>
    /// Creates a credential that sends the access token of the Nitro CLI session.
    /// </summary>
    public static NitroCredential FromAccessToken(string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return new NitroCredential(
            NitroCredentialKind.AccessToken,
            accessToken,
            NitroCredentialUnavailableReason.None,
            UnavailableMessage: null);
    }

    /// <summary>
    /// Creates a credential that signals that the Nitro API cannot be contacted.
    /// </summary>
    public static NitroCredential Unavailable(
        NitroCredentialUnavailableReason reason,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new NitroCredential(NitroCredentialKind.None, Value: null, reason, message);
    }
}
