using System.Text;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Renders aligned mail headings, thread summaries, and message rows as markup.
/// </summary>
internal static class MailTable
{
    private const string Ellipsis = "…";
    private const char EllipsisChar = '…';
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";
    private const string ExpandedFoldGlyph = "▾";
    private const string CollapsedFoldGlyph = "▸";
    private const string NoFoldGlyph = " ";
    private const string UnreadToMeMarker = "●";
    private const string ReadMarker = " ";

    /// <summary>
    /// Thread-membership indicator for an indented, expanded-thread child
    /// row, distinct from the from-me/direct/broadcast relationship glyph.
    /// </summary>
    private const string ThreadChildGlyph = "└";

    private const int SelectionWidth = 2;
    private const int FoldWidth = 2;
    private const int MarkerWidth = 2;
    private const int GlyphWidth = 2;
    private const int FromWidth = 14;
    private const int ToWidth = 14;
    private const int AgeWidth = 10;
    private const int CountWidth = 5;
    private const int ColumnGap = 1;
    private const int MinElasticWidth = 3;

    /// <summary>
    /// The subject/preview elastic split: subject gets two fifths of
    /// whatever remains after the fixed-width columns, preview gets the
    /// rest.
    /// </summary>
    private const int SubjectShareNumerator = 2;
    private const int SubjectShareDenominator = 5;

    /// <summary>
    /// The column widths and count-column visibility shared by a heading and its rows.
    /// </summary>
    public readonly record struct Columns(
        int PrefixWidth, int FromWidth, int ToWidth, int SubjectWidth, int PreviewWidth, int AgeWidth,
        int CountWidth, bool ShowCount);

    /// <summary>
    /// Computes column widths with an optional count column, allocating remaining
    /// space to Subject and Preview. Fixed columns can exceed the available width;
    /// elastic columns shrink to zero.
    /// </summary>
    public static Columns ComputeColumns(int contentWidth, bool showCount)
    {
        const int prefixWidth = SelectionWidth + FoldWidth + MarkerWidth + GlyphWidth;
        var countBudget = showCount ? CountWidth + ColumnGap : 0;

        var fixedWidth = prefixWidth
            + FromWidth + ColumnGap
            + ToWidth + ColumnGap
            + AgeWidth + ColumnGap
            + countBudget;

        // Reserve the remaining gap between the elastic columns.
        var elastic = Math.Max(0, contentWidth - fixedWidth - ColumnGap);
        var subjectWidth = elastic <= 0 ? 0 : Math.Min(elastic, Math.Max(MinElasticWidth, elastic * SubjectShareNumerator / SubjectShareDenominator));
        var previewWidth = Math.Max(0, elastic - subjectWidth);

        return new Columns(prefixWidth, FromWidth, ToWidth, subjectWidth, previewWidth, AgeWidth, CountWidth, showCount);
    }

    /// <summary>
    /// Renders the heading row: column labels aligned to <paramref name="columns"/>,
    /// styled via <c>mail.row.heading</c>. The prefix area (selection, fold,
    /// unread, and relationship columns) carries no label.
    /// </summary>
    public static string RenderHeading(Columns columns)
    {
        var cells = new List<string>
        {
            new string(' ', columns.PrefixWidth - 1),
            Pad("From", columns.FromWidth),
            Pad("To", columns.ToWidth),
            Pad("Subject", columns.SubjectWidth),
            Pad("Preview", columns.PreviewWidth),
            Pad("Age", columns.AgeWidth)
        };

        if (columns.ShowCount)
        {
            cells.Add(PadLeft("#", columns.CountWidth));
        }

        var plain = string.Join(' ', cells).TrimEnd();
        return Stylize(ThemeTokens.GetStyle("mail.row.heading").ToMarkup(), Markup.Escape(plain));
    }

