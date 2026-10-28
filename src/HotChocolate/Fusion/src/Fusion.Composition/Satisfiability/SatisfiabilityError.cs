using System.Collections.Immutable;
using System.Text;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// An entry of a satisfiability error explanation, with the entries that explain it.
/// </summary>
internal sealed class SatisfiabilityError
{
    /// <summary>
    /// The maximum number of lines that <see cref="ToString()"/> renders before it cuts the explanation.
    /// </summary>
    private const int MaxLines = 1000;

    /// <summary>
    /// The maximum number of entries that <see cref="ToString()"/> renders directly below one entry.
    /// </summary>
    private const int MaxEntriesPerLevel = 100;

    private readonly ImmutableArray<SatisfiabilityError> _errors;
    private readonly int _omittedCount;

    public SatisfiabilityError(string message, ImmutableArray<SatisfiabilityError>? errors = null)
    {
        Message = message;
        _errors = errors ?? [];
    }

    private SatisfiabilityError(int omittedCount)
    {
        Message = string.Empty;
        _errors = [];
        _omittedCount = omittedCount;
    }

    /// <summary>
    /// Gets an entry that stands for one entry that was not created.
    /// </summary>
    public static SatisfiabilityError Unexplained { get; } = new(omittedCount: 1);

    public string Message { get; }

    public override string ToString() => ToString(MaxLines, MaxEntriesPerLevel);

    /// <summary>
    /// Renders the explanation, cut after <paramref name="maxLines"/> lines and after
    /// <paramref name="maxEntriesPerLevel"/> entries below one entry. Each cut level ends with an
    /// "... and N more" line that counts the entries it leaves out at that level.
    /// </summary>
    internal string ToString(int maxLines, int maxEntriesPerLevel)
    {
        var builder = new StringBuilder();
        var remainingLines = maxLines;
        Render(builder, indentationLevel: 0, maxEntriesPerLevel, ref remainingLines);
        return builder.ToString();
    }

    /// <summary>
    /// Renders this entry and its entries, and returns <c>false</c> when the line cap cut them.
    /// </summary>
    private bool Render(
        StringBuilder builder,
        int indentationLevel,
        int maxEntriesPerLevel,
        ref int remainingLines)
    {
        AppendLine(builder, indentationLevel, Message);
        remainingLines--;

        var rendered = 0;
        var omitted = 0;
        var complete = true;

        foreach (var error in _errors)
        {
            if (error._omittedCount > 0)
            {
                omitted += error._omittedCount;
                continue;
            }

            if (!complete || rendered == maxEntriesPerLevel)
            {
                omitted++;
                continue;
            }

            if (remainingLines <= 0)
            {
                complete = false;
                omitted++;
                continue;
            }

            complete = error.Render(builder, indentationLevel + 1, maxEntriesPerLevel, ref remainingLines);
            rendered++;
        }

        if (omitted > 0)
        {
            AppendLine(builder, indentationLevel + 1, string.Format(SatisfiabilityError_AndMore, omitted));
            remainingLines--;
        }

        return complete;
    }

    private static void AppendLine(StringBuilder builder, int indentationLevel, string text)
    {
        if (indentationLevel > 0)
        {
            builder.Append('\n');
            builder.Append(' ', indentationLevel * 2);
        }

        builder.Append(text);
    }
}
