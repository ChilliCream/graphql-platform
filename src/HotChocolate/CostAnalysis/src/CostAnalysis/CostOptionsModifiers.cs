using System.Collections.Immutable;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Holds the per-request cost option modifiers added through <c>ModifyCostOptions</c>.
/// </summary>
internal sealed class CostOptionsModifiers
{
    private readonly ImmutableArray<Action<CostOptions>> _modifiers;

    /// <summary>
    /// Creates a new instance holding a single modifier.
    /// </summary>
    /// <param name="configure">
    /// The modifier to hold.
    /// </param>
    public CostOptionsModifiers(Action<CostOptions> configure)
    {
        _modifiers = [configure];
    }

    private CostOptionsModifiers(ImmutableArray<Action<CostOptions>> modifiers)
    {
        _modifiers = modifiers;
    }

    /// <summary>
    /// Creates a new instance with <paramref name="configure"/> appended.
    /// </summary>
    /// <param name="configure">
    /// The modifier to append.
    /// </param>
    /// <returns>
    /// Returns a new instance holding every modifier of this instance, followed by
    /// <paramref name="configure"/>.
    /// </returns>
    public CostOptionsModifiers With(Action<CostOptions> configure)
        => new(_modifiers.Add(configure));

    /// <summary>
    /// Applies every modifier to <paramref name="options"/>, in the order they were added.
    /// </summary>
    /// <param name="options">
    /// The cost options to mutate.
    /// </param>
    public void Apply(CostOptions options)
    {
        foreach (var modifier in _modifiers)
        {
            modifier(options);
        }
    }
}
