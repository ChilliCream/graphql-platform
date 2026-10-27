using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Renders the Mail table's header row and each thread row: subject, from, to, message
/// count, and last-activity columns. Narrow widths drop To first, then From, then
/// Messages, identically for the header and the rows so the two always agree. Built on
/// the shared <see cref="TableLayout"/> and <see cref="TableRenderer"/> table widget.
/// </summary>
internal static class MailTable
{
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";

    private const string SubjectHeader = "SUBJECT";
    private const string FromHeader = "FROM";
    private const string ToHeader = "TO";
    private const string MessagesHeader = "MESSAGES";
    private const string LastActivityHeader = "LAST ACTIVITY";

    private const int MinSubjectWidth = 24;
    private const int MinFromWidth = 10;
    private const int MinToWidth = 10;
    private const int MinMessagesWidth = 8;
    private const int MinLastActivityWidth = 13;

    private const string NowLabel = "now";
    private const string JustNowLabel = "just now";
    private const string AgoSuffix = " ago";
    private const string IsoDateFormat = "yyyy-MM-dd";

    // To drops first, then From, then Messages; Subject and Last Activity have no drop
    // priority so they are never dropped.
    private static readonly IReadOnlyList<TableColumnSpec> s_columns =
    [
        new TableColumnSpec(SubjectHeader, MinSubjectWidth),
        new TableColumnSpec(FromHeader, MinFromWidth, DropPriority: 1),
        new TableColumnSpec(ToHeader, MinToWidth, DropPriority: 0),
        new TableColumnSpec(MessagesHeader, MinMessagesWidth, DropPriority: 2, Alignment: ColumnAlignment.Right),
        new TableColumnSpec(LastActivityHeader, MinLastActivityWidth)
    ];

    /// <summary>
    /// The column widths a set of rows agree on: each column padded to at least its minimum
    /// width, and wider still when its header title or a row's value needs more room.
    /// </summary>
    public readonly record struct Widths(int Subject, int From, int To, int Messages, int LastActivity);

    /// <summary>
    /// Computes <see cref="Widths"/> across <paramref name="rows"/>, with each column no
    /// narrower than its minimum width or its header title.
    /// </summary>
    public static Widths ComputeWidths(IReadOnlyList<MailThreadSummary> rows, DateTimeOffset now)
    {
        var rowValues = new List<IReadOnlyList<string>>(rows.Count);

        foreach (var row in rows)
        {
            rowValues.Add(
            [
                row.Subject,
                row.LastSender,
                FormatRecipients(row.LastRecipients),
                FormatMessageCount(row.MessageCount),
                FormatActivityAge(row.LastMessageAt, now)
            ]);
        }

        var widths = TableLayout.ComputeWidths(s_columns, rowValues);
        return new Widths(widths[0], widths[1], widths[2], widths[3], widths[4]);
    }

    /// <summary>
    /// Builds the markup line for one thread row. Columns are padded to <paramref name="widths"/>.
    /// When the full set of columns does not fit within <paramref name="maxWidth"/> display
    /// columns, To is dropped first, then From, then Messages; Subject and Last Activity
    /// always remain, with Subject truncated as a last resort. A <paramref name="maxWidth"/>
    /// of 0 or less produces an empty line.
    /// </summary>
    public static string Render(MailThreadSummary row, DateTimeOffset now, bool selected, int maxWidth, Widths widths)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var prefix = selected ? SelectedPrefix : UnselectedPrefix;
        var subjectStyle = ThemeTokens.GetStyle("mail.list.subject").ToMarkup();
        var fromStyle = ThemeTokens.GetStyle("mail.list.from").ToMarkup();
        var toStyle = ThemeTokens.GetStyle("mail.list.to").ToMarkup();
        var messagesStyle = ThemeTokens.GetStyle("mail.list.messages").ToMarkup();
        var ageStyle = ThemeTokens.GetStyle("mail.list.age").ToMarkup();

        var cells = new TableCellSpec[]
        {
            new(row.Subject, subjectStyle),
            new(row.LastSender, fromStyle),
            new(FormatRecipients(row.LastRecipients), toStyle),
            new(FormatMessageCount(row.MessageCount), messagesStyle),
            new(FormatActivityAge(row.LastMessageAt, now), ageStyle)
        };

        var budget = maxWidth - PrefixWidth(prefix);
        var layout = TableLayout.Plan(budget, s_columns, ToWidthList(widths));
        var line = TableRenderer.RenderRow(prefix, new TableCellSpec(string.Empty), cells, s_columns, layout);

