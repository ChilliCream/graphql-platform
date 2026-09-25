using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Microsoft.Extensions.Time.Testing;
using Spectre.Console;
using Spectre.Console.Testing;
using static ChilliCream.Nitro.CommandLine.Tests.Tui.AnsiAssertions;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

/// <summary>
/// Exercises <see cref="MailThreadPopoverModel"/> against fakes: loading and rendering a
/// thread's header and messages, harness attribution, scrolling, the copy-id gesture, and
/// tick-driven age recomputation.
/// </summary>
public sealed class MailThreadPopoverModelTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0', bool shift = false) =>
        new(ch, key, shift, false, false);

    private static string RenderToText(MailThreadPopoverModel model, int width = 100, int height = 30)
    {
        var console = new TestConsole().Width(width);
        console.Write(model.Render(width, height));
        return console.Output;
    }

    private static string RenderToAnsiText(MailThreadPopoverModel model, int width = 100, int height = 30)
    {
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(width);
        console.Write(model.Render(width, height));
        return console.Output;
    }

    private static MailThreadPopoverModel CreateModel(
        string threadId, FakeMailStore mailStore, FakeAgentStore? agentStore = null, TimeProvider? timeProvider = null)
        => new(threadId, mailStore, agentStore ?? new FakeAgentStore(new FakeTimeProvider(s_now)), timeProvider ?? new FakeTimeProvider(s_now));

    private static MailMessage CreateMessage(
        string id,
        string threadId,
        string sender,
        string subject,
        string body,
        DateTimeOffset createdAt,
        IReadOnlyList<MailRecipient>? recipients = null)
        => MailMessageBuilder.Create(
            id, sender: sender, subject: subject, body: body, threadId: threadId, createdAt: createdAt,
            recipients: recipients ?? [MailMessageBuilder.ToRecipient("alice")]);

    private static void SeedAgentWithHarness(FakeAgentStore store, string name, string harness) => store.Seed(new AgentRow
    {
        Name = name,
        Role = "",
        Harness = harness,
        HarnessVersion = "1.0.0",
        SessionId = $"s-{name}",
        Cwd = "",
        WorkspacePath = "",
        RegisteredAt = s_now,
        StartedAt = s_now,
        LastSeenAt = s_now,
        EndpointKind = AgentSessionEndpointKind.ClaudePeer,
        EndpointAddr = "peer-1",
        BlockBudgetUsed = 0,
        AnnouncementPending = false,
        IdlePushArmed = false
    });

    [Fact]
    public void Render_Should_ShowTheSubjectAsTheTitle_When_ThreadIsLoaded()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(CreateMessage("m1", "t1", "bob", "Status update", "Hello.", s_now));
        var model = CreateModel("t1", store);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var text = RenderToText(model);

        // assert
        Assert.Contains("Status update", text);
    }

    [Fact]
    public void Render_Should_ShowTheSubjectLiterally_When_TheSubjectContainsBrackets()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(CreateMessage("m1", "t1", "bob", "[Fusion] Add x", "Hello.", s_now));
        var model = CreateModel("t1", store);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var text = RenderToText(model);

        // assert
        Assert.Contains("[Fusion] Add x", text);
    }

    [Fact]
    public void Render_Should_ShowParticipantsMessageCountAndAges_When_ThreadHasThreeMessages()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(CreateMessage(
            "m1", "t1", "bob", "Status update", "First.", s_now,
            [MailMessageBuilder.ToRecipient("alice"), MailMessageBuilder.CcRecipient("carol")]));
        store.Messages.Add(CreateMessage("m2", "t1", "alice", "Status update", "Second.", s_now.AddMinutes(1)));
        store.Messages.Add(CreateMessage("m3", "t1", "bob", "Status update", "Third.", s_now.AddMinutes(2)));
        var time = new FakeTimeProvider(s_now.AddMinutes(2));
        var model = CreateModel("t1", store, timeProvider: time);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var text = RenderToText(model);

        // assert
        Assert.Contains("Participants: alice, bob, carol", text);
        Assert.Contains("Messages: 3", text);
        Assert.Contains("Started: 2m", text);
        Assert.Contains("Last activity: now", text);
    }

    [Fact]
    public void Render_Should_ShowEveryMessageOldestFirstWithSpeakerRecipientsAndBody_When_ThreadHasThreeMessages()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(CreateMessage(
            "m1", "t1", "bob", "Status update", "First message.", s_now,
            [MailMessageBuilder.ToRecipient("alice"), MailMessageBuilder.CcRecipient("carol")]));
        store.Messages.Add(CreateMessage("m2", "t1", "alice", "Status update", "Second message.", s_now.AddMinutes(1)));
        store.Messages.Add(CreateMessage("m3", "t1", "bob", "Status update", "Third message.", s_now.AddMinutes(2)));
        var model = CreateModel("t1", store, timeProvider: new FakeTimeProvider(s_now));
        model.Load(TestContext.Current.CancellationToken);
        var console = new TestConsole().Width(100);

        // act
        console.Write(model.Render(100, 30));

        // assert
        console.Output.MatchInlineSnapshot(
            """



                      ╭─Status update────────────────────────────────────────────────────────────────╮
                      │                                                                              │
                      │ Participants: alice, bob, carol                                              │
                      │ Messages: 3                                                                  │
                      │ Started: now                                                                 │
                      │ Last activity: now                                                           │
                      │                                                                              │
                      │ bob · now                                                                    │
                      │ To: alice                                                                    │
                      │ Cc: carol                                                                    │
                      │ First message.                                                               │
                      │                                                                              │
                      │ alice · now                                                                  │
                      │ To: alice                                                                    │
                      │ Second message.                                                              │
                      │                                                                              │
                      │ bob · now                                                                    │
                      │ To: alice                                                                    │
                      │ Third message.                                                               │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      ╰──────────────────────────────────────────────────────────────────────────────╯



            """);
    }

    [Fact]
    public void Render_Should_AttributeTheSenderWithItsHarness_When_TheSenderHasAKnownHarness()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        SeedAgentWithHarness(agentStore, "bob", AgentSessionHarness.ClaudeCode);
        var store = new FakeMailStore();
        store.Messages.Add(CreateMessage("m1", "t1", "bob", "Subject", "Body.", s_now));
        var model = CreateModel("t1", store, agentStore);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var text = RenderToText(model);

        // assert
        Assert.Contains("bob (Claude Code)", text);
    }

    [Fact]
    public void Render_Should_ShowTheSenderAlone_When_NoAgentRowExists()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(CreateMessage("m1", "t1", "bob", "Subject", "Body.", s_now));
        var model = CreateModel("t1", store);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var text = RenderToText(model);
        var actual = (
            HasName: text.Contains("bob", StringComparison.Ordinal),
            HasAttribution: text.Contains("bob (", StringComparison.Ordinal));

        // assert
        Assert.Equal((true, false), actual);
    }

    [Fact]
    public void Render_Should_ApplyTheSectionTitleStyle_When_RenderingTheSpeakerLine()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(CreateMessage("m1", "t1", "bob", "Subject", "Body.", s_now));
        var model = CreateModel("t1", store);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var output = RenderToAnsiText(model);

        // assert
        AssertAnsiStylePrefixesText(output, "detail.section.header", "bob");
    }

    [Fact]
    public void Load_Should_NotMarkAnyMessageRead_When_TheThreadHasAnUnreadMessage()
    {
        // arrange
        var store = new FakeMailStore();
        var recipient = MailMessageBuilder.ToRecipient("alice");
        store.Messages.Add(CreateMessage("m1", "t1", "bob", "Subject", "Body.", s_now, [recipient]));
        var model = CreateModel("t1", store);

        // act
        model.Load(TestContext.Current.CancellationToken);

        // assert
        var stored = Assert.Single(store.Messages);
        Assert.Null(Assert.Single(stored.Recipients).ReadAt);
    }

    [Fact]
    public void HandleKey_Should_ScrollTheBody_When_JIsPressedRepeatedly()
    {
        // arrange
        var store = new FakeMailStore();
        var body = string.Join('\n', Enumerable.Range(0, 40).Select(i => $"Line {i}"));
        store.Messages.Add(CreateMessage("m1", "t1", "bob", "Subject", body, s_now));
        var model = CreateModel("t1", store);
        model.Load(TestContext.Current.CancellationToken);
        var before = RenderToText(model, height: 10);

        // act
        for (var i = 0; i < 5; i++)
        {
            model.HandleKey(Key(ConsoleKey.J, 'j'));
        }

        var after = RenderToText(model, height: 10);

        // assert
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void HandleKey_Should_JumpToTheBottomAndBackToTheTop_When_ShiftGThenGArePressed()
    {
        // arrange
        var store = new FakeMailStore();
        var body = string.Join('\n', Enumerable.Range(0, 40).Select(i => $"Line {i}"));
        store.Messages.Add(CreateMessage("m1", "t1", "bob", "Subject", body, s_now));
        var model = CreateModel("t1", store);
        model.Load(TestContext.Current.CancellationToken);
        var top = RenderToText(model, height: 10);

        // act
        model.HandleKey(Key(ConsoleKey.G, 'G', shift: true));
        var bottom = RenderToText(model, height: 10);
        model.HandleKey(Key(ConsoleKey.G, 'g'));
        var backAtTop = RenderToText(model, height: 10);

        // assert
        Assert.NotEqual(top, bottom);
        Assert.Equal(top, backAtTop);
    }

    [Fact]
    public void HandleKey_Should_ReturnCopyRequestedWithTheThreadId_When_YIsPressed()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(CreateMessage("m1", "t1", "bob", "Subject", "Body.", s_now));
        var model = CreateModel("t1", store);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var result = model.HandleKey(Key(ConsoleKey.Y, 'y'));

        // assert
        var request = Assert.IsType<PopoverResult.Request>(result);
        var copy = Assert.IsType<MailThreadPopoverRequest.CopyRequested>(request.Payload);
        Assert.Equal("t1", copy.ThreadId);
    }

    [Fact]
    public void HandleKey_Should_ReturnClosed_When_EscapeIsPressed()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(CreateMessage("m1", "t1", "bob", "Subject", "Body.", s_now));
        var model = CreateModel("t1", store);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var result = model.HandleKey(Key(ConsoleKey.Escape));

        // assert
        Assert.IsType<PopoverResult.Closed>(result);
    }

    [Fact]
    public void Tick_Should_ReturnFalse_When_NoTimeHasPassed()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeMailStore();
        store.Messages.Add(CreateMessage("m1", "t1", "bob", "Subject", "Body.", s_now));
        var model = CreateModel("t1", store, timeProvider: time);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var dirty = model.Tick();

        // assert
        Assert.False(dirty);
    }

    [Fact]
    public void Tick_Should_ReturnTrueAndUpdateTheAges_When_TimeAdvances()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeMailStore();
        store.Messages.Add(CreateMessage("m1", "t1", "bob", "Subject", "Body.", s_now));
        var model = CreateModel("t1", store, timeProvider: time);
        model.Load(TestContext.Current.CancellationToken);

        // act
        time.Advance(TimeSpan.FromMinutes(2));
        var dirty = model.Tick();
        var text = RenderToText(model);

        // assert
        Assert.True(dirty);
        Assert.Contains("Started: 2m", text);
    }

    [Fact]
    public void DefaultHints_Should_ListScrollCopyIdAndClose_When_Inspected()
    {
        // arrange
        // act
        var hints = MailThreadPopoverModel.DefaultHints;

        // assert
        Assert.Equal(
            [new("j/k", "scroll"), new("y", "copy id"), new("esc", "close")],
            hints);
    }
}
