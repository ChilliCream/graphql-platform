using ChilliCream.Nitro.CommandLine.Tui.Agents;
using Microsoft.Extensions.Time.Testing;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

public sealed class AgentDetailViewTests
{
    [Fact]
    public async Task Render_Should_RenderTruncationMarker_When_ContentInteriorIsOneColumn()
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var taskStore = new FakeTaskStore();
        taskStore.Tasks.Add(TaskItemBuilder.Create("t-1", assignee: "agent-a"));
        var mailStore = new FakeMailStore();
        mailStore.Messages.Add(MailMessageBuilder.Create("m-1", sender: "agent-a", createdAt: now));
        var model = new AgentDetailModel(taskStore, mailStore);
        await model.LoadAsync(
            AgentSessionParticipantBuilder.Participant(agentName: "agent-a"),
            CancellationToken.None);
        var view = new AgentDetailView(model, new FakeTimeProvider(now));
        var console = new TestConsole().Width(5).Height(1_000);

        // act
        console.Write(view.Render(width: 5, height: 1_000, focused: true));

        // assert
        Assert.Equal(2, console.Output.Count(c => c == '…'));
    }
}
