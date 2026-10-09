using System.Collections.Immutable;

namespace HotChocolate.Fusion.DirectiveMergers;

/// <summary>
/// Combines source list-size settings into a public <c>@listSize</c> directive.
/// </summary>
internal static class ListSizeDirectiveFold
{
    /// <summary>
    /// Folds <c>assumedSize</c>: the maximum over sources that have it, absent (skipped) when
    /// none has it.
    /// </summary>
    public static int? FoldAssumedSize(IEnumerable<int?> values) => Max(values);

    /// <summary>
    /// Returns the larger of the folded assumed size and the configured default list size.
    /// A missing assumed size counts as zero. An unbounded default
    /// (<see langword="null"/>) returns <see langword="null"/>.
    /// </summary>
    public static int? ApplyDefaultListSize(int? foldedAssumedSize, int? defaultListSize)
        => defaultListSize is { } value ? Math.Max(foldedAssumedSize ?? 0, value) : null;

    /// <summary>
    /// Folds <c>slicingArgumentDefaultValue</c>: the maximum over sources that have it, absent
    /// (skipped) when none has it. No spec default exists for this ChilliCream extension.
    /// </summary>
    public static int? FoldSlicingArgumentDefaultValue(IEnumerable<int?> values) => Max(values);

    /// <summary>
    /// Folds <c>slicingArguments</c> or <c>sizedFields</c>: the union of every source's names,
    /// in first-seen order.
    /// </summary>
    public static ImmutableArray<string> FoldNames(IEnumerable<ImmutableArray<string>> perSource)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = ImmutableArray.CreateBuilder<string>();

        foreach (var names in perSource)
        {
            foreach (var name in names)
            {
                if (seen.Add(name))
                {
                    result.Add(name);
                }
            }
        }

        return result.ToImmutable();
    }

    /// <summary>
    /// Returns <see langword="true"/> if any source requires a slicing argument,
    /// <see langword="false"/> if only false and null values occur, or <see langword="null"/>
    /// if all values are null or the sequence is empty. Inputs must include each source's applicable default.
    /// </summary>
    public static bool? FoldRequireOneSlicingArgument(IEnumerable<bool?> values)
    {
        bool? result = null;

        foreach (var value in values)
        {
            switch (value)
            {
                case true:
                    return true;
                case false:
                    result = false;
                    break;
            }
        }

        return result;
    }

    private static int? Max(IEnumerable<int?> values)
    {
        int? max = null;

        foreach (var value in values)
        {
            if (value is null)
            {
                continue;
            }

            if (max is null || value > max)
            {
                max = value;
            }
        }

        return max;
    }
}
