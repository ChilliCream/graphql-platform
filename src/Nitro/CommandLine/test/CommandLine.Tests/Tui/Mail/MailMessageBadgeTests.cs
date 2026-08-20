using ChilliCream.Nitro.CommandLine.Tui.Mail;
using Spectre.Console;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailMessageBadgeTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Render_Should_BuildBadgeLine_When_UnselectedAndRead()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            subject: "Status update",
            createdAt: Now.AddMinutes(-5),
            recipients: [MailMessageBuilder.ToRecipient("alice", readAt: Now)]);

        // act
        var line = MailMessageBadge.Render(message, "alice", Now, selected: false, maxWidth: 80);

        // assert
        line.MatchInlineSnapshot("    bob Status update 5m");
    }

    [Fact]
    public void Render_Should_MarkUnread_When_ActorHasNotReadTheMessage()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            subject: "Status update",
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient("alice")]);

        // act
        var line = MailMessageBadge.Render(message, "alice", Now, selected: false, maxWidth: 80);

        // assert
        Assert.Contains("  * bob", line);
        Assert.Contains("[bold]Status update[/]", line);
    }

    [Fact]
    public void Render_Should_PrefixAndHighlight_When_Selected()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            subject: "Status update",
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient("alice", readAt: Now)]);

        // act
        var line = MailMessageBadge.Render(message, "alice", Now, selected: true, maxWidth: 80);

        // assert
        line.MatchInlineSnapshot("[default on grey35]>   bob Status update now[/]");
    }

    [Fact]
    public void Render_Should_TruncateSubjectWithEllipsis_When_LineExceedsMaxWidth()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            subject: "A very long subject line that will not fit",
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient("alice", readAt: Now)]);

        // act
        var line = MailMessageBadge.Render(message, "alice", Now, selected: false, maxWidth: 20);

        // assert
        line.MatchInlineSnapshot("    bob A very … now");
    }

    [Fact]
    public void Render_Should_ReturnEmpty_When_MaxWidthIsZero()
    {
        // arrange
        var message = MailMessageBuilder.Create("m-1", createdAt: Now);

        // act
        var line = MailMessageBadge.Render(message, "actor", Now, selected: false, maxWidth: 0);

        // assert
        Assert.Equal(string.Empty, line);
    }

    [Fact]
    public void Render_Should_EscapeSubject_When_SubjectContainsMarkupChars()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            subject: "[URGENT] fix this",
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient("alice", readAt: Now)]);
        var console = new TestConsole().Width(80);

        // act
        var line = MailMessageBadge.Render(message, "alice", Now, selected: false, maxWidth: 80);
        var exception = Record.Exception(() => console.Write(new Markup(line)));

        // assert
        Assert.Null(exception);
        Assert.Contains("[URGENT] fix this", console.Output);
    }
}
