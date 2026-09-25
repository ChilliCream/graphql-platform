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
        Assert.Equal("  ● agent-a         implementer     Claude Code     just now      just now  ", line);
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
        Assert.Equal("  ● agent-a         -               Claude Code     just now      just now  ", line);
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
        Assert.Equal("  ● agent-a         -               -               just now      just now  ", line);
    }

    [Fact]
    public void Render_Should_DropStartedColumn_When_WidthIsTooNarrowForAllColumns()
    {
        // arrange
        const int maxWidth = 65;
        var row = CreateRow(role: "implementer");
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(AgentRowBadge.Render(row, s_now, selected: false, maxWidth, widths));

        // assert
        Assert.Equal("  ● agent-a         implementer     Claude Code     just now  ", line);
    }

    [Fact]
    public void Render_Should_DropHarnessColumn_When_WidthIsTooNarrowForHarnessAndStarted()
    {
        // arrange
        const int maxWidth = 50;
        var row = CreateRow(role: "implementer");
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(AgentRowBadge.Render(row, s_now, selected: false, maxWidth, widths));

        // assert
        Assert.Equal("  ● agent-a         implementer     just now  ", line);
    }

    [Fact]
    public void Render_Should_TruncateRole_When_WidthIsTooNarrowForNameAndRole()
    {
        // arrange
        const int maxWidth = 30;
        var row = CreateRow(name: "a-long-agent-name", role: "implementer");
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(AgentRowBadge.Render(row, s_now, selected: false, maxWidth, widths));

        // assert
        Assert.Equal("  ● a-long-agent-name    impl…", line);
    }

    [Fact]
    public void Render_Should_ShowNameAndRoleOnly_When_WidthIsTooNarrowForLastSeen()
    {
        // arrange
        const int maxWidth = 35;
        var row = CreateRow(name: "agent-a", role: "implementer");
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(AgentRowBadge.Render(row, s_now, selected: false, maxWidth, widths));

        // assert
        Assert.Equal("  ● agent-a         implementer ", line);
    }

    [Fact]
    public void RenderHeader_Should_ShowNameAndRoleOnly_When_WidthIsTooNarrowForLastSeen()
    {
        // arrange
        const int maxWidth = 35;
        var row = CreateRow(name: "agent-a", role: "implementer");
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var header = Markup.Remove(AgentRowBadge.RenderHeader(maxWidth, widths));

        // assert
        Assert.Equal("    NAME            ROLE        ", header);
    }

    [Theory]
    [InlineData(0, "just now")]
    [InlineData(90, "1m ago")]
    public void Render_Should_FormatLastSeen_When_AgeIsFreshOrMinutesOld(int elapsedSeconds, string expectedAge)
    {
        // arrange
        var row = CreateRow(lastSeenAt: s_now.AddSeconds(-elapsedSeconds));
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(AgentRowBadge.Render(row, s_now, selected: false, maxWidth: 80, widths));

        // assert
        // The Last Seen column is padded to the "LAST SEEN" header's width, so a shorter age has trailing fill spaces.
        Assert.EndsWith(expectedAge, line.TrimEnd());
    }

    [Fact]
    public void Render_Should_FormatLastSeenAsBareDate_When_AgeIsAtLeastAWeekOld()
    {
        // arrange
        var lastSeenAt = s_now.AddDays(-8);
        var row = CreateRow(lastSeenAt: lastSeenAt);
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(AgentRowBadge.Render(row, s_now, selected: false, maxWidth: 80, widths));

        // assert
        Assert.EndsWith(lastSeenAt.ToUniversalTime().ToString("yyyy-MM-dd"), line.TrimEnd());
    }

    [Fact]
    public void RenderHeader_Should_AlignColumnsWithRow_When_WidthIsWide()
    {
        // arrange
        const int maxWidth = 80;
        var row = CreateRow(role: "implementer");
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var header = Markup.Remove(AgentRowBadge.RenderHeader(maxWidth, widths));
        var line = Markup.Remove(AgentRowBadge.Render(row, s_now, selected: false, maxWidth, widths));
        var nameOffset = header.IndexOf("NAME", StringComparison.Ordinal);
        var roleOffset = header.IndexOf("ROLE", StringComparison.Ordinal);
        var harnessOffset = header.IndexOf("HARNESS", StringComparison.Ordinal);
        var startedOffset = header.IndexOf("STARTED", StringComparison.Ordinal);
        var lastSeenOffset = header.IndexOf("LAST SEEN", StringComparison.Ordinal);
        var columnsAtHeaderOffsets = (
            Name: line.Substring(nameOffset, 7),
            Role: line.Substring(roleOffset, 11),
            Harness: line.Substring(harnessOffset, 11),
            Started: line.Substring(startedOffset, 8),
            LastSeen: line.Substring(lastSeenOffset, 8));

        // assert
        Assert.Equal(
            (Name: "agent-a", Role: "implementer", Harness: "Claude Code", Started: "just now", LastSeen: "just now"),
            columnsAtHeaderOffsets);
    }

    [Fact]
    public void RenderHeader_Should_DropTheSameColumnsAsRow_When_WidthIsNarrow()
    {
        // arrange
        const int maxWidth = 50;
        var row = CreateRow(role: "implementer");
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var header = Markup.Remove(AgentRowBadge.RenderHeader(maxWidth, widths));
        var line = Markup.Remove(AgentRowBadge.Render(row, s_now, selected: false, maxWidth, widths));
        var actual = (
            HeaderHasRole: header.Contains("ROLE", StringComparison.Ordinal),
            RowHasRole: line.Contains("implementer", StringComparison.Ordinal),
            HeaderHasStarted: header.Contains("STARTED", StringComparison.Ordinal),
            HeaderHasHarness: header.Contains("HARNESS", StringComparison.Ordinal),
            RowHasHarness: line.Contains("Claude Code", StringComparison.Ordinal));

        // assert
        Assert.Equal((true, true, false, false, false), actual);
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
