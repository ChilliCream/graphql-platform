namespace Mocha.Resources;

/// <summary>
/// Marker interface that distinguishes contributor sources from the composite that aggregates them.
/// </summary>
internal interface IMochaResourceSourceContributor
{
    MochaResourceSource Source { get; }
}

internal sealed class MochaResourceSourceContributor(MochaResourceSource source) : IMochaResourceSourceContributor
{
    public MochaResourceSource Source { get; } = source;
}