        if (selected)
        {
            var highlightStyle = ThemeTokens.GetStyle("selection.highlight").ToMarkup();
            line = TableRenderer.Stylize(highlightStyle, line);
        }

        return line;
    }

    /// <summary>
    /// Builds the header title line shown above the rows: SUBJECT, FROM, TO, MESSAGES,
    /// LAST ACTIVITY, aligned to <paramref name="widths"/>. Columns are dropped using the
    /// same thresholds as <see cref="Render"/>, so the header always agrees with the rows
    /// below it. A <paramref name="maxWidth"/> of 0 or less produces an empty line.
    /// </summary>
    public static string RenderHeader(int maxWidth, Widths widths)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var headerStyle = ThemeTokens.GetStyle("mail.list.header").ToMarkup();
        var budget = maxWidth - PrefixWidth(UnselectedPrefix);
        var layout = TableLayout.Plan(budget, s_columns, ToWidthList(widths));

        return TableRenderer.RenderRow(
            UnselectedPrefix, new TableCellSpec(string.Empty), BuildHeaderCells(headerStyle), s_columns, layout);
    }

    /// <summary>
    /// Builds the dashed rule line under the header, filling <paramref name="maxWidth"/>
    /// display columns. A <paramref name="maxWidth"/> of 0 or less produces an empty line.
    /// </summary>
    public static string RenderRule(int maxWidth)
    {
        var ruleStyle = ThemeTokens.GetStyle("mail.list.age").ToMarkup();
        return TableRenderer.RenderRule(maxWidth, ruleStyle);
    }

    /// <summary>
    /// Appends the Mail table's fixed top block to <paramref name="lines"/>: a blank line, the
    /// header row and the rule, keeping only the first <paramref name="headerLineCount"/> of the
    /// three. A <paramref name="maxWidth"/> of 0 or less appends nothing.
    /// </summary>
    public static void AddHeaderLines(List<string> lines, int headerLineCount, int maxWidth, Widths widths)
    {
        if (maxWidth <= 0)
        {
            return;
        }

        var headerStyle = ThemeTokens.GetStyle("mail.list.header").ToMarkup();
        var ruleStyle = ThemeTokens.GetStyle("mail.list.age").ToMarkup();
        var budget = maxWidth - PrefixWidth(UnselectedPrefix);
        var layout = TableLayout.Plan(budget, s_columns, ToWidthList(widths));

        TableRenderer.RenderTopBlock(
            lines,
            headerLineCount,
            maxWidth,
            UnselectedPrefix,
            new TableCellSpec(string.Empty),
            s_columns,
            layout,
            headerStyle,
            ruleStyle);
    }

    private static TableCellSpec[] BuildHeaderCells(string headerStyle) =>
    [
        new(SubjectHeader, headerStyle),
        new(FromHeader, headerStyle),
        new(ToHeader, headerStyle),
        new(MessagesHeader, headerStyle),
        new(LastActivityHeader, headerStyle)
    ];

    private static int PrefixWidth(string prefix) => DisplayWidth.Measure(prefix) + 1;

    private static IReadOnlyList<int> ToWidthList(Widths widths) =>
        [widths.Subject, widths.From, widths.To, widths.Messages, widths.LastActivity];

    private static string FormatMessageCount(int count) => count.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns the first recipient name with a count of additional names, or an empty
    /// string for an empty list.
    /// </summary>
    private static string FormatRecipients(IReadOnlyList<string> names)
    {
        if (names.Count == 0)
        {
            return string.Empty;
        }

        var overflow = names.Count - 1;
        return overflow > 0 ? $"{names[0]}+{overflow}" : names[0];
    }

    /// <summary>
    /// Formats the elapsed time between <paramref name="value"/> and <paramref name="now"/>:
    /// "just now" under a minute, a relative duration ("26m", "3h", "2d") with an " ago"
    /// suffix, or an absolute date once the thread's last activity is a week or older.
    /// </summary>
    private static string FormatActivityAge(DateTimeOffset value, DateTimeOffset now)
    {
        var formatted = MailAges.Format(value, now);

        if (formatted == NowLabel)
        {
            return JustNowLabel;
        }

        return IsAbsoluteDate(formatted) ? formatted : formatted + AgoSuffix;
    }

    private static bool IsAbsoluteDate(string formatted) =>
        DateTime.TryParseExact(
            formatted, IsoDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
