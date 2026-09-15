using System.Collections.Immutable;

namespace HotChocolate.Fusion.DirectiveMergers;

/// <summary>
/// Computes the per-argument fold rules used to derive the public <c>@listSize</c> directive
/// from the per-source <c>@listSize</c> usages of every serving source.
/// </summary>
internal static class ListSizeDirectiveFold
{
    /// <summary>
    /// Folds <c>assumedSize</c>: the maximum over sources that have it, absent (skipped) when
    /// none has it.
    /// </summary>
    public static int? FoldAssumedSize(IEnumerable<int?> values) => Max(values);

    /// <summary>
    /// Applies the <c>@fusion__cost_options(defaultListSize:)</c> composition setting to an
    /// already-folded <c>assumedSize</c> for a field that at least one serving source serves
    /// without a compatible <c>@listSize</c> usage of its own. When
    /// <paramref name="defaultListSize"/> is set, the sound bound is the greater of the folded
    /// value (treated as <c>0</c> when absent) and the default. When
    /// <paramref name="defaultListSize"/> is unbounded (<see langword="null"/>), the
    /// unannotated source's effective size is unknown, so the result is
    /// <see langword="null"/> (omitted) rather than the finite folded value, per the
    /// execution-schema RFC.
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
    /// Folds <c>requireOneSlicingArgument</c>: <c>true</c> if any (already-defaulted) source
    /// value is <c>true</c>, <c>false</c> if all are <c>false</c>, <c>null</c> if all are
    /// <c>null</c> or the sequence is empty. Callers substitute an omitted source usage with
    /// that source's own declared definition default before folding (R-REQUIRE-ONE-DEFAULT).
    /// </summary>
    public static bool? FoldRequireOneSlicingArgument(IEnumerable<bool?> values)
    {
        bool? result = null;

        foreach (var value in values)
        {
            switch (value)
            {
                case true:
                    return true; // Early exit - true wins regardless of the remaining values.
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
