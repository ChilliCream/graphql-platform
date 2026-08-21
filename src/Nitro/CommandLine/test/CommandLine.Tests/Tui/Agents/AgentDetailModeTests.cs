using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Agents;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using Spectre.Console.Testing;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

public sealed class AgentDetailModeTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static AgentRecord Agent(string name, string role = "")
        => new()
        {
            Name = name,
            Role = role,
            Implicit = false,
            RegisteredAt = Now,
            LastSeenAt = Now
        };

    private static string RenderToText(AgentDetailMode mode, int width = 80, int height = 24)
    {
        var console = new TestConsole().Width(width);
        console.Write(mode.Render(width, height));
        return console.Output;
    }

    private static AgentDetailMode CreateMode(
        FakeAgentRegistry registry, FakeTaskStore taskStore, FakeMailStore mailStore)
        => new(registry, taskStore, mailStore);

    [Fact]
    public void OpenOnAgent_Should_LoadAgentThroughRegistry_And_RenderIdentity()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a", role: "backend"));
        var mode = CreateMode(registry, new FakeTaskStore(), new FakeMailStore());

        // act
        mode.OpenOnAgent("agent-a");

        // assert
        var text = RenderToText(mode);
        Assert.Contains("agent-a", text);
        Assert.Contains("backend", text);
    }

    [Fact]
    public void OpenOnAgent_Should_RenderNotFoundMessage_When_AgentDoesNotExist()
    {
        // arrange
        var mode = CreateMode(new FakeAgentRegistry(), new FakeTaskStore(), new FakeMailStore());

        // act
        mode.OpenOnAgent("missing");

        // assert
        Assert.Contains("not found", RenderToText(mode));
    }

    [Fact]
    public void OpenOnAgent_Should_RenderAssignedTasks()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        var taskStore = new FakeTaskStore();
        taskStore.Tasks.Add(TaskItemBuilder.Create("a-1", assignee: "agent-a"));
        var mode = CreateMode(registry, taskStore, new FakeMailStore());

        // act
        mode.OpenOnAgent("agent-a");

        // assert
        var text = RenderToText(mode);
        Assert.Contains("Tasks", text);
        Assert.Contains("a-1", text);
    }

    [Fact]
    public void OpenOnAgent_Should_RenderSentMail()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        var mailStore = new FakeMailStore();
        mailStore.Messages.Add(MailMessageBuilder.Create("m-1", sender: "agent-a", subject: "Status update"));
        var mode = CreateMode(registry, new FakeTaskStore(), mailStore);

        // act
        mode.OpenOnAgent("agent-a");

        // assert
        var text = RenderToText(mode);
        Assert.Contains("Sent mail", text);
        Assert.Contains("Status update", text);
    }

    [Fact]
    public void OpenOnAgent_Should_ReplaceWhicheverAgentWasPreviouslyLoaded()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        registry.Agents.Add(Agent("agent-b"));
        var mode = CreateMode(registry, new FakeTaskStore(), new FakeMailStore());
        mode.OpenOnAgent("agent-a");

        // act
        mode.OpenOnAgent("agent-b");

        // assert
        var text = RenderToText(mode);
        Assert.Contains("agent-b", text);
        Assert.DoesNotContain("agent-a", text);
    }

    [Fact]
    public void Handle_RefreshRequested_Should_ReloadCurrentAgentThroughStores()
    {
        // arrange: the task is added to the store only after the initial
        // load, so it only shows up once RefreshRequested re-fetches.
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        var taskStore = new FakeTaskStore();
        var mode = CreateMode(registry, taskStore, new FakeMailStore());
        mode.OpenOnAgent("agent-a");
        Assert.DoesNotContain("a-1", RenderToText(mode));
        taskStore.Tasks.Add(TaskItemBuilder.Create("a-1", assignee: "agent-a"));

        // act
        var messages = mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Empty(messages);
        Assert.Contains("a-1", RenderToText(mode));
    }

    [Fact]
    public void Handle_RefreshRequested_Should_BeNoOp_When_NoAgentOpened()
    {
        // arrange
        var mode = CreateMode(new FakeAgentRegistry(), new FakeTaskStore(), new FakeMailStore());

        // act
        var exception = Record.Exception(() => mode.Handle(new TuiMessage.RefreshRequested()));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Handle_MoveCursorDown_Should_NotThrow_When_AgentOpened()
        => AssertHandleDoesNotThrow(new TuiMessage.MoveCursor(CursorDirection.Down));

    [Fact]
    public void Handle_MoveCursorUp_Should_NotThrow_When_AgentOpened()
        => AssertHandleDoesNotThrow(new TuiMessage.MoveCursor(CursorDirection.Up));

    [Fact]
    public void Handle_MoveToEdgeTop_Should_NotThrow_When_AgentOpened()
        => AssertHandleDoesNotThrow(new TuiMessage.MoveToEdge(EdgeTarget.Top));

    [Fact]
    public void Handle_MoveToEdgeBottom_Should_NotThrow_When_AgentOpened()
        => AssertHandleDoesNotThrow(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));

    private static void AssertHandleDoesNotThrow(TuiMessage message)
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        var mode = CreateMode(registry, new FakeTaskStore(), new FakeMailStore());
        mode.OpenOnAgent("agent-a");
        RenderToText(mode, height: 5);

        // act
        var exception = Record.Exception(() => mode.Handle(message));

        // assert
        Assert.Null(exception);
    }
}
