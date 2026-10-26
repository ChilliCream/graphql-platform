using System.Collections.Immutable;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// Limits the explanation entries that the satisfiability validators create while they search.
/// Once it is exhausted, the validators stop searching and their result is an incomplete explanation.
/// </summary>
internal sealed class SatisfiabilityExplanationBudget
{
    private readonly long _maxEntries;
    private readonly bool _createsEntries;

    private SatisfiabilityExplanationBudget(long maxEntries, bool createsEntries)
    {
        _maxEntries = maxEntries;
        _createsEntries = createsEntries;
    }

    /// <summary>
    /// Gets the number of entries counted against the budget.
    /// </summary>
    public long EntryCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether an entry was refused because the budget was used up.
    /// </summary>
    public bool IsExhausted { get; private set; }

    /// <summary>
    /// Creates a budget that creates every entry and is never exhausted.
    /// </summary>
    public static SatisfiabilityExplanationBudget Unlimited() => new(long.MaxValue, createsEntries: true);

    /// <summary>
    /// Creates a budget that creates at most <paramref name="maxEntries"/> entries.
    /// </summary>
    public static SatisfiabilityExplanationBudget Limited(int maxEntries) => new(maxEntries, createsEntries: true);

    /// <summary>
    /// Creates a budget for a search that only decides whether it fails, which creates no entries and
    /// is exhausted after <paramref name="maxEntries"/> failures, or never when it is <c>null</c>.
    /// </summary>
    public static SatisfiabilityExplanationBudget DecisionOnly(int? maxEntries)
        => new(maxEntries ?? long.MaxValue, createsEntries: false);

    /// <summary>
    /// Creates an entry with <paramref name="message"/> and <paramref name="errors"/>, or returns
    /// <see cref="SatisfiabilityError.Unexplained"/> when the budget creates no entries or is used up.
    /// </summary>
    public SatisfiabilityError CreateError(string message, ImmutableArray<SatisfiabilityError>? errors = null)
    {
        if (EntryCount == _maxEntries)
        {
            IsExhausted = true;
            return SatisfiabilityError.Unexplained;
        }

        EntryCount++;

        return _createsEntries ? new SatisfiabilityError(message, errors) : SatisfiabilityError.Unexplained;
    }
}
