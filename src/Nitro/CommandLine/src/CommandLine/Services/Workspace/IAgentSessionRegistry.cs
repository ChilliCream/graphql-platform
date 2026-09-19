namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Manages session presence, actor bindings, heartbeats, and notification state
/// using harness session ids and Nitro instance ids.
/// </summary>
internal interface IAgentSessionRegistry
{
    /// <summary>
    /// Starts or refreshes presence; coding harnesses reuse a durable actor or create one
    /// from <paramref name="envActor"/> (allocating when null), while other harnesses use
    /// that value as an optional binding. A matching host preserves state except for
    /// heartbeat and actor reconciliation; a different host replaces presence and
    /// clears delivery reservations, block budget, and ping state.
    /// </summary>
    Task<AgentSessionRecord> StartAsync(
        AgentSessionGeneration generation,
        string cwd,
        string workspacePath,
        string endpointKind,
        string endpointAddr,
        string? envActor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Starts or refreshes a session with an optional endpoint credential.
    /// The default implementation ignores <paramref name="endpointSecret"/>.
    /// </summary>
    Task<AgentSessionRecord> StartAsync(
        AgentSessionGeneration generation,
        string cwd,
        string workspacePath,
        string endpointKind,
        string endpointAddr,
        string? endpointSecret,
        string? envActor,
        CancellationToken cancellationToken)
        => StartAsync(
            generation,
            cwd,
            workspacePath,
            endpointKind,
            endpointAddr,
            envActor,
            cancellationToken);

    /// <summary>
    /// Claims the matching session for the normalized actor. Throws <see cref="ExitException"/>
    /// when no session matches or changing a protected explicit binding requires
    /// <paramref name="forceRebind"/> and it is false.
    /// </summary>
    Task<AgentSessionClaimResult> ClaimAsync(
        AgentSessionGeneration generation,
        string actor,
        bool forceRebind,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the session matching <paramref name="generation"/> and returns whether
    /// a row was deleted; a missing or differently owned session is unchanged.
    /// </summary>
    Task<bool> EndAsync(AgentSessionGeneration generation, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the session matching the harness, session id, and host in
    /// <paramref name="generation"/>, or null when none matches.
    /// </summary>
    Task<AgentSessionRecord?> FindByGenerationAsync(
        AgentSessionGeneration generation, CancellationToken cancellationToken);

    /// <summary>
    /// Resets the matching session's block budget to zero; a missing session is unchanged.
    /// </summary>
    Task ResetBlockBudgetAsync(AgentSessionGeneration generation, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically increments <c>block_budget_used</c> by one for the row
    /// matching <paramref name="generation"/> exactly and returns the new
    /// value. Returns null when no row matches that generation.
    /// </summary>
    Task<int?> IncrementBlockBudgetAsync(AgentSessionGeneration generation, CancellationToken cancellationToken);

    /// <summary>
    /// Reaps current-instance sessions whose heartbeat has reached the stale cutoff
    /// and returns the deleted records. Sessions owned by another instance are retained.
    /// </summary>
    Task<IReadOnlyList<AgentSessionRecord>> ReapAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reaps stale current-instance rows, then returns every surviving row
    /// with its computed <see cref="AgentSessionState"/>.
    /// </summary>
    Task<IReadOnlyList<AgentSessionView>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Updates only the matching session's heartbeat to now.
    /// Returns false when no session matches.
    /// </summary>
    Task<bool> TouchAsync(AgentSessionGeneration generation, CancellationToken cancellationToken);

    /// <summary>
    /// Updates only the matching session's harness version.
    /// Returns false when no session matches.
    /// </summary>
    Task<bool> RecordHarnessVersionAsync(
        AgentSessionGeneration generation, string harnessVersion, CancellationToken cancellationToken);

    /// <summary>
    /// Updates only the matching session's normalized role, preserving the agent identity
    /// role. Returns false when no session matches.
    /// </summary>
    Task<bool> SetRoleAsync(AgentSessionGeneration generation, string role, CancellationToken cancellationToken);

    /// <summary>
    /// Reaps stale local sessions, then returns each surviving session with its agent
    /// identity when available and its computed presence state.
    /// </summary>
    Task<IReadOnlyList<AgentSessionParticipant>> ListParticipantsAsync(CancellationToken cancellationToken);

    async Task<IReadOnlyList<AgentSessionIdentityView>> ListIdentitiesAsync(CancellationToken cancellationToken)
    {
        var participants = await ListParticipantsAsync(cancellationToken);

        return participants
            .Where(participant => participant.Session.AgentName is not null)
            .Select(participant => new AgentSessionIdentityView(
                new AgentSessionIdentityRecord
                {
                    Harness = participant.Session.Harness,
                    SessionId = participant.Session.SessionId,
                    Actor = participant.Session.AgentName!,
                    Role = participant.Session.Role,
                    ActorRevision = 1,
                    CreatedAt = participant.Session.StartedAt.ToString("O"),
                    LastSeenAt = participant.Session.LastBeatAt.ToString("O")
                },
                participant))
            .ToList();
    }

    /// <summary>
    /// Atomically registers the known actor and normalized role and applies the session
    /// claim transition. Throws <see cref="ExitException"/> for an unknown actor, a missing
    /// session, or a protected explicit binding that cannot be changed without
    /// <paramref name="forceRebind"/>.
    /// </summary>
    Task<AgentSessionRegisterResult> RegisterAsync(
        AgentSessionGeneration generation,
        string actor,
        string role,
        string client,
        bool forceRebind,
        CancellationToken cancellationToken);

    async Task<AgentSessionRegisterResult> RegisterAsync(
        AgentSessionGeneration generation,
        string? actor,
        bool actorGiven,
        string? role,
        bool roleGiven,
        CancellationToken cancellationToken)
    {
        var current = await FindByGenerationAsync(generation, cancellationToken)
            ?? throw new ExitException("The current session is no longer connected.");

        return await RegisterAsync(
            generation,
            actorGiven
                ? actor ?? string.Empty
                : current.AgentName ?? throw new ExitException("The current session has no actor."),
            roleGiven ? role ?? string.Empty : current.Role,
            generation.Harness,
            forceRebind: true,
            cancellationToken);
    }

    async Task<AgentSessionRegisterResult> RegisterAsync(
        AgentSessionGeneration generation,
        string? actor,
        bool actorGiven,
        string? role,
        bool roleGiven,
        bool force,
        CancellationToken cancellationToken)
    {
        if (force)
        {
            throw new ExitException("This session registry does not support forced actor takeover.");
        }

        return await RegisterAsync(
            generation, actor, actorGiven, role, roleGiven, cancellationToken);
    }

    /// <summary>
    /// Returns the row matching <paramref name="harness"/>, <paramref
    /// name="host"/>, and <paramref name="sessionId"/> exactly. Null when no
    /// row matches.
    /// </summary>
    Task<AgentSessionRecord?> FindBySessionIdAsync(
        string harness, string host, string sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Reaps stale local sessions, then returns surviving current-instance sessions
    /// bound to <paramref name="agentName"/>.
    /// </summary>
    Task<IReadOnlyList<AgentSessionRecord>> FindLiveClaimedByAgentNameAsync(
        string agentName, CancellationToken cancellationToken);

    /// <summary>
    /// Claims the matching session's ping cooldown for <paramref name="attemptId"/>,
    /// clearing the previous result and detail. Returns false when no session matches
    /// or the previous attempt is newer than <paramref name="now"/> minus <paramref name="cooldown"/>.
    /// </summary>
    Task<bool> TryClaimPingCooldownAsync(
        AgentSessionRecord session,
        string attemptId,
        DateTimeOffset now,
        TimeSpan cooldown,
        CancellationToken cancellationToken);

    /// <summary>
    /// Records the ping result and detail only when the harness, session id, and
    /// current attempt id match; an outdated completion changes nothing.
    /// </summary>
    Task WritePingResultAsync(
        string harness,
        string sessionId,
        string attemptId,
        string result,
        string? detail,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks the matching session's actor announcement as pending.
    /// A missing session is unchanged.
    /// </summary>
    Task ArmAnnouncementAsync(AgentSessionGeneration generation, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically clears the matching session's pending announcement and returns true.
    /// Returns false when no session matches or no announcement is pending.
    /// </summary>
    Task<bool> ClaimAnnouncementAsync(AgentSessionGeneration generation, CancellationToken cancellationToken);

    /// <summary>
    /// Arms one idle push for the matching session. A missing session is unchanged.
    /// </summary>
    Task RearmIdlePushAsync(AgentSessionGeneration generation, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically consumes the matching session's armed idle push and returns true.
    /// Returns false when no session matches or no push is armed.
    /// </summary>
    Task<bool> ClaimIdlePushAsync(AgentSessionGeneration generation, CancellationToken cancellationToken);
}
