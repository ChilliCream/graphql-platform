using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Agents;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Microsoft.Extensions.Time.Testing;
using Spectre.Console;
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

    private static string RenderToAnsiText(AgentPopoverModel model, int width = 100, int height = 30)
    {
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(width);
        console.Write(model.Render(width, height));
        return console.Output;
    }

    private static string RenderMarkupToAnsiText(string markupLine)
    {
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(200);
        console.Write(new Markup(markupLine));
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
        model.Load(TestContext.Current.CancellationToken);

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
        model.Load(TestContext.Current.CancellationToken);

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
        model.Load(TestContext.Current.CancellationToken);

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
        model.Load(TestContext.Current.CancellationToken);

        // act
        var text = RenderToText(model);

        // assert
        Assert.Contains("Agent not found.", text);
    }

    [Fact]
    public void Render_Should_ShowSpacerLinesAndSectionTitles_When_ThereAreTwoItemsPerSection()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var mailStore = new FakeMailStore { ParticipationRows = [CreateMailSummary(0), CreateMailSummary(1)] };
        var taskStore = new FakeTaskStore
        {
            ParticipationRows = [TaskItemBuilder.Create("a1", "First"), TaskItemBuilder.Create("a2", "Second")]
        };
        var memoryStore = new FakeMemoryStore { ParticipationRows = [CreateMemoryEntry(0), CreateMemoryEntry(1)] };
        var model = CreateModel(agent.Name, agentStore, mailStore, taskStore, memoryStore);
        model.Load(TestContext.Current.CancellationToken);
        var console = new TestConsole().Width(100);

        // act
        console.Write(model.Render(100, 30));

        // assert
        console.Output.MatchInlineSnapshot(
            """



                      ╭─ackbar───────────────────────────────────────────────────────────────────────╮
                      │                                                                              │
                      │ State: ● Online                                                              │
                      │ Role: -                                                                      │
                      │ Harness: claude-code 1.0.0                                                   │
                      │ Session id: s-a                                                              │
                      │ Started: just now                                                            │
                      │ Last Seen: just now                                                          │
                      │                                                                              │
                      │ Mail (last 10)                                                               │
                      │ now  Subject 0  felix -> oscar (1)                                           │
                      │ now  Subject 1  felix -> oscar (1)                                           │
                      │ › show more                                                                  │
                      │                                                                              │
                      │ Tickets (last 10)                                                            │
                      │ a1  open  First                                                              │
                      │ a2  open  Second                                                             │
                      │ › show more                                                                  │
                      │                                                                              │
                      │ Memory (last 10)                                                             │
                      │ journal  now  Memory 0                                                       │
                      │ journal  now  Memory 1                                                       │
                      │ › show more                                                                  │
                      ╰──────────────────────────────────────────────────────────────────────────────╯



            """);
    }

    [Fact]
    public void HandleKey_Should_SkipTheSpacerAndSectionTitle_When_JMovesFromMailShowMoreIntoTickets()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var mail = new[] { CreateMailSummary(0), CreateMailSummary(1) };
        var tickets = new[] { TaskItemBuilder.Create("a1", "First"), TaskItemBuilder.Create("a2", "Second") };
        var memory = new[] { CreateMemoryEntry(0), CreateMemoryEntry(1) };
        var model = CreateModel(
            agent.Name,
            agentStore,
            new FakeMailStore { ParticipationRows = mail },
            new FakeTaskStore { ParticipationRows = tickets },
            new FakeMemoryStore { ParticipationRows = memory });
        model.Load(TestContext.Current.CancellationToken);
        var highlighted = AgentPopoverView.BuildLines(
            agent, mail, tickets, memory, s_now, 200, (AgentPopoverSection.Tickets, false, 0));
        var unselected = AgentPopoverView.BuildLines(
            agent, mail, tickets, memory, s_now, 200, (AgentPopoverSection.Mail, false, 0));
        var expectedHighlightAnsi = RenderMarkupToAnsiText(highlighted.Lines[highlighted.SelectedLineIndex]);
        var expectedTitleAnsi = RenderMarkupToAnsiText(
            unselected.Lines.First(line => Markup.Remove(line) == "Tickets (last 10)"));

        // act
        MoveCursorDown(model, 3);
        var output = RenderToAnsiText(model);

        // assert
        Assert.Contains(expectedHighlightAnsi, output);
        Assert.Contains(expectedTitleAnsi, output);
    }

    [Fact]
    public void HandleKey_Should_SkipTheSpacerAndSectionTitle_When_KMovesFromTicketsBackToMailShowMore()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var mail = new[] { CreateMailSummary(0), CreateMailSummary(1) };
        var tickets = new[] { TaskItemBuilder.Create("a1", "First"), TaskItemBuilder.Create("a2", "Second") };
        var memory = new[] { CreateMemoryEntry(0), CreateMemoryEntry(1) };
        var model = CreateModel(
            agent.Name,
            agentStore,
            new FakeMailStore { ParticipationRows = mail },
            new FakeTaskStore { ParticipationRows = tickets },
            new FakeMemoryStore { ParticipationRows = memory });
        model.Load(TestContext.Current.CancellationToken);
        var highlighted = AgentPopoverView.BuildLines(
            agent, mail, tickets, memory, s_now, 200, (AgentPopoverSection.Mail, true, -1));
        var expectedHighlightAnsi = RenderMarkupToAnsiText(highlighted.Lines[highlighted.SelectedLineIndex]);

        // act
        MoveCursorDown(model, 3);
        model.HandleKey(Key(ConsoleKey.K, 'k'));
        var output = RenderToAnsiText(model);

        // assert
        Assert.Contains(expectedHighlightAnsi, output);
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
        model.Load(TestContext.Current.CancellationToken);

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
        model.Load(TestContext.Current.CancellationToken);
        model.HandleKey(Key(ConsoleKey.J, 'j'));
        model.HandleKey(Key(ConsoleKey.J, 'j'));

        // act
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));
        var text = RenderToText(model);

        // assert
        Assert.Contains("Mail (2)", text);
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
        model.Load(TestContext.Current.CancellationToken);
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
        model.Load(TestContext.Current.CancellationToken);

        // act
        var result = model.HandleKey(Key(ConsoleKey.D, 'd'));

        // assert
        var request = Assert.IsType<PopoverResult.Request>(result);
        var delete = Assert.IsType<AgentPopoverRequest.DeleteRequested>(request.Payload);
        Assert.Equal(agent.Name, delete.Name);
    }

    [Fact]
    public void HandleKey_Should_ReturnCopyRequestedWithTheAgentsOwnSessionId_When_YIsPressed()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var model = CreateModel(agent.Name, agentStore);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var result = model.HandleKey(Key(ConsoleKey.Y, 'y'));

        // assert
        var request = Assert.IsType<PopoverResult.Request>(result);
        var copy = Assert.IsType<AgentPopoverRequest.CopyRequested>(request.Payload);
        Assert.Equal((agent.Name, agent.SessionId), (copy.Name, copy.SessionId));
    }

    [Fact]
    public void HandleKey_Should_OpenTheMailThreadDetail_When_EnterIsPressedOnAMailRow()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var mailStore = new FakeMailStore { ParticipationRows = [CreateMailSummary(0)] };
        mailStore.Threads["t0"] = [CreateMailMessage("t0", "m1", "felix", "Subject 0", "Body text 0")];
        var model = CreateModel(agent.Name, agentStore, mailStore: mailStore);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var result = model.HandleKey(Key(ConsoleKey.Enter, '\r'));
        var text = RenderToText(model);

        // assert
        Assert.Null(result);
        Assert.Contains("Body text 0", text);
        Assert.Equal(AgentPopoverModel.DetailHints, model.Hints);
    }

    [Fact]
    public void HandleKey_Should_LeaveReadAtUntouched_When_OpeningAMailThreadFromTheSummary()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var mailStore = new FakeMailStore { ParticipationRows = [CreateMailSummary(0)] };
        mailStore.Threads["t0"] = [CreateMailMessage("t0", "m1", "felix", "Subject 0", "Body text 0")];
        var model = CreateModel(agent.Name, agentStore, mailStore: mailStore);
        model.Load(TestContext.Current.CancellationToken);

        // act
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));

        // assert
        Assert.Equal(["t0"], mailStore.LoadedThreadIds);
        Assert.False(mailStore.MarkReadCalled);
    }

    [Fact]
    public void HandleKey_Should_OpenTheTicketDetail_When_EnterIsPressedOnATicketRow()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var task = TaskItemBuilder.Create("a1", "Fix the thing");
        var taskStore = new FakeTaskStore { ParticipationRows = [task] };
        taskStore.Tasks[task.Id] = task;
        var model = CreateModel(agent.Name, agentStore, taskStore: taskStore);
        model.Load(TestContext.Current.CancellationToken);

        // act
        // Mail has no rows, so cursor 1 is the ticket's first item.
        MoveCursorDown(model, 1);
        var result = model.HandleKey(Key(ConsoleKey.Enter, '\r'));
        var text = RenderToText(model);

        // assert
        Assert.Null(result);
        Assert.Contains("Fix the thing", text);
    }

    [Fact]
    public void HandleKey_Should_OpenTheJournalMemoryDetail_When_EnterIsPressedOnAMemoryRow()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var memoryStore = new FakeMemoryStore { ParticipationRows = [CreateMemoryEntry(0)] };
        memoryStore.JournalEntries["j0"] = new MemoryJournalEntry
        {
            Id = "j0",
            Body = "Journal body 0",
            CreatedAt = s_now,
            CreatedBy = "felix"
        };
        var model = CreateModel(agent.Name, agentStore, memoryStore: memoryStore);
        model.Load(TestContext.Current.CancellationToken);

        // act
        // Mail and tickets have no rows, so cursor 2 is memory's first item.
        MoveCursorDown(model, 2);
        var result = model.HandleKey(Key(ConsoleKey.Enter, '\r'));
        var text = RenderToText(model);

        // assert
        Assert.Null(result);
        Assert.Contains("Journal body 0", text);
    }

    [Fact]
    public void HandleKey_Should_RenderTheJournalEntryWithoutTheMemoryTabActionHint_When_EnterIsPressedOnAJournalRow()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var memoryStore = new FakeMemoryStore { ParticipationRows = [CreateMemoryEntry(0)] };
        memoryStore.JournalEntries["j0"] = new MemoryJournalEntry
        {
            Id = "j0",
            Body = "Journal body 0",
            CreatedAt = s_now,
            CreatedBy = "felix"
        };
        var model = CreateModel(agent.Name, agentStore, memoryStore: memoryStore);
        model.Load(TestContext.Current.CancellationToken);

        // act
        // Mail and tickets have no rows, so cursor 2 is memory's first item.
        MoveCursorDown(model, 2);
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));
        var text = RenderToText(model);

        // assert
        text.MatchInlineSnapshot(
            """
            ╭─j0───────────────────────────────────────────────────────────────────────────────────────────────╮
            │ Created: 2026-01-01T00:00:00Z by felix                                                           │
            │                                                                                                  │
            │ Journal body 0                                                                                   │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            │                                                                                                  │
            ╰──────────────────────────────────────────────────────────────────────────────────────────────────╯

            """);
    }

    [Fact]
    public void HandleKey_Should_OpenTheCuratedMemoryDetail_When_TheSelectedMemoryRowIsCurated()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var memoryStore = new FakeMemoryStore
        {
            ParticipationRows = [new MemoryParticipationEntry(MemoryParticipationKind.Curated, "m0", "note", [], "Curated body 0", s_now)]
        };
        memoryStore.CuratedRecords["m0"] = new MemoryRecord
        {
            Id = "m0",
            Type = "note",
            Tags = [],
            Body = "Curated body 0",
            CreatedAt = s_now,
            UpdatedAt = s_now,
            CreatedBy = "felix"
        };
        var model = CreateModel(agent.Name, agentStore, memoryStore: memoryStore);
        model.Load(TestContext.Current.CancellationToken);

        // act
        MoveCursorDown(model, 2);
        var result = model.HandleKey(Key(ConsoleKey.Enter, '\r'));
        var text = RenderToText(model);

        // assert
        Assert.Null(result);
        Assert.Contains("Curated body 0", text);
    }

    [Fact]
    public void HandleKey_Should_OpenTheItemDetail_When_EnterIsPressedInsideTheFullList()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var mailStore = new FakeMailStore { ParticipationRows = [CreateMailSummary(0), CreateMailSummary(1)] };
        mailStore.Threads["t0"] = [CreateMailMessage("t0", "m1", "felix", "Subject 0", "Body text 0")];
        var model = CreateModel(agent.Name, agentStore, mailStore: mailStore);
        model.Load(TestContext.Current.CancellationToken);
        MoveCursorDown(model, 2);
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));

        // act
        var result = model.HandleKey(Key(ConsoleKey.Enter, '\r'));
        var text = RenderToText(model);

        // assert
        Assert.Null(result);
        Assert.Contains("Body text 0", text);
    }

    [Fact]
    public void HandleKey_Should_ReturnToTheSummaryWithCursorUnchanged_When_EscapeIsPressedInADetailOpenedFromTheSummary()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var mail = new[] { CreateMailSummary(0), CreateMailSummary(1) };
        var mailStore = new FakeMailStore { ParticipationRows = mail };
        mailStore.Threads["t1"] = [CreateMailMessage("t1", "m2", "felix", "Subject 1", "Body text 1")];
        var model = CreateModel(agent.Name, agentStore, mailStore: mailStore);
        model.Load(TestContext.Current.CancellationToken);
        MoveCursorDown(model, 1);
        var expected = AgentPopoverView.BuildLines(agent, mail, [], [], s_now, 200, (AgentPopoverSection.Mail, false, 1));
        var expectedHighlightAnsi = RenderMarkupToAnsiText(expected.Lines[expected.SelectedLineIndex]);
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));

        // act
        var result = model.HandleKey(Key(ConsoleKey.Escape));
        var output = RenderToAnsiText(model);

        // assert
        Assert.Null(result);
        Assert.Contains(expectedHighlightAnsi, output);
    }

    [Fact]
    public void HandleKey_Should_ReturnToTheFullList_When_EscapeIsPressedInADetailOpenedFromTheShowMoreList()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var mailStore = new FakeMailStore { ParticipationRows = [CreateMailSummary(0), CreateMailSummary(1)] };
        mailStore.Threads["t0"] = [CreateMailMessage("t0", "m1", "felix", "Subject 0", "Body text 0")];
        var model = CreateModel(agent.Name, agentStore, mailStore: mailStore);
        model.Load(TestContext.Current.CancellationToken);
        MoveCursorDown(model, 2);
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));

        // act
        var result = model.HandleKey(Key(ConsoleKey.Escape));
        var text = RenderToText(model);

        // assert
        Assert.Null(result);
        Assert.Contains("Mail (2)", text);
    }

    [Fact]
    public void HandleKey_Should_ReturnCopyItemRequestedWithTheThreadId_When_YIsPressedInAMailDetail()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var mailStore = new FakeMailStore { ParticipationRows = [CreateMailSummary(0)] };
        mailStore.Threads["t0"] = [CreateMailMessage("t0", "m1", "felix", "Subject 0", "Body text 0")];
        var model = CreateModel(agent.Name, agentStore, mailStore: mailStore);
        model.Load(TestContext.Current.CancellationToken);
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));

        // act
        var result = model.HandleKey(Key(ConsoleKey.Y, 'y'));

        // assert
        var request = Assert.IsType<PopoverResult.Request>(result);
        var copy = Assert.IsType<AgentPopoverRequest.CopyItemRequested>(request.Payload);
        Assert.Equal("t0", copy.Id);
    }

    [Fact]
    public void HandleKey_Should_ReturnCopyItemRequestedWithTheTaskId_When_YIsPressedInATicketDetail()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var task = TaskItemBuilder.Create("a1", "Fix the thing");
        var taskStore = new FakeTaskStore { ParticipationRows = [task] };
        taskStore.Tasks[task.Id] = task;
        var model = CreateModel(agent.Name, agentStore, taskStore: taskStore);
        model.Load(TestContext.Current.CancellationToken);
        MoveCursorDown(model, 1);
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));

        // act
        var result = model.HandleKey(Key(ConsoleKey.Y, 'y'));

        // assert
        var request = Assert.IsType<PopoverResult.Request>(result);
        var copy = Assert.IsType<AgentPopoverRequest.CopyItemRequested>(request.Payload);
        Assert.Equal("a1", copy.Id);
    }

    [Fact]
    public void HandleKey_Should_ScrollTheDetailBody_When_JIsPressedInAMailDetail()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var mailStore = new FakeMailStore { ParticipationRows = [CreateMailSummary(0)] };
        mailStore.Threads["t0"] =
        [
            CreateMailMessage("t0", "m1", "felix", "Subject 0", string.Join('\n', Enumerable.Range(0, 40).Select(i => $"Line {i}")))
        ];
        var model = CreateModel(agent.Name, agentStore, mailStore: mailStore);
        model.Load(TestContext.Current.CancellationToken);
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));
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
    public void Hints_Should_ReturnDetailHints_When_ANestedDetailIsOpen()
    {
        // arrange
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        var agent = AddOnlineAgent(agentStore, "s-a");
        var mailStore = new FakeMailStore { ParticipationRows = [CreateMailSummary(0)] };
        mailStore.Threads["t0"] = [CreateMailMessage("t0", "m1", "felix", "Subject 0", "Body text 0")];
        var model = CreateModel(agent.Name, agentStore, mailStore: mailStore);
        model.Load(TestContext.Current.CancellationToken);

        // act
        model.HandleKey(Key(ConsoleKey.Enter, '\r'));

        // assert
        Assert.Equal(AgentPopoverModel.DetailHints, model.Hints);
    }

    [Fact]
    public void SummaryHints_Should_IncludeEnterOpen_When_Inspected()
    {
        // arrange
        // act
        var hints = AgentPopoverModel.SummaryHints;

        // assert
        Assert.Contains(new KeyHint("enter", "open"), hints);
    }

    [Fact]
    public void ListHints_Should_IncludeEnterOpen_When_Inspected()
    {
        // arrange
        // act
        var hints = AgentPopoverModel.ListHints;

        // assert
        Assert.Contains(new KeyHint("enter", "open"), hints);
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
        model.Load(TestContext.Current.CancellationToken);

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
        var agentStore = new FakeAgentStore(time);
        var agent = AddOnlineAgent(agentStore, "s-a");
        var model = CreateModel(agent.Name, agentStore, timeProvider: time);
        model.Load(TestContext.Current.CancellationToken);

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
        model.Load(TestContext.Current.CancellationToken);

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
        model.Load(TestContext.Current.CancellationToken);

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
        model.Load(TestContext.Current.CancellationToken);
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

    private static MailMessage CreateMailMessage(string threadId, string id, string sender, string subject, string body) => new()
    {
        Id = id,
        ThreadId = threadId,
        Sender = sender,
        Subject = subject,
        Body = body,
        CreatedAt = s_now
    };

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
