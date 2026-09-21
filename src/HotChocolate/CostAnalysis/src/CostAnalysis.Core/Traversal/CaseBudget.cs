namespace HotChocolate.CostAnalysis;

/// <summary>
/// Limits the number of Boolean case splits allowed when compiling an operation.
/// A non-positive limit is exhausted immediately.
/// </summary>
internal sealed class CaseBudget(int limit)
{
    private int _spent;

    public int Remaining => Math.Max(0, limit - _spent);

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

    public bool CanCompleteIndependentDecision(int variableCount)
    {
        var requiredSplits = variableCount >= 31
            ? long.MaxValue
            : (1L << variableCount) - 1;

        if (requiredSplits <= Remaining)
        {
            return true;
        }

        IsExhausted = true;
        return false;
    }
}
