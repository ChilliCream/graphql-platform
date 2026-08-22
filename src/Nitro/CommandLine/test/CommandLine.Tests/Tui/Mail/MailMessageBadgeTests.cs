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
        line.MatchInlineSnapshot("    [green]+[/] [white]bob[/] Status update [dim grey58]5m[/]");
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
        Assert.Contains("  * [green]+[/] [white]bob[/]", line);
        Assert.Contains("[bold white]Status update[/]", line);
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
        line.MatchInlineSnapshot(
            "[default on grey35]>   [green]+[/] [white]bob[/] Status update [dim grey58]now[/][/]");
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
        var line = MailMessageBadge.Render(message, "alice", Now, selected: false, maxWidth: 22);

        // assert
        line.MatchInlineSnapshot("    [green]+[/] [white]bob[/] A very … [dim grey58]now[/]");
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

    [Fact]
    public void Render_Should_ShowSender_When_MessageIsReceived()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            subject: "Status update",
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient("alice", readAt: Now)]);

        // act
        var line = MailMessageBadge.Render(message, "alice", Now, selected: false, maxWidth: 80);

        // assert
        Assert.Contains("[white]bob[/] Status update", line);
    }

    [Fact]
    public void Render_Should_ShowToRecipient_When_ActorSentTheMessage()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "alice",
            subject: "Status update",
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient("bob", readAt: Now)]);

        // act
        var line = MailMessageBadge.Render(message, "alice", Now, selected: false, maxWidth: 80);

        // assert
        Assert.Contains("[dim grey58]To [/][white]bob[/] Status update", line);
    }

    [Fact]
    public void Render_Should_ShowFirstRecipientPlusOverflow_When_ActorBroadcastToMany()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "alice",
            subject: "Status update",
            createdAt: Now,
            recipients:
            [
                MailMessageBuilder.ToRecipient("bob", ordinal: 0, readAt: Now),
                MailMessageBuilder.ToRecipient("carol", ordinal: 1, readAt: Now),
                MailMessageBuilder.ToRecipient("dave", ordinal: 2, readAt: Now)
            ]);

        // act
        var line = MailMessageBadge.Render(message, "alice", Now, selected: false, maxWidth: 80);

        // assert
        Assert.Contains("[dim grey58]To [/][white]bob+2[/] Status update", line);
    }

    [Fact]
    public void Render_Should_ShowSender_When_MessageIsBetweenTwoOtherAgents()
    {
        // arrange: the actor is neither the sender nor a recipient.
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            subject: "Status update",
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient("carol", readAt: Now)]);

        // act
        var line = MailMessageBadge.Render(message, "alice", Now, selected: false, maxWidth: 80);

        // assert
        Assert.Contains("[white]bob[/] Status update", line);
        Assert.DoesNotContain("To ", line);
    }

    [Fact]
    public void Render_Should_ShowFromActorGlyph_When_ActorSentTheMessage()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "alice",
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient("bob", readAt: Now)]);

        // act
        var line = MailMessageBadge.Render(message, "alice", Now, selected: false, maxWidth: 80);

        // assert
        Assert.Contains("    [skyblue1]F[/] [dim grey58]To [/][white]bob[/]", line);
    }

    [Fact]
    public void Render_Should_ShowDirectGlyph_When_ActorIsSoleRecipient()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient("alice", readAt: Now)]);

        // act
        var line = MailMessageBadge.Render(message, "alice", Now, selected: false, maxWidth: 80);

        // assert
        Assert.Contains("    [green]+[/] [white]bob[/]", line);
    }

    [Fact]
    public void Render_Should_ShowBroadcastGlyph_When_ActorIsOneOfSeveralRecipients()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            createdAt: Now,
            recipients:
            [
                MailMessageBuilder.ToRecipient("alice", ordinal: 0, readAt: Now),
                MailMessageBuilder.ToRecipient("carol", ordinal: 1, readAt: Now)
            ]);

        // act
        var line = MailMessageBadge.Render(message, "alice", Now, selected: false, maxWidth: 80);

        // assert
        Assert.Contains("    [orange1]T[/] [white]bob[/]", line);
    }

    [Fact]
    public void Render_Should_ShowBlankGlyph_When_ActorIsNeitherSenderNorRecipient()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient("carol", readAt: Now)]);

        // act
        var line = MailMessageBadge.Render(message, "alice", Now, selected: false, maxWidth: 80);

        // assert: the blank glyph is a bare space with no color token, so
        // none of the relationship-glyph colors appear on this row.
        Assert.Contains("[white]bob[/]", line);
        Assert.DoesNotContain("[skyblue1]F[/]", line);
        Assert.DoesNotContain("[green]+[/]", line);
        Assert.DoesNotContain("[orange1]T[/]", line);
    }

    [Fact]
    public void Render_Should_TruncateSubjectAccountingForPeerPrefixAndGlyph_When_LineExceedsMaxWidth()
    {
        // arrange: same subject and width as the pre-PEER-column truncation
        // test, but now the actor is the sender so the "To " prefix and the
        // 'F' glyph both eat into the same budget.
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "alice",
            subject: "A very long subject line that will not fit",
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient("bob", readAt: Now)]);

        // act
        var line = MailMessageBadge.Render(message, "alice", Now, selected: false, maxWidth: 24);

        // assert: fixed parts are prefix(2) + marker(1) + space(1) +
        // glyph(1) + space(1) + "To bob"(6) + space(1) + age "now"(3) +
        // space(1) = 17, leaving a 7-column budget for the subject
        // including its trailing ellipsis.
        line.MatchInlineSnapshot(
            "    [skyblue1]F[/] [dim grey58]To [/][white]bob[/] A very… [dim grey58]now[/]");
    }

    [Fact]
    public void Render_Should_BeIdenticalForTheSameMessage_When_RenderedTwice()
    {
        // arrange: the PEER column and glyph must not depend on which
        // mailbox is open, so rendering the same message twice (standing in
        // for two different mailbox contexts) must produce identical output.
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "alice",
            subject: "Status update",
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient("bob", readAt: Now)]);

        // act
        var firstRender = MailMessageBadge.Render(message, "alice", Now, selected: false, maxWidth: 80);
        var secondRender = MailMessageBadge.Render(message, "alice", Now, selected: false, maxWidth: 80);

        // assert
        Assert.Equal(firstRender, secondRender);
    }
}
