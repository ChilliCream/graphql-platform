using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Agents;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

public sealed class AgentRowBadgeTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Render_Should_FitNarrowWidth_When_SelectedOrImplicit(bool selected, bool implicitIdentity)
    {
        // arrange
        const int maxWidth = 11;
        var agent = implicitIdentity
            ? new AgentRecord
            {
                Name = "agent-a",
                Role = "",
                Client = "",
                Implicit = true,
                RegisteredAt = s_now,
                LastSeenAt = s_now
            }
            : null;
        var row = new AgentParticipantRow(
            AgentSessionParticipantBuilder.Participant(agentName: "agent-a", role: "漢⌚❤️1️⃣", agent: agent),
            Activity: null);
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = AgentRowBadge.Render(row, s_now, selected, maxWidth, widths);

        // assert
        Assert.True(Markup.Remove(line).GetCellWidth() <= maxWidth);
    }

    [Fact]
    public void Render_Should_ReturnEmpty_When_MaxWidthIsZero()
    {
        // arrange
        var row = new AgentParticipantRow(AgentSessionParticipantBuilder.Participant(agentName: "agent-a"), Activity: null);
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = AgentRowBadge.Render(row, s_now, selected: false, maxWidth: 0, widths);

        // assert
        Assert.Empty(line);
    }

    [Fact]
    public void Render_Should_KeepEveryColumn_When_MaxWidthFits()
    {
        // arrange
        const int maxWidth = 120;
        var row = new AgentParticipantRow(
            AgentSessionParticipantBuilder.Participant(agentName: "agent-a", role: "developer"),
            Activity: null);
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(AgentRowBadge.Render(row, s_now, selected: false, maxWidth, widths));

        // assert
        Assert.Equal("    agent-a ● claude-code developer started now heard now", line);
    }
}
