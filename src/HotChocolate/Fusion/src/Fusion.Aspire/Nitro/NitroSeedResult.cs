namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Describes where the fusion configuration that a gateway composes against came from.
/// </summary>
internal enum NitroSeedOutcome
{
    /// <summary>
    /// A fresh fusion configuration was downloaded from Nitro.
    /// </summary>
    Downloaded,

    /// <summary>
    /// A fresh fusion configuration could not be fetched, so the last cached one is used.
    /// </summary>
    ServedFromCache,

    /// <summary>
    /// A fresh fusion configuration could not be fetched and no cached one exists.
    /// </summary>
    Unavailable
}

/// <summary>
/// The fusion configuration that a gateway composes against.
/// </summary>
/// <param name="Outcome">
/// Where the fusion configuration came from.
/// </param>
/// <param name="FilePath">
/// The full path of the fusion archive, or <c>null</c> when <paramref name="Outcome"/> is
/// <see cref="NitroSeedOutcome.Unavailable"/>.
/// </param>
/// <param name="DownloadedAt">
/// The point in time at which the fusion configuration was downloaded, or <c>null</c> when
/// <paramref name="Outcome"/> is <see cref="NitroSeedOutcome.Unavailable"/>.
/// </param>
/// <param name="Message">
/// The reason why a fresh fusion configuration could not be fetched. It is <c>null</c> when
/// <paramref name="Outcome"/> is <see cref="NitroSeedOutcome.Downloaded"/>, and it is the message
/// to fail the gateway with when <paramref name="Outcome"/> is
/// <see cref="NitroSeedOutcome.Unavailable"/>.
/// </param>
internal sealed record NitroSeedResult(
    NitroSeedOutcome Outcome,
    string? FilePath,
    DateTimeOffset? DownloadedAt,
    string? Message);