    /// <summary>
    /// Renders a thread summary using the supplied expansion, selection, and
    /// actor-specific unread state.
    /// </summary>
    public static string RenderThreadRow(
        MailThreadSummary summary,
        bool expanded,
        bool unreadToMe,
        bool selected,
        string actor,
        DateTimeOffset now,
        Columns columns)
    {
        var prefix = BuildPrefix(
            selected,
            foldGlyph: expanded ? ExpandedFoldGlyph : CollapsedFoldGlyph,
            unreadToMe,
            glyph: ThreadRelationshipGlyph(summary, actor));

        var from = Pad(summary.LastSender, columns.FromWidth);
        var to = Pad(FormatOverflowList(summary.LastRecipients), columns.ToWidth);
        var subject = Truncate(summary.Subject, columns.SubjectWidth);
        var preview = Truncate(summary.BodyPreview, columns.PreviewWidth);
        var age = MailAges.Format(summary.LastMessageAt, now);

        var fromMarkup = StylizeFrom(from, IsSelf(summary.LastSender, actor));
        var toMarkup = Stylize(ThemeTokens.GetStyle("mail.row.to").ToMarkup(), Markup.Escape(to));
        var subjectMarkup = StylizeSubject(Pad(subject, columns.SubjectWidth), unreadToMe);
        var previewMarkup = Stylize(
            ThemeTokens.GetStyle("mail.row.preview").ToMarkup(), Markup.Escape(Pad(preview, columns.PreviewWidth)));
        var ageMarkup = Stylize(ThemeTokens.GetStyle("mail.row.age").ToMarkup(), Markup.Escape(Pad(age, columns.AgeWidth)));

        var line = $"{prefix}{fromMarkup} {toMarkup} {subjectMarkup} {previewMarkup} {ageMarkup}";

        if (columns.ShowCount)
        {
            var countMarkup = Stylize(
                ThemeTokens.GetStyle("mail.row.thread.count").ToMarkup(),
                Markup.Escape(PadLeft($"({summary.MessageCount})", columns.CountWidth)));
            line += $" {countMarkup}";
        }

        return selected ? Stylize(ThemeTokens.GetStyle("selection.highlight").ToMarkup(), line) : line;
    }

    /// <summary>
    /// Renders a message using the supplied selection and actor-specific unread state.
    /// An expanded thread child uses the thread-membership glyph; the count column
    /// is blank for all message rows.
    /// </summary>
    public static string RenderMessageRow(
        MailMessage message,
        bool threadChild,
        bool unreadToMe,
        bool selected,
        string actor,
        DateTimeOffset now,
        Columns columns)
    {
        var glyph = threadChild ? ThreadChildGlyph : MailRecipientView.GetRelationshipGlyph(message, actor).ToString();
        var glyphToken = threadChild ? "mail.row.thread.membership" : GlyphToken(MailRecipientView.GetRelationshipGlyph(message, actor));

        var prefix = BuildPrefix(
            selected,
            foldGlyph: NoFoldGlyph,
            unreadToMe,
            glyphText: glyph,
            glyphToken: glyphToken);

        var to = FormatOverflowList(RecipientNames(message));
        var preview = CreatePreview(message.Body);

        var from = Pad(message.Sender, columns.FromWidth);
        var toPadded = Pad(to, columns.ToWidth);
        var subject = Truncate(message.Subject, columns.SubjectWidth);
        var age = MailAges.Format(message.CreatedAt, now);

        var fromMarkup = StylizeFrom(from, IsSelf(message.Sender, actor));
        var toMarkup = Stylize(ThemeTokens.GetStyle("mail.row.to").ToMarkup(), Markup.Escape(toPadded));
        var subjectMarkup = StylizeSubject(Pad(subject, columns.SubjectWidth), unreadToMe);
        var previewMarkup = Stylize(
            ThemeTokens.GetStyle("mail.row.preview").ToMarkup(),
            Markup.Escape(Pad(Truncate(preview, columns.PreviewWidth), columns.PreviewWidth)));
        var ageMarkup = Stylize(ThemeTokens.GetStyle("mail.row.age").ToMarkup(), Markup.Escape(Pad(age, columns.AgeWidth)));

        var line = $"{prefix}{fromMarkup} {toMarkup} {subjectMarkup} {previewMarkup} {ageMarkup}";

        if (columns.ShowCount)
        {
            line += $" {new string(' ', columns.CountWidth)}";
        }

        return selected ? Stylize(ThemeTokens.GetStyle("selection.highlight").ToMarkup(), line) : line;
    }

