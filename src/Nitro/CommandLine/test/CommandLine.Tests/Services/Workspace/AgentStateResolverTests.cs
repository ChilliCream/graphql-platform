using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="AgentStateResolver"/>: the boundary of the online window, its idle
/// side, and the precedence of deleted, ended and unreachable rows over it.
/// </summary>
public sealed class AgentStateResolverTests
{
    private static readonly DateTimeOffset s_now =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static AgentRow CreateRow() => new()
    {
        Name = "maya",
        Role = AgentRole.Implementer,
        Harness = "claude-code",
        HarnessVersion = "1.0.0",
        SessionId = "session-1",
        Cwd = "/repo",
        WorkspacePath = "/repo",
        RegisteredAt = s_now,
        StartedAt = s_now,
        LastSeenAt = s_now,
        EndedAt = null,
        DeletedAt = null,
        EndpointKind = AgentSessionEndpointKind.ClaudePeer,
        EndpointAddr = "peer-1",
        EndpointSecret = null,
        BlockBudgetUsed = 0,
        AnnouncementPending = false,
        IdlePushArmed = false
    };

    [Fact]
    public void Resolve_Should_ReturnOnline_When_LastBeatIsWithinTheWindow()
    {
        // arrange
        var row = CreateRow() with { LastSeenAt = s_now - TimeSpan.FromMinutes(29).Add(TimeSpan.FromSeconds(59)) };

        // act
        var state = AgentStateResolver.Resolve(row, s_now);

        // assert
        Assert.Equal(AgentState.Online, state);
    }

    [Fact]
    public void Resolve_Should_ReturnIdle_When_LastBeatIsJustPastTheWindow()
    {
        // arrange
        var row = CreateRow() with { LastSeenAt = s_now - TimeSpan.FromMinutes(30).Add(TimeSpan.FromSeconds(1)) };

        // act
        var state = AgentStateResolver.Resolve(row, s_now);

        // assert
        Assert.Equal(AgentState.Idle, state);
    }

    [Fact]
    public void Resolve_Should_ReturnOffline_When_EndedAtIsSetWithAFreshBeat()
    {
        // arrange
        var row = CreateRow() with { EndedAt = s_now };

        // act
        var state = AgentStateResolver.Resolve(row, s_now);

        // assert
        Assert.Equal(AgentState.Offline, state);
    }

    [Fact]
    public void Resolve_Should_ReturnUnreachable_When_EndpointKindIsNoneWithAFreshBeat()
    {
        // arrange
        // Covers login-only agents, whose endpoint kind is always none.
        var row = CreateRow() with
        {
            Harness = null,
            SessionId = null,
            EndpointKind = AgentSessionEndpointKind.None,
            EndpointAddr = string.Empty
        };

        // act
        var state = AgentStateResolver.Resolve(row, s_now);

        // assert
        Assert.Equal(AgentState.Unreachable, state);
    }

    [Fact]
    public void Resolve_Should_ReturnUnreachable_When_EndpointKindIsNoneWithAStaleBeat()
    {
        // arrange
        // The endpoint-none check runs before the idle window check, so age never matters here.
        var row = CreateRow() with
        {
            Harness = null,
            SessionId = null,
            EndpointKind = AgentSessionEndpointKind.None,
            EndpointAddr = string.Empty,
            LastSeenAt = s_now - TimeSpan.FromDays(30)
        };

        // act
        var state = AgentStateResolver.Resolve(row, s_now);

        // assert
        Assert.Equal(AgentState.Unreachable, state);
    }

    [Fact]
    public void Resolve_Should_ReturnOffline_When_DeletedAtIsSetWithAFreshBeatAndAnEndpoint()
    {
        // arrange
        var row = CreateRow() with { DeletedAt = s_now };

        // act
        var state = AgentStateResolver.Resolve(row, s_now);

        // assert
        Assert.Equal(AgentState.Offline, state);
    }
}
