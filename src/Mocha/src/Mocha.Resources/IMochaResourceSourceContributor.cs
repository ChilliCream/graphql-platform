namespace Mocha.Resources;

/// <summary>
/// Internal marker interface that lets DI distinguish contributor sources (which feed the
/// composite) from the composite itself.
/// </summary>
/// <remarks>
/// Avoids the recursion that would result from registering both the composite and its children
/// against the same <see cref="MochaResourceSource"/> service type — when the composite resolves
/// its child set it asks for <see cref="IMochaResourceSourceContributor"/> instead.
/// </remarks>
internal interface IMochaResourceSourceContributor
{
    MochaResourceSource Source { get; }
}

internal sealed class MochaResourceSourceContributor(MochaResourceSource source) : IMochaResourceSourceContributor
{
    public MochaResourceSource Source { get; } = source;
}
