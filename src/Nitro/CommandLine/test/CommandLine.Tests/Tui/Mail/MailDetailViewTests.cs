using ChilliCream.Nitro.CommandLine.Tui.Mail;
using Spectre.Console;
using Spectre.Console.Testing;
using static ChilliCream.Nitro.CommandLine.Tests.Tui.AnsiAssertions;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailDetailViewTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static async Task<MailState> CreateStateWithMessageAsync(FakeMailStore store, string body = "Hello there.")
    {
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            subject: "Status update",
            body: body,
            createdAt: s_now,
            recipients: [MailMessageBuilder.ToRecipient("alice")]));

        var state = new MailState("alice", new MailDataLoader(store));
        await state.RefreshAsync(CancellationToken.None);

        // Switch the selected thread to its single-message view.
        state.ShowMessage();
        return state;
    }

    [Fact]
    public async Task Render_Should_ShowSenderAndBody_When_MessageSelected()
    {
        // arrange
        var store = new FakeMailStore();
        var state = await CreateStateWithMessageAsync(store);
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(view.Render(state, 80, 20, focused: true));

        // assert
        Assert.Contains("From: bob", console.Output);
        Assert.Contains("Hello there.", console.Output);
    }

    [Fact]
    public async Task Render_Should_ShowNoMessageSelected_When_MessagesListIsEmptyButLoaded()
    {
        // arrange
        var store = new FakeMailStore();
        var state = new MailState("alice", new MailDataLoader(store));
        await state.RefreshAsync(CancellationToken.None);
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(view.Render(state, 80, 20, focused: true));

        // assert
        Assert.Contains("No messages.", console.Output);
    }

    [Fact]
    public async Task Render_Should_ShowEveryMessageInTheThread_When_ViewModeIsThread()
    {
        // arrange
        var store = new FakeMailStore();
        var state = await CreateStateWithMessageAsync(store);
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "carol", body: "Reply body.", threadId: "m-1", createdAt: s_now.AddMinutes(1)));
        await state.ShowThreadAsync(CancellationToken.None);
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(view.Render(state, 80, 20, focused: true));

        // assert
        Assert.Contains("bob", console.Output);
        Assert.Contains("carol", console.Output);
        Assert.Contains("Reply body.", console.Output);
    }

    [Fact]
    public async Task Render_Should_NotThrow_When_WidthOrHeightIsZero()
    {
        // arrange
        var store = new FakeMailStore();
        var state = await CreateStateWithMessageAsync(store);
        var view = new MailDetailView();

        // act
        var exception = Record.Exception(() => view.Render(state, 0, 0, focused: true));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task Render_Should_WordWrapLongBody_WithoutClippingContent()
    {
        // arrange
        var store = new FakeMailStore();
        var body = string.Join(" ", Enumerable.Repeat("word", 200));
        var state = await CreateStateWithMessageAsync(store, body);
        var view = new MailDetailView();
        var console = new TestConsole().Width(30).Height(40);

        // act
        var exception = Record.Exception(() => console.Write(view.Render(state, 30, 40, focused: true)));

        // assert
        Assert.Null(exception);
        Assert.Contains("word", console.Output);
    }

    [Fact]
    public async Task Render_Should_ShowEachRecipientsOwnState_When_ReadStatesAreMixed()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            createdAt: s_now,
            recipients:
            [
                MailMessageBuilder.ToRecipient("orchestrator", ordinal: 0, readAt: s_now.AddHours(14).AddMinutes(2)),
                MailMessageBuilder.ToRecipient("planner-1", ordinal: 1)
            ]));
        var state = new MailState("orchestrator", new MailDataLoader(store));
        await state.RefreshAsync(CancellationToken.None);
        state.ShowMessage(); // Threads mode defaults a single-message thread's row to Thread view
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(view.Render(state, 80, 20, focused: true));

        // assert
        Assert.Contains("orchestrator: read", console.Output);
        Assert.Contains("planner-1: unread", console.Output);
    }

    [Fact]
    public async Task Render_Should_ShowArchivedMarker_When_RecipientHasArchived()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            createdAt: s_now,
            recipients:
            [
                MailMessageBuilder.ToRecipient("alice", readAt: s_now, archivedAt: s_now.AddMinutes(5))
            ]));
        var state = new MailState("alice", new MailDataLoader(store));
        await state.RefreshAsync(CancellationToken.None);
        // Use Workspace to include archived messages.
        await state.SelectMailboxAsync(MailMailbox.Workspace, CancellationToken.None);
        state.ShowMessage(); // Threads mode defaults a single-message thread's row to Thread view
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(view.Render(state, 80, 20, focused: true));

        // assert
        Assert.Contains("alice: read", console.Output);
        Assert.Contains("archived", console.Output);
    }

    [Fact]
    public async Task Render_Should_ShowRecipientStatesAndNoSenderState_When_ActorSentTheMessage()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1",
            sender: "alice",
            createdAt: s_now,
            recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var state = new MailState("alice", new MailDataLoader(store));
        await state.RefreshAsync(CancellationToken.None);
        // Select Sent to display the message sent by alice.
        await state.SelectMailboxAsync(MailMailbox.Sent, CancellationToken.None);
        state.ShowMessage(); // Threads mode defaults a single-message thread's row to Thread view
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(view.Render(state, 80, 20, focused: true));

        // assert
        Assert.Contains("bob: unread", console.Output);
        Assert.DoesNotContain("alice: unread", console.Output);
        Assert.DoesNotContain("alice: read", console.Output);
    }

    [Fact]
    public async Task Render_Should_AttributeSenderAndRecipientClient_When_LookupHasNonEmptyEntries()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            createdAt: s_now,
            recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = new MailState("alice", new MailDataLoader(store));
        await state.RefreshAsync(CancellationToken.None);
        state.ShowMessage(); // Threads mode defaults a single-message thread's row to Thread view
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);
        var clientsByName = new Dictionary<string, string>
        {
            ["bob"] = "Codex",
            ["alice"] = "Claude Code"
        };

        // act
        console.Write(view.Render(state, 80, 20, focused: true, clientsByName));

        // assert
        Assert.Contains("From: bob (Codex)", console.Output);
        Assert.Contains("alice (Claude Code): unread", console.Output);
    }

    [Fact]
    public async Task Render_Should_ShowNoAttribution_When_ClientIsEmptyOrAgentIsUnknownToTheLookup()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            createdAt: s_now,
            recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = new MailState("alice", new MailDataLoader(store));
        await state.RefreshAsync(CancellationToken.None);
        state.ShowMessage(); // Threads mode defaults a single-message thread's row to Thread view
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);
        // Supply an empty client for bob and no lookup entry for alice.
        var clientsByName = new Dictionary<string, string> { ["bob"] = "" };

        // act
        console.Write(view.Render(state, 80, 20, focused: true, clientsByName));

        // assert
        Assert.Contains("From: bob", console.Output);
        Assert.DoesNotContain("From: bob (", console.Output);
        Assert.Contains("alice: unread", console.Output);
        Assert.DoesNotContain("alice (", console.Output);
    }

    [Fact]
    public async Task Render_Should_ShowBracketsLiterally_When_ClientContainsMarkupSyntax()
    {
        // arrange
        // Use a client name containing markup brackets.
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            createdAt: s_now,
            recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = new MailState("alice", new MailDataLoader(store));
        await state.RefreshAsync(CancellationToken.None);
        state.ShowMessage(); // Threads mode defaults a single-message thread's row to Thread view
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);
        var clientsByName = new Dictionary<string, string> { ["bob"] = "claude-opus-5[1m]" };

        // act
        var exception = Record.Exception(() => console.Write(view.Render(state, 80, 20, focused: true, clientsByName)));

        // assert
        Assert.Null(exception);
        Assert.Contains("From: bob (claude-opus-5[1m])", console.Output);
    }

    [Fact]
    public async Task Render_Should_AttributeEachSpeakerInTheThread_When_LookupHasEntries()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", body: "Hello.", createdAt: s_now,
            recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "carol", body: "Reply body.", threadId: "m-1", createdAt: s_now.AddMinutes(1)));
        var state = new MailState("alice", new MailDataLoader(store));
        await state.RefreshAsync(CancellationToken.None);
        await state.ShowThreadAsync(CancellationToken.None);
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);
        var clientsByName = new Dictionary<string, string> { ["bob"] = "Codex", ["carol"] = "Claude Code" };

        // act
        console.Write(view.Render(state, 80, 20, focused: true, clientsByName));

        // assert
        Assert.Contains("bob (Codex)", console.Output);
        Assert.Contains("carol (Claude Code)", console.Output);
    }

    [Fact]
    public async Task Render_Should_StayWithinPaneHeight_When_BroadcastHasManyRecipients()
    {
        // arrange
        var store = new FakeMailStore();
        var recipients = Enumerable.Range(0, 20)
            .Select(i => MailMessageBuilder.ToRecipient($"agent-{i}", ordinal: i))
            .ToArray();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: s_now, recipients: recipients));
        var state = new MailState("agent-0", new MailDataLoader(store));
        await state.RefreshAsync(CancellationToken.None);
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);

        // act
        var exception = Record.Exception(() => console.Write(view.Render(state, 80, 20, focused: true)));

        // assert
        Assert.Null(exception);
        var lineCount = console.Output.Split('\n').Length;
        Assert.True(lineCount <= 21, $"Expected the panel to stay within its height budget, but got {lineCount} lines.");
    }

    [Fact]
    public async Task Render_Should_ApplyAnsiStyling_ToFieldLabelToken()
    {
        // arrange
        var store = new FakeMailStore();
        var state = await CreateStateWithMessageAsync(store);
        var view = new MailDetailView();
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(80).Height(20);

        // act
        console.Write(view.Render(state, 80, 20, focused: true));

        // assert
        AssertAnsiStylePrefixesText(console.Output, "detail.section.header", "From:");
    }

    [Fact]
    public async Task Render_Should_ApplyAnsiStyling_ToRecipientStateTokens_ForUnreadAndRead()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            createdAt: s_now,
            recipients:
            [
                MailMessageBuilder.ToRecipient("orchestrator", ordinal: 0, readAt: s_now.AddHours(14).AddMinutes(2)),
                MailMessageBuilder.ToRecipient("planner-1", ordinal: 1)
            ]));
        var state = new MailState("orchestrator", new MailDataLoader(store));
        await state.RefreshAsync(CancellationToken.None);
        state.ShowMessage(); // Threads mode defaults a single-message thread's row to Thread view
        var view = new MailDetailView();
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(80).Height(20);

        // act
        console.Write(view.Render(state, 80, 20, focused: true));

        // assert
        AssertAnsiStylePrefixesText(
            console.Output, "mail.detail.recipient.read", "orchestrator: read 2026-01-01 14:02");
        AssertAnsiStylePrefixesText(console.Output, "mail.detail.recipient.unread", "planner-1: unread");
    }
}
