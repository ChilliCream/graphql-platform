using System.Net;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Describes why a fusion configuration download succeeded or failed. The distinction lets the
/// caller word its message and pick its fallback without inspecting strings.
/// </summary>
internal enum NitroDownloadStatus
{
    /// <summary>
    /// The download completed and the payload was written to the destination.
    /// </summary>
    Success,

    /// <summary>
    /// Nitro rejected the credential. Not retried.
    /// </summary>
    Unauthorized,

    /// <summary>
    /// Nitro has no fusion configuration for the requested api and stage. Not retried.
    /// </summary>
    NotFound,

    /// <summary>
    /// The download kept failing with a server or connection error until the retry budget was
    /// exhausted.
    /// </summary>
    TransientExhausted,

    /// <summary>
    /// The download failed for a reason that a retry cannot fix. Not retried.
    /// </summary>
    PermanentFailure
}

/// <summary>
/// The outcome of a fusion configuration download.
/// </summary>
/// <param name="Status">
/// Why the download succeeded or failed.
/// </param>
/// <param name="StatusCode">
/// The HTTP status code of the last attempt, or <c>null</c> when the request never reached the
/// server.
/// </param>
/// <param name="Attempts">
/// The number of attempts that were made.
/// </param>
/// <param name="Message">
/// A message that describes the failure. It is <c>null</c> when <paramref name="Status"/> is
/// <see cref="NitroDownloadStatus.Success"/>.
/// </param>
internal sealed record NitroDownloadResult(
    NitroDownloadStatus Status,
    HttpStatusCode? StatusCode,
    int Attempts,
    string? Message);
