namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Describes the outcome of reading the Nitro CLI session file.
/// </summary>
internal enum NitroSessionStatus
{
    /// <summary>
    /// The session file was read and carries an access token with an expiry.
    /// </summary>
    Available,

    /// <summary>
    /// The access token expired and could not be refreshed.
    /// </summary>
    Expired,

    /// <summary>
    /// No session file exists, which means the user never signed in on this machine.
    /// </summary>
    Missing,

    /// <summary>
    /// The session file exists but cannot be used, either because it could not be read or
    /// because it carries no access token or no expiry.
    /// </summary>
    Unusable
}

/// <summary>
/// The outcome of reading the Nitro CLI session file.
/// </summary>
/// <param name="Status">
/// Whether a usable session was read.
/// </param>
/// <param name="Session">
/// The session, set when <paramref name="Status"/> is
/// <see cref="NitroSessionStatus.Available"/> or <see cref="NitroSessionStatus.Expired"/>.
/// </param>
/// <param name="Message">
/// A message that names the session file and explains why no usable session was read. It is
/// <c>null</c> when <paramref name="Status"/> is <see cref="NitroSessionStatus.Available"/>.
/// </param>
internal sealed record NitroSessionReadResult(
    NitroSessionStatus Status,
    NitroSession? Session,
    string? Message);
