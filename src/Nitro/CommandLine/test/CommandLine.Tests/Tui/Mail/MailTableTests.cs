using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using Spectre.Console;
using Spectre.Console.Testing;

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
            UnreadCount = unreadCount
        };

    private static void AssertRendersWithoutError(string markup)
    {
        var console = new TestConsole().Width(200);
        var exception = Record.Exception(() => console.Write(new Markup(markup)));
        Assert.Null(exception);
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
}
