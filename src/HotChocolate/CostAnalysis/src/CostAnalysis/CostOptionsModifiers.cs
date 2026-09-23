namespace HotChocolate.CostAnalysis;

/// <summary>
/// Holds the per-request cost option modifiers added through <c>ModifyCostOptions</c>.
/// </summary>
internal sealed class CostOptionsModifiers
{
    private readonly List<Action<CostOptions>> _modifiers = [];

    /// <summary>
    /// Adds a modifier to the end of the list.
    /// </summary>
    /// <param name="configure">
    /// The modifier to add.
    /// </param>
    public void Add(Action<CostOptions> configure) => _modifiers.Add(configure);

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
