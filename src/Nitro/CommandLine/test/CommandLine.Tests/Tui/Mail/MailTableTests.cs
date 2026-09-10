using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using Spectre.Console;
using Spectre.Console.Testing;
using static ChilliCream.Nitro.CommandLine.Tests.Tui.AnsiAssertions;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailTableTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static MailThreadSummary Thread(
        string threadId = "t-1",
        string subject = "Status update",
        string lastSender = "bob",
        IReadOnlyList<string>? lastRecipients = null,
        string bodyPreview = "Body preview text",
        int messageCount = 1,
        int? unreadCount = null,
        DateTimeOffset? lastMessageAt = null)
        => new()
        {
            ThreadId = threadId,
            Subject = subject,
            MessageCount = messageCount,
            LastMessageAt = lastMessageAt ?? Now,
            LastSender = lastSender,
            LastRecipients = lastRecipients ?? ["alice"],
            BodyPreview = bodyPreview,
            UnreadCount = unreadCount,
            ArchivedCount = null
        };

    private static void AssertRendersWithoutError(string markup)
    {
        var console = new TestConsole().Width(200);
        var exception = Record.Exception(() => console.Write(new Markup(markup)));
        Assert.Null(exception);
    }

    /// <summary>
    /// Renders <paramref name="markup"/> through a plain (no-ANSI) <see cref="TestConsole"/>
    /// so style tags disappear and only the literal characters remain, for
    /// tests that need to measure column positions rather than styling.
    /// </summary>
    private static string RenderPlain(string markup)
    {
        var console = new TestConsole().Width(300);
        console.Write(new Markup(markup));
        return console.Output.TrimEnd('\r', '\n');
    }

    /// <summary>
    /// Renders <paramref name="markup"/> through a <see cref="TestConsole"/>
    /// built with <c>.Colors(ColorSystem.TrueColor)</c> and
    /// <c>.EmitAnsiSequences()</c>, the console shape
    /// <see cref="AnsiAssertions.AssertAnsiStyleApplied"/> requires - a plain
    /// <see cref="TestConsole"/> strips markup entirely, so it would leave
    /// every ANSI-tier assertion green even for a wrong or missing token.
    /// </summary>
    private static string RenderAnsi(string markup)
    {
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(300);
        console.Write(new Markup(markup));
        return console.Output;
    }

    /// <summary>
    /// An independent, test-only terminal-cell-width measurement (2 cells
    /// for CJK ideographs and the emoji block used by these tests' fixtures,
    /// 1 otherwise) for asserting against <see cref="MailTable"/>'s own
    /// production width measurement without calling its private members
    /// directly.
    /// </summary>
    private static int MeasureCellWidth(string value)
        => value.EnumerateRunes().Sum(rune => rune.Value switch
        {
            >= 0x4E00 and <= 0x9FFF => 2, // CJK unified ideographs
            >= 0x1F300 and <= 0x1FAFF => 2, // emoji blocks
            _ => 1
        });

    /// <summary>
    /// True when <paramref name="value"/> contains a UTF-16 surrogate half
    /// with no matching partner - what a char-index (rather than Rune-index)
    /// truncation could produce by cutting an astral-plane character (for
    /// example most emoji) in half.
    /// </summary>
    private static bool ContainsLoneSurrogate(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsHighSurrogate(value[i]))
            {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                {
                    return true;
                }

                i++;
            }
            else if (char.IsLowSurrogate(value[i]))
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void ComputeColumns_Should_SplitElasticRemainder_BetweenSubjectAndPreview()
    {
        // act
        var columns = MailTable.ComputeColumns(100, showCount: false);

        // assert: prefix(8) + from(14+1) + to(14+1) + age(10+1) + trailing
        // gap(1) = 49 fixed columns, leaving 50 for subject (2/5) + preview.
        Assert.Equal(20, columns.SubjectWidth);
        Assert.Equal(30, columns.PreviewWidth);
        Assert.False(columns.ShowCount);
    }

    [Fact]
    public void ComputeColumns_Should_ReserveTheCountColumn_When_ShowCountIsTrue()
    {
        // act
        var columns = MailTable.ComputeColumns(100, showCount: true);

        // assert: the same 100 columns now also spend 6 on the count column.
        Assert.True(columns.ShowCount);
        Assert.Equal(17, columns.SubjectWidth);
        Assert.Equal(27, columns.PreviewWidth);
    }

    [Fact]
    public void ComputeColumns_Should_DegradeToZero_Not_Negative_When_WidthIsNarrow()
    {
        // act
        var columns = MailTable.ComputeColumns(10, showCount: true);

        // assert
        Assert.True(columns.SubjectWidth >= 0);
        Assert.True(columns.PreviewWidth >= 0);
    }

    [Fact]
    public void RenderHeading_Should_ContainEveryColumnLabel()
    {
        // arrange
        var columns = MailTable.ComputeColumns(120, showCount: true);

        // act
        var heading = MailTable.RenderHeading(columns);

        // assert
        Assert.Contains("From", heading);
        Assert.Contains("To", heading);
        Assert.Contains("Subject", heading);
        Assert.Contains("Preview", heading);
        Assert.Contains("Age", heading);
        Assert.Contains("#", heading);
        AssertRendersWithoutError(heading);
    }

    [Fact]
    public void RenderHeading_Should_OmitCountLabel_When_ShowCountIsFalse()
    {
        // arrange
        var columns = MailTable.ComputeColumns(120, showCount: false);

        // act
        var heading = MailTable.RenderHeading(columns);

        // assert
        Assert.DoesNotContain("#", heading);
    }

    [Fact]
    public void RenderThreadRow_Should_ContainFromToSubjectPreviewAndAge()
    {
        // arrange
        var columns = MailTable.ComputeColumns(120, showCount: true);
        var thread = Thread(subject: "Status update", lastSender: "bob", lastRecipients: ["alice"], bodyPreview: "hi there");

        // act
        var line = MailTable.RenderThreadRow(thread, expanded: false, unreadToMe: false, selected: false, "alice", Now, columns);

        // assert
        Assert.Contains("bob", line);
        Assert.Contains("alice", line);
        Assert.Contains("Status update", line);
        Assert.Contains("hi there", line);
        Assert.Contains("now", line);
        AssertRendersWithoutError(line);
    }

    [Fact]
    public void RenderThreadRow_Should_ShowMessageCount_When_ShowCountIsTrue()
    {
        // arrange
        var columns = MailTable.ComputeColumns(120, showCount: true);
        var thread = Thread(messageCount: 7);

        // act
        var line = MailTable.RenderThreadRow(thread, expanded: false, unreadToMe: false, selected: false, "alice", Now, columns);

        // assert
        Assert.Contains("(7)", line);
    }

    [Fact]
    public void RenderThreadRow_Should_UseExpandedFoldGlyph_When_Expanded()
    {
        // arrange
        var columns = MailTable.ComputeColumns(120, showCount: true);
        var thread = Thread();

        // act
        var collapsed = MailTable.RenderThreadRow(thread, expanded: false, unreadToMe: false, selected: false, "alice", Now, columns);
        var expanded = MailTable.RenderThreadRow(thread, expanded: true, unreadToMe: false, selected: false, "alice", Now, columns);

        // assert
        Assert.Contains("▸", collapsed);
        Assert.Contains("▾", expanded);
    }

    [Fact]
    public void RenderThreadRow_Should_ApplyUnreadToMeToken_When_UnreadToMeIsTrue()
    {
        // arrange
        var columns = MailTable.ComputeColumns(120, showCount: true);
        var thread = Thread();

        // act
        var unread = MailTable.RenderThreadRow(thread, expanded: false, unreadToMe: true, selected: false, "alice", Now, columns);
        var read = MailTable.RenderThreadRow(thread, expanded: false, unreadToMe: false, selected: false, "alice", Now, columns);

        // assert: the unread-to-me row carries the marker glyph and a style
        // span the read row does not.
        Assert.Contains("●", unread);
        Assert.DoesNotContain("●", read);
        Assert.NotEqual(unread, read);
    }

    [Fact]
    public void RenderThreadRow_Should_Highlight_When_Selected()
    {
        // arrange
        var columns = MailTable.ComputeColumns(120, showCount: true);
        var thread = Thread();

        // act
        var selected = MailTable.RenderThreadRow(thread, expanded: false, unreadToMe: false, selected: true, "alice", Now, columns);
        var unselected = MailTable.RenderThreadRow(thread, expanded: false, unreadToMe: false, selected: false, "alice", Now, columns);

        // assert
        Assert.Contains(">", selected);
        Assert.NotEqual(selected, unselected);
        AssertRendersWithoutError(selected);
    }

    [Fact]
    public void RenderThreadRow_Should_EscapeMarkupCharacters_InSubjectAndPreview()
    {
        // arrange
        var columns = MailTable.ComputeColumns(120, showCount: true);
        var thread = Thread(subject: "[URGENT] fix this", bodyPreview: "see [here]");

        // act
        var line = MailTable.RenderThreadRow(thread, expanded: false, unreadToMe: false, selected: false, "alice", Now, columns);

        // assert
        AssertRendersWithoutError(line);
    }

    [Fact]
    public void RenderThreadRow_Should_ApplyAnsiStyling_ToGlyphPeerAndAgeTokens_When_ActorIsDirectRecipient()
    {
        // arrange: bob's last message addresses alice alone, so the thread
        // gets the direct relationship glyph.
        var columns = MailTable.ComputeColumns(120, showCount: true);
        var thread = Thread(lastSender: "bob", lastRecipients: ["alice"]);

        // act
        var line = MailTable.RenderThreadRow(thread, expanded: false, unreadToMe: false, selected: false, "alice", Now, columns);
        var output = RenderAnsi(line);

        // assert: mail.row.to and mail.row.age share their color with other
        // row tokens (mail.row.thread.fold and mail.row.preview/thread.count
        // respectively), so a generic "does this style appear anywhere"
        // check would pass even if the wrong cell carried it; pinning the
        // style to the actual rendered text catches that. A plain
        // TestConsole strips markup entirely, so a wrong or missing token
        // name here would still leave every plain-text Contains assertion
        // above green.
        AssertAnsiStyleApplied(output, "mail.row.glyph.direct");
        AssertAnsiStylePrefixesText(output, "mail.row.to", "alice");
        AssertAnsiStylePrefixesText(output, "mail.row.age", "now");
        AssertAnsiStyleApplied(output, "mail.row.from");
    }

    [Fact]
    public void RenderThreadRow_Should_ApplyFromMeTokens_When_ActorSentTheThread()
    {
        // arrange
        var columns = MailTable.ComputeColumns(120, showCount: true);
        var thread = Thread(lastSender: "alice", lastRecipients: ["bob"]);

        // act
        var line = MailTable.RenderThreadRow(thread, expanded: false, unreadToMe: false, selected: false, "alice", Now, columns);
        var output = RenderAnsi(line);

        // assert: mail.row.glyph.from-me and mail.row.from.me render the
        // same color, so pinning each to its own text (the 'F' glyph, the
        // "alice" From cell) proves both cells carry a token, not just one.
        AssertAnsiStylePrefixesText(output, "mail.row.glyph.from-me", "F");
        AssertAnsiStylePrefixesText(output, "mail.row.from.me", "alice");
    }

    [Fact]
    public void RenderThreadRow_Should_ApplyAnsiStyling_ToUnreadToMeMarker_When_UnreadToMeIsTrue()
    {
        // arrange
        var columns = MailTable.ComputeColumns(120, showCount: true);
        var thread = Thread();

        // act
        var line = MailTable.RenderThreadRow(thread, expanded: false, unreadToMe: true, selected: false, "alice", Now, columns);
        var output = RenderAnsi(line);

        // assert
        AssertAnsiStyleApplied(output, "mail.row.unread-to-me");
    }

    [Fact]
    public void RenderMessageRow_Should_ContainFromToSubjectAndAge()
    {
        // arrange
        var columns = MailTable.ComputeColumns(120, showCount: false);
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            subject: "Status update",
            body: "hi there, this is the body",
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient("alice")]);

        // act
        var line = MailTable.RenderMessageRow(message, threadChild: false, unreadToMe: false, selected: false, "alice", Now, columns);

        // assert
        Assert.Contains("bob", line);
        Assert.Contains("alice", line);
        Assert.Contains("Status update", line);
        Assert.Contains("hi there", line);
        AssertRendersWithoutError(line);
    }

    [Fact]
    public void RenderMessageRow_Should_UseThreadMembershipGlyph_NotRelationshipGlyph_When_ThreadChild()
    {
        // arrange: the actor sent the message (would otherwise get the
        // from-me 'F' glyph), but as an expanded thread child it should show
        // the distinct thread-membership vocabulary instead (TUI research
        // conv. 7).
        var columns = MailTable.ComputeColumns(120, showCount: true);
        var message = MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]);

        // act
        var childRow = MailTable.RenderMessageRow(message, threadChild: true, unreadToMe: false, selected: false, "alice", Now, columns);
        var flatRow = MailTable.RenderMessageRow(message, threadChild: false, unreadToMe: false, selected: false, "alice", Now, columns);

        // assert
        Assert.Contains("└", childRow);
        Assert.DoesNotContain("└", flatRow);
        Assert.Contains("F", flatRow);
    }

    [Fact]
    public void RenderMessageRow_Should_ApplyUnreadToMeToken_When_UnreadToMeIsTrue()
    {
        // arrange
        var columns = MailTable.ComputeColumns(120, showCount: false);
        var message = MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]);

        // act
        var unread = MailTable.RenderMessageRow(message, threadChild: false, unreadToMe: true, selected: false, "alice", Now, columns);
        var read = MailTable.RenderMessageRow(message, threadChild: false, unreadToMe: false, selected: false, "alice", Now, columns);

        // assert
        Assert.Contains("●", unread);
        Assert.DoesNotContain("●", read);
    }

    [Fact]
    public void RenderMessageRow_Should_ApplyAnsiStyling_ToGlyphPeerPreviewAndAgeTokens_When_ActorIsDirectRecipient()
    {
        // arrange: bob addresses alice alone, so the message gets the direct
        // relationship glyph.
        var columns = MailTable.ComputeColumns(120, showCount: false);
        var message = MailMessageBuilder.Create(
            "m-1", sender: "bob", body: "hi", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]);

        // act
        var line = MailTable.RenderMessageRow(message, threadChild: false, unreadToMe: false, selected: false, "alice", Now, columns);
        var output = RenderAnsi(line);

        // assert: mail.row.to, mail.row.preview, and mail.row.age share
        // colors with other row tokens (mail.row.thread.fold and each
        // other), so a generic "does this style appear anywhere" check
        // would pass even if the wrong cell carried it; pinning the style
        // to the actual rendered text catches that. A plain TestConsole
        // strips markup entirely, so a wrong or missing token name here
        // would still leave every plain-text Contains assertion above
        // green.
        AssertAnsiStyleApplied(output, "mail.row.glyph.direct");
        AssertAnsiStylePrefixesText(output, "mail.row.to", "alice");
        AssertAnsiStylePrefixesText(output, "mail.row.preview", "hi");
        AssertAnsiStylePrefixesText(output, "mail.row.age", "now");
    }

    [Fact]
    public void RenderMessageRow_Should_ApplyAnsiStyling_ToUnreadToMeMarker_When_UnreadToMeIsTrue()
    {
        // arrange
        var columns = MailTable.ComputeColumns(120, showCount: false);
        var message = MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]);

        // act
        var line = MailTable.RenderMessageRow(message, threadChild: false, unreadToMe: true, selected: false, "alice", Now, columns);
        var output = RenderAnsi(line);

        // assert
        AssertAnsiStyleApplied(output, "mail.row.unread-to-me");
    }

    [Fact]
    public void RenderMessageRow_Should_LeaveTheCountColumnBlank_When_FlatRowInThreadedColumns()
    {
        // arrange: a flat-mode row rendered with Threads-mode columns (for
        // example a Sent-mailbox flat fallback while the count column is
        // still reserved) must not print a stray count.
        var columns = MailTable.ComputeColumns(120, showCount: true);
        var message = MailMessageBuilder.Create("m-1", createdAt: Now);

        // act
        var line = MailTable.RenderMessageRow(message, threadChild: false, unreadToMe: false, selected: false, "actor", Now, columns);

        // assert
        Assert.DoesNotContain("(", line);
    }

    [Fact]
    public void RenderThreadRow_And_RenderMessageRow_Should_BeDeterministic()
    {
        // arrange
        var columns = MailTable.ComputeColumns(120, showCount: true);
        var thread = Thread();

        // act
        var first = MailTable.RenderThreadRow(thread, expanded: false, unreadToMe: false, selected: false, "alice", Now, columns);
        var second = MailTable.RenderThreadRow(thread, expanded: false, unreadToMe: false, selected: false, "alice", Now, columns);

        // assert
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RenderHeading_And_RenderThreadRow_Should_AlignColumnStarts_AtTheSameIndex(bool showCount)
    {
        // arrange: a width wide enough that Subject/Preview stay above zero
        // (elastic > 0), so every column has a well-defined start index.
        const int contentWidth = 100;
        var columns = MailTable.ComputeColumns(contentWidth, showCount);
        var thread = Thread(
            subject: new string('S', 40),
            lastSender: new string('F', columns.FromWidth),
            lastRecipients: [new string('T', columns.ToWidth)],
            bodyPreview: new string('P', 40));

        var offsetFrom = columns.PrefixWidth;
        var offsetTo = offsetFrom + columns.FromWidth + 1;
        var offsetSubject = offsetTo + columns.ToWidth + 1;
        var offsetPreview = offsetSubject + columns.SubjectWidth + 1;
        var offsetAge = offsetPreview + columns.PreviewWidth + 1;

        // act
        var heading = RenderPlain(MailTable.RenderHeading(columns));
        var row = RenderPlain(MailTable.RenderThreadRow(
            thread, expanded: false, unreadToMe: false, selected: false, "alice", Now, columns));

        // assert: the RenderHeading join fix restores exactly PrefixWidth
        // columns before "From", so every later label starts at the same
        // index as the row's corresponding data cell.
        Assert.Equal(offsetFrom, heading.IndexOf("From", StringComparison.Ordinal));
        Assert.Equal(offsetFrom, row.IndexOf(new string('F', columns.FromWidth), StringComparison.Ordinal));
        Assert.Equal(offsetTo, heading.IndexOf("To", StringComparison.Ordinal));
        Assert.Equal(offsetTo, row.IndexOf(new string('T', columns.ToWidth), StringComparison.Ordinal));
        Assert.Equal(offsetSubject, heading.IndexOf("Subject", StringComparison.Ordinal));
        Assert.Equal(offsetSubject, row.IndexOf(new string('S', 4), StringComparison.Ordinal));
        Assert.Equal(offsetPreview, heading.IndexOf("Preview", StringComparison.Ordinal));
        Assert.Equal(offsetPreview, row.IndexOf(new string('P', 4), StringComparison.Ordinal));
        Assert.Equal(offsetAge, heading.IndexOf("Age", StringComparison.Ordinal));

        // assert: the row line fills exactly contentWidth (it is never
        // trimmed); the heading is trimmed only after its own last column,
        // so it can be no longer than contentWidth.
        Assert.Equal(contentWidth, row.Length);
        Assert.True(heading.Length <= contentWidth);
    }

    [Theory]
    [InlineData(50, false)] // elastic 0
    [InlineData(51, false)] // elastic 1
    [InlineData(52, false)] // elastic 2
    [InlineData(56, true)] // elastic 0
    [InlineData(57, true)] // elastic 1
    [InlineData(58, true)] // elastic 2
    public void RenderHeading_And_RenderThreadRow_Should_NeverExceedContentWidth_When_ElasticIsNearZero(
        int contentWidth, bool showCount)
    {
        // arrange
        var columns = MailTable.ComputeColumns(contentWidth, showCount);
        var thread = Thread(subject: new string('S', 40), bodyPreview: new string('P', 40));

        // act
        var heading = RenderPlain(MailTable.RenderHeading(columns));
        var row = RenderPlain(MailTable.RenderThreadRow(
            thread, expanded: false, unreadToMe: false, selected: false, "alice", Now, columns));

        // assert: the clamped subject floor keeps Subject/Preview from
        // pushing Age (and the count column, when shown) past contentWidth.
        Assert.True(heading.Length <= contentWidth, $"heading length {heading.Length} exceeded {contentWidth}.");
        Assert.True(row.Length <= contentWidth, $"row length {row.Length} exceeded {contentWidth}.");
    }

    [Fact]
    public void RenderThreadRow_Should_FitContentWidth_InTerminalCells_When_EveryColumnHoldsWideCjkCharacters()
    {
        // arrange: every text column stuffed with CJK ideographs (2 terminal
        // cells each), so a Pad/Truncate that measured UTF-16 string.Length
        // instead of terminal cell width - the pre-fix bug - would overflow
        // this budget in a real terminal even though every field's own
        // .Length still looked correct.
        const int contentWidth = 80;
        var columns = MailTable.ComputeColumns(contentWidth, showCount: true);
        var thread = Thread(
            subject: new string('件', 60),
            lastSender: new string('名', 60),
            lastRecipients: [new string('宛', 60)],
            bodyPreview: new string('文', 60));

        // act
        var row = RenderPlain(MailTable.RenderThreadRow(
            thread, expanded: false, unreadToMe: false, selected: false, "alice", Now, columns));

        // assert: the row still fills exactly contentWidth terminal cells,
        // the same guarantee the all-ASCII alignment test above makes,
        // where cell width and UTF-16 length coincide.
        Assert.Equal(contentWidth, MeasureCellWidth(row));
    }

    [Fact]
    public void RenderMessageRow_Should_FitContentWidth_InTerminalCells_When_EveryColumnHoldsEmojiCharacters()
    {
        // arrange: every text column stuffed with astral-plane emoji (a
        // UTF-16 surrogate pair each, 2 terminal cells), the width class the
        // pre-fix .Length-based Pad/Truncate both undercounted (as one
        // "wide" character it should count once) and, being surrogate
        // pairs, measured as 2 chars for what should be one 2-cell rune -
        // two compensating errors this fix's Rune-based measurement
        // resolves independently.
        const int contentWidth = 80;
        var columns = MailTable.ComputeColumns(contentWidth, showCount: false);
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: string.Concat(Enumerable.Repeat("🎈", 60)),
            subject: string.Concat(Enumerable.Repeat("🎉", 60)),
            body: string.Concat(Enumerable.Repeat("🎊", 60)),
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient(string.Concat(Enumerable.Repeat("🎁", 60)))]);

        // act
        var row = RenderPlain(MailTable.RenderMessageRow(
            message, threadChild: false, unreadToMe: false, selected: false, "alice", Now, columns));

        // assert
        Assert.Equal(contentWidth, MeasureCellWidth(row));
    }

    [Theory]
    [InlineData(20)]
    [InlineData(21)]
    [InlineData(22)]
    [InlineData(35)]
    [InlineData(36)]
    [InlineData(50)]
    [InlineData(51)]
    public void RenderMessageRow_Should_NeverEmitALoneSurrogate_When_TruncatingEmojiContent(int contentWidth)
    {
        // arrange: every fixed and elastic column budget gets exercised at a
        // different cut point as contentWidth varies, hunting for a
        // truncation boundary that would land mid-surrogate-pair under a
        // char-index (rather than Rune-boundary) Truncate.
        var columns = MailTable.ComputeColumns(contentWidth, showCount: false);
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: string.Concat(Enumerable.Repeat("🎈", 40)),
            subject: string.Concat(Enumerable.Repeat("🎉", 40)),
            body: string.Concat(Enumerable.Repeat("🎊", 40)),
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient(string.Concat(Enumerable.Repeat("🎁", 40)))]);

        // act
        var row = MailTable.RenderMessageRow(
            message, threadChild: false, unreadToMe: false, selected: false, "alice", Now, columns);

        // assert: no lone surrogate in either the raw markup or the plain
        // rendered text - the ellipsis this fix truncates to never ends up
        // appended after only half of a split emoji.
        Assert.False(ContainsLoneSurrogate(row));
        Assert.False(ContainsLoneSurrogate(RenderPlain(row)));
    }
}
