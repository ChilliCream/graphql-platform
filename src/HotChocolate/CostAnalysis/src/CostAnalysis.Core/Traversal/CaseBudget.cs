namespace HotChocolate.CostAnalysis;

/// <summary>
/// Tracks the number of exact cases spent while compiling one operation and
/// reports when the case budget is exhausted. One instance is shared across
/// every boundary of a single compile.
/// </summary>
internal sealed class CaseBudget(int limit)
{
    private int _spent;

    /// <summary>
    /// Gets a value indicating whether the budget has been exhausted. Once
    /// set, it stays set for the rest of the compile.
    /// </summary>
    public bool IsExhausted { get; private set; } = limit <= 0;

    /// <summary>
    /// Attempts to spend one exact case.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the case was spent; <see langword="false"/>,
    /// setting <see cref="IsExhausted"/>, once the configured limit of exact
    /// cases has already been spent.
    /// </returns>
    public bool TrySpend()
    {
        if (IsExhausted)
        {
            return false;
        }

        if (_spent >= limit)
        {
            IsExhausted = true;
            return false;
        }

        _spent++;
        return true;
    }
}