    private static string BuildPrefix(
        bool selected,
        string foldGlyph,
        bool unreadToMe,
        char? glyph = null,
        string? glyphText = null,
        string? glyphToken = null)
    {
        var selectionText = selected ? SelectedPrefix : UnselectedPrefix;
        var foldMarkup = Stylize(ThemeTokens.GetStyle("mail.row.thread.fold").ToMarkup(), Markup.Escape(Pad(foldGlyph, FoldWidth - 1)));
        var markerText = unreadToMe ? UnreadToMeMarker : ReadMarker;
        var markerMarkup = unreadToMe
            ? Stylize(ThemeTokens.GetStyle("mail.row.unread-to-me").ToMarkup(), Markup.Escape(Pad(markerText, MarkerWidth - 1)))
            : Markup.Escape(Pad(markerText, MarkerWidth - 1));

        var resolvedGlyphText = glyphText ?? glyph?.ToString() ?? " ";
        var resolvedGlyphToken = glyphToken ?? (glyph is { } g ? GlyphToken(g) : string.Empty);
        var glyphMarkup = resolvedGlyphToken.Length == 0
            ? Markup.Escape(Pad(resolvedGlyphText, GlyphWidth - 1))
            : Stylize(ThemeTokens.GetStyle(resolvedGlyphToken).ToMarkup(), Markup.Escape(Pad(resolvedGlyphText, GlyphWidth - 1)));

        return $"{Markup.Escape(selectionText)}{foldMarkup} {markerMarkup} {glyphMarkup} ";
    }

    private static char ThreadRelationshipGlyph(MailThreadSummary summary, string actor)
    {
        if (IsSelf(summary.LastSender, actor))
        {
            return MailRecipientView.FromActorGlyph;
        }

        var isRecipient = summary.LastRecipients.Any(r => IsSelf(r, actor));

        if (!isRecipient)
        {
            return MailRecipientView.BlankGlyph;
        }

        return summary.LastRecipients.Count == 1 ? MailRecipientView.DirectGlyph : MailRecipientView.BroadcastGlyph;
    }

    private static bool IsSelf(string name, string actor) => string.Equals(name, actor, StringComparison.OrdinalIgnoreCase);

    private static string GlyphToken(char glyph) => glyph switch
    {
        MailRecipientView.FromActorGlyph => "mail.row.glyph.from-me",
        MailRecipientView.DirectGlyph => "mail.row.glyph.direct",
        MailRecipientView.BroadcastGlyph => "mail.row.glyph.broadcast",
        _ => string.Empty
    };

    private static string StylizeFrom(string paddedText, bool isSelf)
    {
        var token = isSelf ? "mail.row.from.me" : "mail.row.from";
        return Stylize(ThemeTokens.GetStyle(token).ToMarkup(), Markup.Escape(paddedText));
    }

    private static string StylizeSubject(string paddedText, bool unreadToMe)
    {
        var token = unreadToMe ? "mail.row.unread-to-me" : string.Empty;
        return token.Length == 0
            ? Markup.Escape(paddedText)
            : Stylize(ThemeTokens.GetStyle(token).ToMarkup(), Markup.Escape(paddedText));
    }

    private static IReadOnlyList<string> RecipientNames(MailMessage message)
        => message.Recipients.OrderBy(r => r.Ordinal).Select(r => r.Name).ToArray();

