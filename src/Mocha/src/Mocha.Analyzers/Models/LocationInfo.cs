namespace Mocha.Analyzers;

/// <summary>
/// An equatable representation of a source location that can safely
/// flow through the incremental pipeline without rooting Roslyn objects.
/// </summary>
/// <param name="FilePath">The file path of the source location.</param>
/// <param name="StartLine">The zero-based start line number.</param>
/// <param name="StartColumn">The zero-based start column number.</param>
/// <param name="EndLine">The zero-based end line number.</param>
/// <param name="EndColumn">The zero-based end column number.</param>
public sealed record LocationInfo(string FilePath, int StartLine, int StartColumn, int EndLine, int EndColumn)
    : IComparable<LocationInfo>
{
    /// <summary>
    /// Orders locations by file path (ordinal), then by start and end position. The ordering
    /// covers every field so it is consistent with value equality, and it is deterministic across
    /// compilations and machines, which reproducible builds and snapshot tests depend on.
    /// </summary>
    public int CompareTo(LocationInfo? other)
    {
        if (other is null)
        {
            return 1;
        }

        var pathComparison = string.CompareOrdinal(FilePath, other.FilePath);

        if (pathComparison != 0)
        {
            return pathComparison;
        }

        if (StartLine != other.StartLine)
        {
            return StartLine < other.StartLine ? -1 : 1;
        }

        if (StartColumn != other.StartColumn)
        {
            return StartColumn < other.StartColumn ? -1 : 1;
        }

        if (EndLine != other.EndLine)
        {
            return EndLine < other.EndLine ? -1 : 1;
        }

        if (EndColumn != other.EndColumn)
        {
            return EndColumn < other.EndColumn ? -1 : 1;
        }

        return 0;
    }

    /// <summary>
    /// Returns the smaller of two locations by <see cref="CompareTo"/> ordering, treating a
    /// <see langword="null"/> location as absent so the other is returned.
    /// </summary>
    public static LocationInfo? Min(LocationInfo? first, LocationInfo? second)
    {
        if (first is null)
        {
            return second;
        }

        if (second is null)
        {
            return first;
        }

        return first.CompareTo(second) <= 0 ? first : second;
    }
}
