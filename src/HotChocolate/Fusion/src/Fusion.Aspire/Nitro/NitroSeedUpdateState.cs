namespace HotChocolate.Fusion.Aspire.Nitro;

internal sealed record NitroSeedCandidate(
    NitroGatewaySeed Seed,
    string VersionIdentity);

internal sealed record NitroSeedSnapshot(
    NitroGatewaySeed Seed,
    long Generation);

internal sealed record NitroSeedAdoption(
    NitroSeedSnapshot Previous,
    NitroSeedSnapshot Current,
    NitroSeedCandidate Candidate,
    bool WasStaged);

internal sealed record NitroSeedRefreshResult(
    NitroSeedCandidate? Candidate,
    string? FailureMessage)
{
    public static NitroSeedRefreshResult Downloaded(NitroSeedCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        return new NitroSeedRefreshResult(candidate, FailureMessage: null);
    }

    public static NitroSeedRefreshResult Failed(string failureMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureMessage);

        return new NitroSeedRefreshResult(Candidate: null, failureMessage);
    }
}