    /// <summary>
    /// Returns the first name with a count of additional names, or an empty string
    /// for an empty list.
    /// </summary>
    private static string FormatOverflowList(IReadOnlyList<string> names)
    {
        if (names.Count == 0)
        {
            return string.Empty;
        }

        var overflow = names.Count - 1;
        return overflow > 0 ? $"{names[0]}+{overflow}" : names[0];
    }

    /// <summary>
    /// Returns the body with each run of whitespace replaced by a single space and
    /// leading and trailing whitespace removed.
    /// </summary>
    private static string CreatePreview(string body)
        => string.Join(' ', body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Stylize(string styleMarkup, string content) =>
        styleMarkup.Length == 0 ? content : $"[{styleMarkup}]{content}[/]";

    /// <summary>
    /// The lowest code point of every contiguous run of terminal-wide code
    /// points this table measures at 2 cells wide. Paired with
    /// <see cref="s_wideRangeEnds"/> at the same index.
    /// </summary>
    private static readonly int[] s_wideRangeStarts =
    [
        0x1100, 0x2E80, 0x3041, 0x3400, 0x4E00, 0xA000, 0xAC00, 0xF900, 0xFE30,
        0xFF00, 0xFFE0, 0x1F1E6, 0x1F200, 0x1F300, 0x1F600, 0x1F680, 0x1F900,
        0x1FA70, 0x20000, 0x30000
    ];

    private static readonly int[] s_wideRangeEnds =
    [
        0x115F, 0x303E, 0x33FF, 0x4DBF, 0x9FFF, 0xA4CF, 0xD7A3, 0xFAFF, 0xFE4F,
        0xFF60, 0xFFE6, 0x1F1FF, 0x1F2FF, 0x1F5FF, 0x1F64F, 0x1F6FF, 0x1F9FF,
        0x1FAFF, 0x2FFFD, 0x3FFFD
    ];

    /// <summary>
    /// A single Unicode scalar's terminal cell width: 2 for a code point in
    /// <see cref="s_wideRangeStarts"/>/<see cref="s_wideRangeEnds"/>, 1 for everything else.
    /// </summary>
    private static int GetRuneWidth(Rune rune)
    {
        var value = rune.Value;

        if (value < 0x1100)
        {
            return 1;
        }

        for (var i = 0; i < s_wideRangeStarts.Length; i++)
        {
            if (value < s_wideRangeStarts[i])
            {
                return 1;
            }

            if (value <= s_wideRangeEnds[i])
            {
                return 2;
            }
        }

        return 1;
    }

    /// <summary>
    /// Returns the sum of the terminal widths assigned to the string's Unicode scalars.
    /// </summary>
    private static int MeasureWidth(string value)
    {
        var width = 0;

        foreach (var rune in value.EnumerateRunes())
        {
            width += GetRuneWidth(rune);
        }

        return width;
    }

    private static string Pad(string value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var truncated = Truncate(value, width);
        var truncatedWidth = MeasureWidth(truncated);
        return truncatedWidth >= width ? truncated : truncated + new string(' ', width - truncatedWidth);
    }

    private static string PadLeft(string value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var truncated = Truncate(value, width);
        var truncatedWidth = MeasureWidth(truncated);
        return truncatedWidth >= width ? truncated : new string(' ', width - truncatedWidth) + truncated;
    }

    /// <summary>
    /// Truncates to the requested terminal-cell width without splitting Unicode
    /// scalars, adding an ellipsis when text is omitted. A non-positive width returns
    /// an empty string.
    /// </summary>
    private static string Truncate(string value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        if (MeasureWidth(value) <= width)
        {
            return value;
        }

        if (width == 1)
        {
            return Ellipsis;
        }

        var budget = width - 1;
        var used = 0;
        var builder = new StringBuilder();

        foreach (var rune in value.EnumerateRunes())
        {
            var runeWidth = GetRuneWidth(rune);

            if (used + runeWidth > budget)
            {
                break;
            }

            builder.Append(rune);
            used += runeWidth;
        }

        return builder.Append(EllipsisChar).ToString();
    }
}
