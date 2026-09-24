using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Agents;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

public sealed class AgentRowBadgeTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static AgentRow CreateRow(
        string name = "agent-a",
        string role = "",
        string? harness = "claude-code",
        string? sessionId = "session-a",
        DateTimeOffset? startedAt = null,
        DateTimeOffset? lastSeenAt = null,
        string endpointKind = AgentSessionEndpointKind.ClaudePeer,
        DateTimeOffset? endedAt = null) => new()
        {
            Name = name,
            Role = role,
            Harness = harness,
            HarnessVersion = "",
            SessionId = sessionId,
            Cwd = "",
            WorkspacePath = "",
            RegisteredAt = s_now,
            StartedAt = startedAt ?? s_now,
            LastSeenAt = lastSeenAt ?? s_now,
            EndedAt = endedAt,
            EndpointKind = endpointKind,
            EndpointAddr = endpointKind == AgentSessionEndpointKind.None ? "" : "peer-1",
            BlockBudgetUsed = 0,
            AnnouncementPending = false,
            IdlePushArmed = false
        };

    [Fact]
    public void Render_Should_ReturnEmpty_When_MaxWidthIsZero()
    {
        // arrange
        var row = CreateRow();
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = AgentRowBadge.Render(row, s_now, selected: false, maxWidth: 0, widths);

        // assert
        Assert.Empty(line);
    }

    [Fact]
    public void Render_Should_ShowEveryColumn_When_MaxWidthFits()
    {
        // arrange
        const int maxWidth = 80;
        var row = CreateRow(role: "implementer");
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(AgentRowBadge.Render(row, s_now, selected: false, maxWidth, widths));

        // assert
        Assert.Equal("  ● agent-a implementer Claude Code now ago now ago", line);
    }

    [Fact]
    public void Render_Should_ShowDashForRole_When_RoleIsEmpty()
    {
        // arrange
        const int maxWidth = 80;
        var row = CreateRow(role: "");
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(AgentRowBadge.Render(row, s_now, selected: false, maxWidth, widths));

        // assert
        Assert.Equal("  ● agent-a - Claude Code now ago now ago", line);
    }

    [Fact]
    public void Render_Should_ShowDashForHarness_When_AgentIsLoginOnly()
    {
        // arrange
        const int maxWidth = 80;
        var row = CreateRow(harness: null, sessionId: null, endpointKind: AgentSessionEndpointKind.None);
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(AgentRowBadge.Render(row, s_now, selected: false, maxWidth, widths));

        // assert
        Assert.Equal("  ● agent-a - - now ago now ago", line);
    }

    [Fact]
    public void Render_Should_DropStartedColumn_When_WidthIsTooNarrowForAllColumns()
    {
        // arrange
        const int maxWidth = 45;
        var row = CreateRow(role: "implementer");
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(AgentRowBadge.Render(row, s_now, selected: false, maxWidth, widths));

        // assert
        Assert.Equal("  ● agent-a implementer Claude Code now ago", line);
    }

    [Fact]
    public void Render_Should_DropRoleColumn_When_WidthIsTooNarrowForRoleAndHarness()
    {
        // arrange
        const int maxWidth = 35;
        var row = CreateRow(role: "implementer");
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(AgentRowBadge.Render(row, s_now, selected: false, maxWidth, widths));

        // assert
        Assert.Equal("  ● agent-a Claude Code now ago", line);
    }

    [Fact]
    public void Render_Should_TruncateName_When_WidthIsTooNarrowForHarness()
    {
        // arrange
        const int maxWidth = 14;
        var row = CreateRow(name: "a-long-agent-name", role: "implementer");
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(AgentRowBadge.Render(row, s_now, selected: false, maxWidth, widths));

        // assert
        Assert.Contains("now ago", line);
        Assert.True(line.GetCellWidth() <= maxWidth);
    }

    [Fact]
    public void PresenceStyle_Should_ReturnDistinctStyles_When_StateDiffers()
    {
        // arrange
        const AgentState online = AgentState.Online;
        const AgentState unreachable = AgentState.Unreachable;
        const AgentState offline = AgentState.Offline;

        // act
        var onlineStyle = AgentRowBadge.PresenceStyle(online);
        var unreachableStyle = AgentRowBadge.PresenceStyle(unreachable);
        var offlineStyle = AgentRowBadge.PresenceStyle(offline);

        // assert
        Assert.NotEqual(onlineStyle, unreachableStyle);
        Assert.NotEqual(onlineStyle, offlineStyle);
        Assert.NotEqual(unreachableStyle, offlineStyle);
    }
}
