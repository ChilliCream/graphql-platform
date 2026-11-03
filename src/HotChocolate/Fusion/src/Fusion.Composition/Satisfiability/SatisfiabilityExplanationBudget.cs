using System.Collections.Immutable;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// Limits the explanation entries that the satisfiability validators create while they search.
/// A budget that stops the search once it is exhausted leaves the search undecided and its explanation incomplete.
/// </summary>
internal sealed class SatisfiabilityExplanationBudget
{
    private readonly long _maxEntries;
    private readonly long _maxFailures;
    private readonly bool _createsEntries;
    private readonly bool _stopsSearch;
    private long _failureCount;

    private SatisfiabilityExplanationBudget(
        long maxEntries,
        bool createsEntries,
        bool stopsSearch,
        long maxFailures)
    {
        _maxEntries = maxEntries;
        _maxFailures = maxFailures;
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
        => new(long.MaxValue, createsEntries: true, stopsSearch: true, maxFailures: long.MaxValue);

    /// <summary>
    /// Creates a budget that creates at most <paramref name="maxEntries"/> entries and stops the search once
    /// they are used up.
    /// </summary>
    public static SatisfiabilityExplanationBudget Limited(int maxEntries)
        => new(maxEntries, createsEntries: true, stopsSearch: true, maxFailures: long.MaxValue);

    /// <summary>
    /// Creates a budget that creates at most <paramref name="maxEntries"/> entries and afterwards only the
    /// entries that hold created entries, and that stops the search after <paramref name="maxFailures"/>
    /// failures.
    /// </summary>
    public static SatisfiabilityExplanationBudget Pruning(int maxEntries, int maxFailures)
        => new(maxEntries, createsEntries: true, stopsSearch: false, maxFailures);

    /// <summary>
    /// Creates a budget for a search that only decides whether it fails, which creates no entries and
    /// is exhausted after <paramref name="maxEntries"/> failures, or never when it is <c>null</c>.
    /// </summary>
    public static SatisfiabilityExplanationBudget DecisionOnly(int? maxEntries)
        => new(maxEntries ?? long.MaxValue, createsEntries: false, stopsSearch: true, maxFailures: long.MaxValue);

    /// <summary>
    /// Creates an entry with <paramref name="message"/> and <paramref name="errors"/>, or returns
    /// <see cref="SatisfiabilityError.Unexplained"/> when the budget creates no entries or is used up and
    /// either stops the search or <paramref name="errors"/> hold no created entry.
    /// </summary>
    public SatisfiabilityError CreateError(string message, ImmutableArray<SatisfiabilityError>? errors = null)
    {
        if (++_failureCount > _maxFailures)
        {
            IsExhausted = true;
        }

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

    /// <summary>
    /// Creates an entry with <paramref name="message"/> and <paramref name="errors"/> for a failure that is
    /// known without its search, also when the budget is exhausted, which adds
    /// <see cref="SatisfiabilityError.Unexplained"/> to <paramref name="errors"/> unless they hold it.
    /// Returns <see cref="SatisfiabilityError.Unexplained"/> when the budget creates no entries.
    /// </summary>
    public SatisfiabilityError CreateKnownError(string message, ImmutableArray<SatisfiabilityError> errors)
    {
        EntryCount++;

        if (!_createsEntries)
        {
            return SatisfiabilityError.Unexplained;
        }

        if (IsExhausted && !errors.Contains(SatisfiabilityError.Unexplained))
        {
            errors = errors.Add(SatisfiabilityError.Unexplained);
        }

        return new SatisfiabilityError(message, errors);
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
