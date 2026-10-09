using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailTableTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static MailThreadSummary Thread(
        string threadId = "t-1",
        string subject = "Status update",
        string lastSender = "bob",
        IReadOnlyList<string>? lastRecipients = null,
        int messageCount = 1,
        DateTimeOffset? lastMessageAt = null)
        => new()
        {
            ThreadId = threadId,
            Subject = subject,
            MessageCount = messageCount,
            LastMessageAt = lastMessageAt ?? s_now,
            LastSender = lastSender,
            LastRecipients = lastRecipients ?? ["alice"],
            BodyPreview = "Body preview text",
            UnreadCount = null,
            ArchivedCount = null
        };

    [Fact]
    public void Render_Should_ReturnEmpty_When_MaxWidthIsZero()
    {
        // arrange
        var thread = Thread();
        var widths = MailTable.ComputeWidths([thread], s_now);

        // act
        var line = MailTable.Render(thread, s_now, selected: false, maxWidth: 0, widths);

        // assert
        Assert.Empty(line);
    }

    [Fact]
    public void Render_Should_ShowEveryColumn_When_MaxWidthFits()
    {
        // arrange
        const int maxWidth = 90;
        var thread = Thread();
        var widths = MailTable.ComputeWidths([thread], s_now);

        // act
        var line = Markup.Remove(MailTable.Render(thread, s_now, selected: false, maxWidth, widths));

        // assert
        var expected = "   "
            + "Status update".PadRight(widths.Subject)
            + "    " + "bob".PadRight(widths.From)
            + "    " + "alice".PadRight(widths.To)
            + "    " + "1".PadLeft(widths.Messages)
            + "    " + "just now".PadRight(widths.LastActivity);
        Assert.Equal(expected, line);
    }

    [Fact]
    public void Render_Should_ShowFirstRecipientPlusOverflowCount_When_ThreadHasMultipleRecipients()
    {
        // arrange
        const int maxWidth = 90;
        var thread = Thread(lastRecipients: ["alice", "carol", "dave"]);
        var widths = MailTable.ComputeWidths([thread], s_now);

        // act
        var line = Markup.Remove(MailTable.Render(thread, s_now, selected: false, maxWidth, widths));

        // assert
        Assert.Contains("alice+2", line);
    }

    [Fact]
    public void Render_Should_RightAlignMessageCount_When_ColumnIsWiderThanTheDigits()
    {
        // arrange
        const int maxWidth = 90;
        var thread = Thread(messageCount: 1);
        var widths = MailTable.ComputeWidths([thread], s_now);

        // act
        var header = Markup.Remove(MailTable.RenderHeader(maxWidth, widths));
        var line = Markup.Remove(MailTable.Render(thread, s_now, selected: false, maxWidth, widths));
        var messagesOffset = header.IndexOf("MESSAGES", StringComparison.Ordinal);

        // assert
        Assert.Equal("1".PadLeft(widths.Messages), line.Substring(messagesOffset, widths.Messages));
    }

    [Theory]
    [InlineData(0, "just now")]
    [InlineData(90, "1m ago")]
    public void Render_Should_FormatLastActivity_When_AgeIsFreshOrMinutesOld(int elapsedSeconds, string expectedAge)
    {
        // arrange
        var thread = Thread(lastMessageAt: s_now.AddSeconds(-elapsedSeconds));
        var widths = MailTable.ComputeWidths([thread], s_now);

        // act
        var line = Markup.Remove(MailTable.Render(thread, s_now, selected: false, maxWidth: 80, widths));

        // assert
        Assert.EndsWith(expectedAge, line.TrimEnd());
    }

    [Fact]
    public void Render_Should_FormatLastActivityAsBareDate_When_ActivityIsAtLeastAWeekOld()
    {
        // arrange
        var lastMessageAt = s_now.AddDays(-8);
        var thread = Thread(lastMessageAt: lastMessageAt);
        var widths = MailTable.ComputeWidths([thread], s_now);

        // act
        var line = Markup.Remove(MailTable.Render(thread, s_now, selected: false, maxWidth: 80, widths));

        // assert
        Assert.EndsWith(lastMessageAt.ToUniversalTime().ToString("yyyy-MM-dd"), line.TrimEnd());
    }

    [Fact]
    public void Render_Should_DropToColumn_When_WidthIsTooNarrowForAllColumns()
    {
        // arrange
        var thread = Thread();
        var widths = MailTable.ComputeWidths([thread], s_now);
        var wide = Markup.Remove(MailTable.Render(thread, s_now, selected: false, maxWidth: 90, widths));

        // act
        var narrowMaxWidth = widths.Subject + 3 + widths.From + 4 + widths.Messages + 4 + widths.LastActivity + 4;
        var narrow = Markup.Remove(MailTable.Render(thread, s_now, selected: false, narrowMaxWidth, widths));
        var actual = (
            WideHasTo: wide.Contains("alice", StringComparison.Ordinal),
            NarrowHasTo: narrow.Contains("alice", StringComparison.Ordinal),
            NarrowHasFrom: narrow.Contains("bob", StringComparison.Ordinal));

        // assert
        Assert.Equal((true, false, true), actual);
    }

    [Fact]
    public void Render_Should_DropFromColumn_When_WidthIsTooNarrowForToAndFrom()
    {
        // arrange
        var thread = Thread();
        var widths = MailTable.ComputeWidths([thread], s_now);
        var narrowMaxWidth = widths.Subject + 3 + widths.Messages + 4 + widths.LastActivity + 4;

        // act
        var narrow = Markup.Remove(MailTable.Render(thread, s_now, selected: false, narrowMaxWidth, widths));
        var actual = (
            HasTo: narrow.Contains("alice", StringComparison.Ordinal),
            HasFrom: narrow.Contains("bob", StringComparison.Ordinal),
            HasActivity: narrow.Contains("just now", StringComparison.Ordinal));

        // assert
        Assert.Equal((false, false, true), actual);
    }

    [Fact]
    public void Render_Should_KeepSubjectAndLastActivity_When_WidthIsTooNarrowForEveryOtherColumn()
    {
        // arrange
        var thread = Thread();
        var widths = MailTable.ComputeWidths([thread], s_now);
        var narrowMaxWidth = widths.Subject + 3 + widths.LastActivity + 4;

        // act
        var narrow = Markup.Remove(MailTable.Render(thread, s_now, selected: false, narrowMaxWidth, widths));
        var actual = (
            HasSubject: narrow.Contains("Status update", StringComparison.Ordinal),
            HasActivity: narrow.Contains("just now", StringComparison.Ordinal),
            HasFrom: narrow.Contains("bob", StringComparison.Ordinal),
            HasTo: narrow.Contains("alice", StringComparison.Ordinal));

        // assert
        Assert.Equal((true, true, false, false), actual);
    }

    [Fact]
    public void Render_Should_TruncateSubject_When_WidthIsTooNarrowForEveryColumn()
    {
        // arrange
        const int maxWidth = 20;
        var thread = Thread();
        var widths = MailTable.ComputeWidths([thread], s_now);

        // act
        var line = Markup.Remove(MailTable.Render(thread, s_now, selected: false, maxWidth, widths));

        // assert
        Assert.True(line.GetCellWidth() <= maxWidth);
    }

    [Fact]
    public void RenderHeader_Should_AlignColumnsWithRow_When_WidthIsWide()
    {
        // arrange
        const int maxWidth = 90;
        var thread = Thread();
        var widths = MailTable.ComputeWidths([thread], s_now);

        // act
        var header = Markup.Remove(MailTable.RenderHeader(maxWidth, widths));
        var line = Markup.Remove(MailTable.Render(thread, s_now, selected: false, maxWidth, widths));
        var subjectOffset = header.IndexOf("SUBJECT", StringComparison.Ordinal);
        var fromOffset = header.IndexOf("FROM", StringComparison.Ordinal);
        var toOffset = header.IndexOf("TO", StringComparison.Ordinal);

        // assert
        Assert.Equal("Status update", line.Substring(subjectOffset, 13));
        Assert.Equal("bob", line.Substring(fromOffset, 3));
        Assert.Equal("alice", line.Substring(toOffset, 5));
    }

    [Fact]
    public void RenderHeader_Should_DropTheSameColumnsAsRow_When_WidthIsNarrow()
    {
        // arrange
        var thread = Thread();
        var widths = MailTable.ComputeWidths([thread], s_now);
        var narrowMaxWidth = widths.Subject + 3 + widths.From + 4 + widths.Messages + 4 + widths.LastActivity + 4;

        // act
        var header = Markup.Remove(MailTable.RenderHeader(narrowMaxWidth, widths));
        var line = Markup.Remove(MailTable.Render(thread, s_now, selected: false, narrowMaxWidth, widths));
        var actual = (
            HeaderHasTo: header.Contains("TO", StringComparison.Ordinal),
            RowHasTo: line.Contains("alice", StringComparison.Ordinal),
            HeaderHasFrom: header.Contains("FROM", StringComparison.Ordinal),
            RowHasFrom: line.Contains("bob", StringComparison.Ordinal));

        // assert
        Assert.Equal((false, false, true, true), actual);
    }

    [Fact]
    public void RenderRule_Should_ReturnEmpty_When_MaxWidthIsZero()
    {
        // act
        var rule = MailTable.RenderRule(0);

        // assert
        Assert.Empty(rule);
    }

    [Fact]
    public void RenderRule_Should_FillTheGivenWidth_When_WidthIsPositive()
    {
        // act
        var rule = Markup.Remove(MailTable.RenderRule(20));

        // assert
        Assert.Equal(new string('─', 20), rule);
    }
}
