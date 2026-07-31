namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// The fusion configuration that a gateway composes against for the lifetime of one run. It is a
/// private copy, so it stays stable while the cache it came from is replaced, and it is never
/// written to.
/// </summary>
/// <param name="ApiId">
/// The id of the Nitro api the fusion configuration belongs to.
/// </param>
/// <param name="Stage">
/// The name of the stage the fusion configuration was fetched for. The settings of the source
/// schemas it carries resolve against this environment.
/// </param>
/// <param name="FilePath">
/// The full path of the private copy of the fusion archive.
/// </param>
/// <param name="DownloadedAt">
/// The point in time at which the fusion configuration was downloaded from Nitro, which tells the
/// developer how old it is.
/// </param>
/// <param name="IsFresh">
/// Whether the fusion configuration was downloaded for this run instead of taken from the cache.
/// </param>
internal sealed record NitroGatewaySeed(
    string ApiId,
    string Stage,
    string FilePath,
    DateTimeOffset DownloadedAt,
    bool IsFresh);

/// <summary>
/// The outcome of acquiring the fusion configuration of a gateway.
/// </summary>
/// <param name="Seed">
/// The fusion configuration, or <c>null</c> when it could not be acquired.
/// </param>
/// <param name="FailureMessage">
/// The message that the gateway fails with, or <c>null</c> when the fusion configuration was
/// acquired.
/// </param>
internal sealed record NitroSeedAcquisition(NitroGatewaySeed? Seed, string? FailureMessage)
{
    public static NitroSeedAcquisition Acquired(NitroGatewaySeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);

        return new NitroSeedAcquisition(seed, FailureMessage: null);
    }

    public static NitroSeedAcquisition Failed(string failureMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureMessage);

        return new NitroSeedAcquisition(Seed: null, failureMessage);
    }
}
