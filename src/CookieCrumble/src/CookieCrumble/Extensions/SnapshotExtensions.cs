using CookieCrumble.Formatters;

namespace CookieCrumble;

public static class SnapshotExtensions
{
    public static void MatchInlineSnapshot(
        this object? value,
        string snapshot,
        ISnapshotValueFormatter? formatter = null)
        => Snapshot.Create().Add(value, formatter: formatter).MatchInline(snapshot);

    public static void MatchInlineSnapshots(
        this IEnumerable<object?> values,
        IEnumerable<string> snapshots,
        ISnapshotValueFormatter? formatter = null)
    {
        var valuesArray = values.ToArray();
        var snapshotsArray = snapshots.ToArray();

        if (valuesArray.Length != snapshotsArray.Length)
        {
            throw new ArgumentException(
                $"The number of snapshots must be the same as the number of values ({valuesArray.Length}).",
                nameof(snapshots));
        }

        var i = 0;
        List<Exception> exceptions = [];

        foreach (var value in valuesArray)
        {
            try
            {
                Snapshot
                    .Create()
                    .Add(value, formatter: formatter)
                    .MatchInline(snapshotsArray[i++]);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count != 0)
        {
            throw new AggregateException(exceptions);
        }
    }

    public static void MatchSnapshot(this Snapshot value)
        => value.Match();

    public static void MatchSnapshot(
        this object? value,
        object? postFix = null,
        string? extension = null,
        ISnapshotValueFormatter? formatter = null)
        => Snapshot.Match(value, postFix?.ToString(), extension, formatter);

    public static void MatchMarkdownSnapshot(
        this object? value,
        object? postFix = null,
        string? extension = null,
        ISnapshotValueFormatter? formatter = null)
        => Snapshot.Create(postFix?.ToString(), extension).Add(value, formatter: formatter).MatchMarkdown();

    public static void MatchMarkdownSnapshot(this Snapshot value)
        => value.MatchMarkdown();
}
