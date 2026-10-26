using System.Collections.Immutable;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// Limits the explanation entries that the satisfiability validators create while they search.
/// A budget that stops the search once it is exhausted leaves the search undecided and its explanation incomplete.
/// </summary>
internal sealed class SatisfiabilityExplanationBudget
{
    private readonly long _maxEntries;
    private readonly bool _createsEntries;
    private readonly bool _stopsSearch;

    private SatisfiabilityExplanationBudget(long maxEntries, bool createsEntries, bool stopsSearch)
    {
        _maxEntries = maxEntries;
        _createsEntries = createsEntries;
        _stopsSearch = stopsSearch;
    }

    /// <summary>
    /// Gets the number of entries counted against the budget.
    /// </summary>
    public long EntryCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the budget was used up and stops the search.
    /// </summary>
    public bool IsExhausted { get; private set; }

    /// <summary>
    /// Creates a budget that creates every entry and is never exhausted.
    /// </summary>
    public static SatisfiabilityExplanationBudget Unlimited()
        => new(long.MaxValue, createsEntries: true, stopsSearch: true);

    /// <summary>
    /// Creates a budget that creates at most <paramref name="maxEntries"/> entries. Once they are used up, it
    /// either stops the search or lets it finish and only creates the entries that hold created entries.
    /// </summary>
    public static SatisfiabilityExplanationBudget Limited(int maxEntries, bool stopsSearch)
        => new(maxEntries, createsEntries: true, stopsSearch);

    /// <summary>
    /// Creates a budget for a search that only decides whether it fails, which creates no entries and
    /// is exhausted after <paramref name="maxEntries"/> failures, or never when it is <c>null</c>.
    /// </summary>
    public static SatisfiabilityExplanationBudget DecisionOnly(int? maxEntries)
        => new(maxEntries ?? long.MaxValue, createsEntries: false, stopsSearch: true);

    /// <summary>
    /// Creates an entry with <paramref name="message"/> and <paramref name="errors"/>, or returns
    /// <see cref="SatisfiabilityError.Unexplained"/> when the budget creates no entries or is used up and
    /// either stops the search or <paramref name="errors"/> hold no created entry.
    /// </summary>
    public SatisfiabilityError CreateError(string message, ImmutableArray<SatisfiabilityError>? errors = null)
    {
        if (EntryCount >= _maxEntries)
        {
            if (_stopsSearch)
            {
                IsExhausted = true;
                return SatisfiabilityError.Unexplained;
            }

            if (errors is not { } children || !HoldsCreatedEntry(children))
            {
                return SatisfiabilityError.Unexplained;
            }
        }

        EntryCount++;

        return _createsEntries ? new SatisfiabilityError(message, errors) : SatisfiabilityError.Unexplained;
    }

    private static bool HoldsCreatedEntry(ImmutableArray<SatisfiabilityError> errors)
    {
        foreach (var error in errors)
        {
            if (!ReferenceEquals(error, SatisfiabilityError.Unexplained))
            {
                return true;
            }
        }

        return false;
    }
}
