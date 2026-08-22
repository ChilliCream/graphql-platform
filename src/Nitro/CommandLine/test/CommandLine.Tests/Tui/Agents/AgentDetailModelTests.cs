using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Agents;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

public sealed class AgentDetailModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static AgentRecord Agent(string name, string role = "")
        => new()
        {
            Name = name,
            Role = role,
            Client = "",
            Implicit = false,
            RegisteredAt = Now,
            LastSeenAt = Now
        };

    private static AgentDetailModel CreateModel(
        FakeAgentRegistry registry, FakeTaskStore taskStore, FakeMailStore mailStore)
        => new(registry, taskStore, mailStore);

    [Fact]
    public async Task LoadAsync_Should_LoadAgentIdentity_ThroughRegistry()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a", role: "backend"));
        var model = CreateModel(registry, new FakeTaskStore(), new FakeMailStore());

        // act
        await model.LoadAsync("agent-a", CancellationToken.None);

        // assert
        Assert.Equal("agent-a", model.CurrentAgentName);
        Assert.NotNull(model.Agent);
        Assert.Equal("backend", model.Agent!.Role);
    }

    [Fact]
    public async Task LoadAsync_Should_SetAgentNull_When_AgentNotRegistered()
    {
        // arrange
        var model = CreateModel(new FakeAgentRegistry(), new FakeTaskStore(), new FakeMailStore());

        // act
        await model.LoadAsync("missing", CancellationToken.None);

        // assert
        Assert.Equal("missing", model.CurrentAgentName);
        Assert.Null(model.Agent);
    }

    [Fact]
    public async Task LoadAsync_Should_OrderTasks_InProgressFirstThenOpenThenOthers()
    {
        // arrange
        var taskStore = new FakeTaskStore();
        taskStore.Tasks.Add(TaskItemBuilder.Create("a-open", status: TaskStates.Open, assignee: "agent-a"));
        taskStore.Tasks.Add(TaskItemBuilder.Create("a-deferred", status: TaskStates.Deferred, assignee: "agent-a"));
        taskStore.Tasks.Add(TaskItemBuilder.Create("a-progress", status: TaskStates.InProgress, assignee: "agent-a"));
        var model = CreateModel(new FakeAgentRegistry(), taskStore, new FakeMailStore());

        // act
        await model.LoadAsync("agent-a", CancellationToken.None);

        // assert
        Assert.Equal(["a-progress", "a-open", "a-deferred"], model.Tasks.Select(t => t.Id));
    }

    [Fact]
    public async Task LoadAsync_Should_OnlyIncludeTasksAssignedToTheAgent()
    {
        // arrange
        var taskStore = new FakeTaskStore();
        taskStore.Tasks.Add(TaskItemBuilder.Create("a-1", assignee: "agent-a"));
        taskStore.Tasks.Add(TaskItemBuilder.Create("b-1", assignee: "agent-b"));
        taskStore.Tasks.Add(TaskItemBuilder.Create("unassigned"));
        var model = CreateModel(new FakeAgentRegistry(), taskStore, new FakeMailStore());

        // act
        await model.LoadAsync("agent-a", CancellationToken.None);

        // assert
        Assert.Equal(["a-1"], model.Tasks.Select(t => t.Id));
    }

    [Fact]
    public async Task LoadAsync_Should_ExcludeClosedAndTombstoneTasks_ByDefault()
    {
        // arrange
        var taskStore = new FakeTaskStore();
        taskStore.Tasks.Add(TaskItemBuilder.Create("a-open", status: TaskStates.Open, assignee: "agent-a"));
        taskStore.Tasks.Add(TaskItemBuilder.Create("a-closed", status: TaskStates.Closed, assignee: "agent-a"));
        taskStore.Tasks.Add(TaskItemBuilder.Create("a-tombstone", status: TaskStates.Tombstone, assignee: "agent-a"));
        var model = CreateModel(new FakeAgentRegistry(), taskStore, new FakeMailStore());

        // act
        await model.LoadAsync("agent-a", CancellationToken.None);

        // assert
        Assert.Equal(["a-open"], model.Tasks.Select(t => t.Id));
    }

    [Fact]
    public async Task LoadAsync_Should_LoadSentMail_ThroughMailStore()
    {
        // arrange
        var mailStore = new FakeMailStore();
        mailStore.Messages.Add(MailMessageBuilder.Create("m-1", sender: "agent-a"));
        mailStore.Messages.Add(MailMessageBuilder.Create("m-2", sender: "agent-b"));
        var model = CreateModel(new FakeAgentRegistry(), new FakeTaskStore(), mailStore);

        // act
        await model.LoadAsync("agent-a", CancellationToken.None);

        // assert
        Assert.Equal(["m-1"], model.SentMail.Select(m => m.Id));
    }

    [Fact]
    public async Task LoadAsync_Should_CapSentMail_AtTwenty()
    {
        // arrange
        var mailStore = new FakeMailStore();

        for (var i = 0; i < 25; i++)
        {
            mailStore.Messages.Add(MailMessageBuilder.Create(
                $"m-{i:D2}", sender: "agent-a", createdAt: Now.AddMinutes(i)));
        }

        var model = CreateModel(new FakeAgentRegistry(), new FakeTaskStore(), mailStore);

        // act
        await model.LoadAsync("agent-a", CancellationToken.None);

        // assert
        Assert.Equal(20, model.SentMail.Count);
    }

    [Fact]
    public async Task LoadAsync_Should_ReplaceWhicheverAgentWasPreviouslyLoaded()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        registry.Agents.Add(Agent("agent-b"));
        var model = CreateModel(registry, new FakeTaskStore(), new FakeMailStore());
        await model.LoadAsync("agent-a", CancellationToken.None);

        // act
        await model.LoadAsync("agent-b", CancellationToken.None);

        // assert
        Assert.Equal("agent-b", model.CurrentAgentName);
        Assert.Equal("agent-b", model.Agent?.Name);
    }
}
