namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The outcomes <see cref="IProcessInfoProvider.Observe"/> classifies a
/// recorded generation into: <c>alive</c> (running, matching start time,
/// same observable process scope as this reader), <c>dead</c> (provably not
/// running in that same scope), or <c>unobservable</c> (this reader cannot
/// tell, typically a different PID namespace than the row's writer
/// recorded, or a permission failure reading the target process). Only
/// <c>dead</c> may ever be reaped.
/// </summary>
internal static class ProcessObservationResult
{
    public const string Alive = "alive";
    public const string Dead = "dead";
    public const string Unobservable = "unobservable";
}
