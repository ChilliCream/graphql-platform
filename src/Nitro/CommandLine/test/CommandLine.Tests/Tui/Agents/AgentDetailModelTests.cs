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

    private static AgentDetailModel CreateModel(FakeTaskStore taskStore, FakeMailStore mailStore)
        => new(taskStore, mailStore);

    [Fact]
    public async Task LoadAsync_Should_LoadBoundIdentity_FromTheParticipant()
    {
        // arrange: the durable identity already travels with the
        // participant (IAgentSessionRegistry.ListParticipantsAsync joins
        // it), so the model never queries a separate registry for it.
        var participant = AgentSessionParticipantBuilder.Participant(
            sessionId: "s-a", agentName: "agent-a", agent: Agent("agent-a", role: "backend"));
        var model = CreateModel(new FakeTaskStore(), new FakeMailStore());

        // act
        await model.LoadAsync(participant, CancellationToken.None);

        // assert
        Assert.Equal(new AgentSessionKey("claude-code", "s-a"), model.CurrentKey);
        Assert.NotNull(model.Participant);
        Assert.NotNull(model.Participant!.Agent);
        Assert.Equal("backend", model.Participant!.Agent!.Role);
    }

    [Fact]
    public async Task LoadAsync_Should_LeaveAgentNull_When_TheSessionIsUnbound()
    {
        // arrange
        var participant = AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: null);
        var model = CreateModel(new FakeTaskStore(), new FakeMailStore());

        // act
        await model.LoadAsync(participant, CancellationToken.None);

        // assert
        Assert.Equal(new AgentSessionKey("claude-code", "s-a"), model.CurrentKey);
        Assert.NotNull(model.Participant);
        Assert.Null(model.Participant!.Agent);
    }

    [Fact]
    public async Task LoadAsync_Should_OrderTasks_InProgressFirstThenOpenThenOthers()
    {
        // arrange
        var taskStore = new FakeTaskStore();
        taskStore.Tasks.Add(TaskItemBuilder.Create("a-open", status: TaskStates.Open, assignee: "agent-a"));
        taskStore.Tasks.Add(TaskItemBuilder.Create("a-deferred", status: TaskStates.Deferred, assignee: "agent-a"));
        taskStore.Tasks.Add(TaskItemBuilder.Create("a-progress", status: TaskStates.InProgress, assignee: "agent-a"));
        var participant = AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a");
        var model = CreateModel(taskStore, new FakeMailStore());

        // act
        await model.LoadAsync(participant, CancellationToken.None);

        // assert
        Assert.Equal(["a-progress", "a-open", "a-deferred"], model.Tasks.Select(t => t.Id));
    }

    [Fact]
    public async Task LoadAsync_Should_OnlyIncludeTasksAssignedToTheBoundActor()
    {
        // arrange
        var taskStore = new FakeTaskStore();
        taskStore.Tasks.Add(TaskItemBuilder.Create("a-1", assignee: "agent-a"));
        taskStore.Tasks.Add(TaskItemBuilder.Create("b-1", assignee: "agent-b"));
        taskStore.Tasks.Add(TaskItemBuilder.Create("unassigned"));
        var participant = AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a");
        var model = CreateModel(taskStore, new FakeMailStore());

        // act
        await model.LoadAsync(participant, CancellationToken.None);

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
        var participant = AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a");
        var model = CreateModel(taskStore, new FakeMailStore());

        // act
        await model.LoadAsync(participant, CancellationToken.None);

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
        var participant = AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a");
        var model = CreateModel(new FakeTaskStore(), mailStore);

        // act
        await model.LoadAsync(participant, CancellationToken.None);

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

        var participant = AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a");
        var model = CreateModel(new FakeTaskStore(), mailStore);

        // act
        await model.LoadAsync(participant, CancellationToken.None);

        // assert
        Assert.Equal(20, model.SentMail.Count);
    }

    [Fact]
    public async Task LoadAsync_Should_LoadNoTasksOrMail_When_TheSessionIsUnbound()
    {
        // arrange: an unbound session has no actor to query tasks or mail
        // by, and the model must not invent history for it.
        var taskStore = new FakeTaskStore();
        taskStore.Tasks.Add(TaskItemBuilder.Create("a-1", assignee: "agent-a"));
        var mailStore = new FakeMailStore();
        mailStore.Messages.Add(MailMessageBuilder.Create("m-1", sender: "agent-a"));
        var participant = AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: null);
        var model = CreateModel(taskStore, mailStore);

        // act
        await model.LoadAsync(participant, CancellationToken.None);

        // assert
        Assert.Empty(model.Tasks);
        Assert.Empty(model.SentMail);
    }

    [Fact]
    public async Task LoadAsync_Should_ReplaceWhicheverParticipantWasPreviouslyLoaded()
    {
        // arrange
        var participantA = AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a");
        var participantB = AgentSessionParticipantBuilder.Participant(sessionId: "s-b", agentName: "agent-b");
        var model = CreateModel(new FakeTaskStore(), new FakeMailStore());
        await model.LoadAsync(participantA, CancellationToken.None);

        // act
        await model.LoadAsync(participantB, CancellationToken.None);

        // assert
        Assert.Equal(new AgentSessionKey("claude-code", "s-b"), model.CurrentKey);
        Assert.Equal("agent-b", model.Participant?.Session.AgentName);
    }

    [Fact]
    public void Clear_Should_ResetToUnloadedState()
    {
        // arrange
        var model = CreateModel(new FakeTaskStore(), new FakeMailStore());

        // act
        model.Clear();

        // assert
        Assert.Null(model.CurrentKey);
        Assert.Null(model.Participant);
        Assert.Empty(model.Tasks);
        Assert.Empty(model.SentMail);
    }
}
