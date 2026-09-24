using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Agents;
using Microsoft.Extensions.Time.Testing;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

/// <summary>
/// Exercises <see cref="AgentPopoverModel"/> against fakes: loading the header and
/// participation sections, cursor navigation, the show-more list, the delete and copy
/// gestures, and tick-driven age recomputation.
/// </summary>
public sealed class AgentPopoverModelTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0') => new(ch, key, false, false, false);

    private static string RenderToText(AgentPopoverModel model, int width = 100, int height = 30)
    {
        var console = new TestConsole().Width(width);
        console.Write(model.Render(width, height));
        return console.Output;
    }

    private static AgentRow AddOnlineAgent(FakeAgentStore store, string sessionId)
        => store.StartSessionAsync(
                new AgentSessionStartRequest
                {
                    Harness = AgentSessionHarness.ClaudeCode,
                    SessionId = sessionId,
                    HarnessVersion = "1.0.0",
                    Cwd = "",
                    WorkspacePath = "",
                    EndpointKind = AgentSessionEndpointKind.ClaudePeer,
                    EndpointAddr = "peer-1"
                },
                TestContext.Current.CancellationToken)
            .GetAwaiter().GetResult().Row!;

    private static AgentRow LoginAgent(FakeAgentStore store)
        => store.LoginAsync(TestContext.Current.CancellationToken).GetAwaiter().GetResult();

    private static void EndSession(FakeAgentStore store, AgentRow agent)
        => store.EndSessionAsync(agent.Harness!, agent.SessionId!, TestContext.Current.CancellationToken)
            .GetAwaiter().GetResult();

    private static AgentPopoverModel CreateModel(
        string name,
        FakeAgentStore agentStore,
        FakeMailStore? mailStore = null,
        FakeTaskStore? taskStore = null,
        FakeMemoryStore? memoryStore = null,
        TimeProvider? timeProvider = null)
        => new(
            name,
            agentStore,
            mailStore ?? new FakeMailStore(),
            taskStore ?? new FakeTaskStore(),
            memoryStore ?? new FakeMemoryStore(),
            timeProvider ?? new FakeTimeProvider(s_now));

    [Fact]
    public void Render_Should_ShowTheHarnessAndSessionId_When_TheAgentIsAHookAgent()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var model = CreateModel(agent.Name, agentStore);
        model.Load();

        // act
        var text = RenderToText(model);

        // assert
        Assert.Contains("claude-code 1.0.0", text);
        Assert.Contains("s-a", text);
    }

    [Fact]
    public void Render_Should_ShowDashesForHarnessAndSession_When_TheAgentIsLoginOnly()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = LoginAgent(agentStore);
        var model = CreateModel(agent.Name, agentStore);
        model.Load();

        // act
        var text = RenderToText(model);

        // assert
        Assert.Contains("Harness: -", text);
        Assert.Contains("Session id: -", text);
    }

    [Fact]
    public void Render_Should_ShowTheEndedRow_When_TheAgentSessionHasEnded()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        EndSession(agentStore, agent);
        var model = CreateModel(agent.Name, agentStore);
        model.Load();

        // act
        var text = RenderToText(model);

        // assert
        Assert.Contains("Ended:", text);
    }

    [Fact]
    public void Render_Should_ShowNotFound_When_TheAgentCannotBeLoaded()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var model = CreateModel("nobody", agentStore);
        model.Load();

        // act
        var text = RenderToText(model);

        // assert
        Assert.Contains("Agent not found.", text);
    }

    [Fact]
    public void Render_Should_ShowExactlyTenMailRowsAndAShowMoreRow_When_ThereAreTwelveMailThreads()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var mailStore = new FakeMailStore
        {
            ParticipationRows = [.. Enumerable.Range(0, 12).Select(CreateMailSummary)]
        };
        var model = CreateModel(agent.Name, agentStore, mailStore: mailStore);
        model.Load();

        // act
        var text = RenderToText(model);
        var rowCount = text.Split("Subject ").Length - 1;

        // assert
        Assert.Equal(10, rowCount);
        Assert.Contains("show more", text);
    }

    [Fact]
    public void HandleKey_Should_OpenTheFullMailList_When_EnterIsPressedOnTheShowMoreRow()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var mailStore = new FakeMailStore
        {
            ParticipationRows = [CreateMailSummary(0), CreateMailSummary(1)]
        };
        var model = CreateModel(agent.Name, agentStore, mailStore: mailStore);
        model.Load();
        model.HandleKey(Key(ConsoleKey.J, 'j'));
        model.HandleKey(Key(ConsoleKey.J, 'j'));

        // act
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));
        var text = RenderToText(model);

        // assert
        Assert.Contains($"{agent.Name} — Mail", text);
        Assert.Contains("Subject 0", text);
    }

    [Fact]
    public void HandleKey_Should_ReturnToTheSummary_When_EscapeIsPressedInTheFullList()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var mailStore = new FakeMailStore
        {
            ParticipationRows = [CreateMailSummary(0), CreateMailSummary(1)]
        };
        var model = CreateModel(agent.Name, agentStore, mailStore: mailStore);
        model.Load();
        model.HandleKey(Key(ConsoleKey.J, 'j'));
        model.HandleKey(Key(ConsoleKey.J, 'j'));
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));

        // act
        var result = model.HandleKey(Key(ConsoleKey.Escape));
        var text = RenderToText(model);

        // assert
        Assert.Null(result);
        Assert.Contains("Harness:", text);
    }

    [Fact]
    public void HandleKey_Should_ReturnDeleteRequested_When_DIsPressed()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var model = CreateModel(agent.Name, agentStore);
        model.Load();

        // act
        var result = model.HandleKey(Key(ConsoleKey.D, 'd'));

        // assert
        var delete = Assert.IsType<AgentPopoverResult.DeleteRequested>(result);
        Assert.Equal(agent.Name, delete.Name);
    }

    [Fact]
    public void HandleKey_Should_ReturnCopyRequestedWithTheAgentsOwnSessionId_When_YIsPressed()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var model = CreateModel(agent.Name, agentStore);
        model.Load();

        // act
        var result = model.HandleKey(Key(ConsoleKey.Y, 'y'));

        // assert
        var copy = Assert.IsType<AgentPopoverResult.CopyRequested>(result);
        Assert.Equal((agent.Name, agent.SessionId), (copy.Name, copy.SessionId));
    }

    [Fact]
    public void Render_Should_ShowTenRowsInTheSummaryAndAllTwentyFiveInEachShowMoreList_When_ThereAreTwentyFiveOfEachParticipationType()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var mailStore = new FakeMailStore
        {
            ParticipationRows = [.. Enumerable.Range(0, 25).Select(CreateMailSummary)]
        };
        var taskStore = new FakeTaskStore
        {
            ParticipationRows = [.. Enumerable.Range(0, 25).Select(i => TaskItemBuilder.Create($"a{i}", $"Ticket {i}"))]
        };
        var memoryStore = new FakeMemoryStore
        {
            ParticipationRows = [.. Enumerable.Range(0, 25).Select(CreateMemoryEntry)]
        };
        var model = CreateModel(agent.Name, agentStore, mailStore, taskStore, memoryStore);
        model.Load();

        // act
        // height 100 keeps every section and its show-more row on screen; show-more rows sit at cursor 10, 21 and 32
        var summary = RenderToText(model, height: 100);
        var summaryCounts = (
            Mail: summary.Split("Subject ").Length - 1,
            Tickets: summary.Split("Ticket ").Length - 1,
            Memory: summary.Split("journal  now  Memory ").Length - 1,
            ShowMore: summary.Split("show more").Length - 1);

        MoveCursorDown(model, 10);
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));
        var mailListCount = RenderToText(model).Split("Subject ").Length - 1;
        model.HandleKey(Key(ConsoleKey.Escape));

        MoveCursorDown(model, 11);
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));
        var ticketListCount = RenderToText(model).Split("Ticket ").Length - 1;
        model.HandleKey(Key(ConsoleKey.Escape));

        MoveCursorDown(model, 11);
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));
        var memoryListCount = RenderToText(model).Split("journal  now  Memory ").Length - 1;

        // assert
        Assert.Equal((10, 10, 10, 3), summaryCounts);
        Assert.Equal((25, 25, 25), (mailListCount, ticketListCount, memoryListCount));
    }

    [Fact]
    public void HandleKey_Should_ReturnClosed_When_EscapeIsPressedAtTheSummaryLevel()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var model = CreateModel(agent.Name, agentStore);
        model.Load();

        // act
        var result = model.HandleKey(Key(ConsoleKey.Escape));

        // assert
        Assert.IsType<AgentPopoverResult.Closed>(result);
    }

    [Fact]
    public void Tick_Should_ReturnFalse_When_NoTimeHasPassed()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var agentStore = new FakeAgentStore(time);
        var agent = AddOnlineAgent(agentStore, "s-a");
        var model = CreateModel(agent.Name, agentStore, timeProvider: time);
        model.Load();

        // act
        var dirty = model.Tick();

        // assert
        Assert.False(dirty);
    }

    [Fact]
    public void Tick_Should_ReturnTrueAndUpdateTheLastSeenAge_When_TimeAdvances()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var agentStore = new FakeAgentStore(time);
        var agent = AddOnlineAgent(agentStore, "s-a");
        var model = CreateModel(agent.Name, agentStore, timeProvider: time);
        model.Load();

        // act
        time.Advance(TimeSpan.FromMinutes(2));
        var dirty = model.Tick();
        var text = RenderToText(model);

        // assert
        Assert.True(dirty);
        Assert.Contains("2m ago", text);
    }

    [Fact]
    public void Tick_Should_ReturnTrueAndUpdateTheMailRowAge_When_OnlyAMailThreadAgeChanges()
    {
        // arrange
        // The agent's own LastSeen is already day-old, so only the mail thread's age moves.
        var time = new FakeTimeProvider(s_now);
        var agentStore = new FakeAgentStore(time);
        var agent = AddOnlineAgent(agentStore, "s-a");
        time.Advance(TimeSpan.FromDays(2));
        var mailStore = new FakeMailStore { ParticipationRows = [CreateMailSummary(0) with { LastMessageAt = time.GetUtcNow() }] };
        var model = CreateModel(agent.Name, agentStore, mailStore: mailStore, timeProvider: time);
        model.Load();

        // act
        time.Advance(TimeSpan.FromMinutes(2));
        var dirty = model.Tick();
        var text = RenderToText(model);

        // assert
        Assert.True(dirty);
        Assert.Contains("2m", text);
    }

    [Fact]
    public void Tick_Should_ReturnTrueAndUpdateTheFullListRowAge_When_TimeAdvancesWhileAShowMoreListIsOpen()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var agentStore = new FakeAgentStore(time);
        var agent = AddOnlineAgent(agentStore, "s-a");
        time.Advance(TimeSpan.FromDays(2));
        var mailStore = new FakeMailStore { ParticipationRows = [CreateMailSummary(0) with { LastMessageAt = time.GetUtcNow() }] };
        var model = CreateModel(agent.Name, agentStore, mailStore: mailStore, timeProvider: time);
        model.Load();
        model.HandleKey(Key(ConsoleKey.J, 'j'));
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));

        // act
        time.Advance(TimeSpan.FromMinutes(2));
        var dirty = model.Tick();
        var text = RenderToText(model);

        // assert
        Assert.True(dirty);
        Assert.Contains("2m", text);
    }

    private static void MoveCursorDown(AgentPopoverModel model, int times)
    {
        for (var i = 0; i < times; i++)
        {
            model.HandleKey(Key(ConsoleKey.J, 'j'));
        }
    }

    private static MemoryParticipationEntry CreateMemoryEntry(int index) =>
        new(MemoryParticipationKind.Journal, $"j{index}", null, [], $"Memory {index}", s_now);

    private static MailThreadSummary CreateMailSummary(int index) => new()
    {
        ThreadId = $"t{index}",
        Subject = $"Subject {index}",
        MessageCount = 1,
        LastMessageAt = s_now,
        LastSender = "felix",
        LastRecipients = ["oscar"],
        BodyPreview = "",
        UnreadCount = 0,
        ArchivedCount = 0
    };
}
