using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

/// <summary>
/// An in-memory <see cref="IAgentStore"/> backed by a plain list of rows, reproducing the
/// login, session, touch, role, and lookup contracts for tests that do not need a real
/// SQLite workspace.
/// </summary>
internal sealed class FakeAgentStore(TimeProvider timeProvider) : IAgentStore
{
    private readonly List<AgentRow> _rows = [];

    public IReadOnlyList<AgentRow> Rows => _rows;

    /// <summary>
    /// Adds a row directly, bypassing the allocated-name contract, for tests that need
    /// a deterministic agent name.
    /// </summary>
    public void Seed(AgentRow row) => _rows.Add(row);

    public Task<AgentRow> LoginAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var row = new AgentRow
        {
            Name = AllocateName(),
            Role = string.Empty,
            Harness = null,
            HarnessVersion = string.Empty,
            SessionId = null,
            Cwd = string.Empty,
            WorkspacePath = string.Empty,
            RegisteredAt = now,
            StartedAt = now,
            LastSeenAt = now,
            EndpointKind = AgentSessionEndpointKind.None,
            EndpointAddr = string.Empty,
            BlockBudgetUsed = 0,
            AnnouncementPending = false,
            IdlePushArmed = false
        };

        _rows.Add(row);

        return Task.FromResult(row);
    }

    public Task<AgentSessionStartResult> StartSessionAsync(
        AgentSessionStartRequest request, CancellationToken cancellationToken)
    {
        EnsureAgentHarness(request.Harness);

        var now = timeProvider.GetUtcNow();
        var index = _rows.FindIndex(r => r.Harness == request.Harness && r.SessionId == request.SessionId);

        if (index >= 0)
        {
            var existing = _rows[index];

            if (existing.IsDeleted)
            {
                return Task.FromResult(AgentSessionStartResult.Ignored);
            }

            var reused = existing with
            {
                EndedAt = null,
                HarnessVersion = request.HarnessVersion.Length == 0
                    ? existing.HarnessVersion
                    : request.HarnessVersion,
                Cwd = request.Cwd,
                WorkspacePath = request.WorkspacePath,
                EndpointKind = request.EndpointKind,
                EndpointAddr = request.EndpointAddr,
                EndpointSecret = request.EndpointSecret,
                LastSeenAt = now
            };

            _rows[index] = reused;

            return Task.FromResult(AgentSessionStartResult.Reused(reused));
        }

        var minted = new AgentRow
        {
            Name = AllocateName(),
            Role = string.Empty,
            Harness = request.Harness,
            HarnessVersion = request.HarnessVersion,
            SessionId = request.SessionId,
            Cwd = request.Cwd,
            WorkspacePath = request.WorkspacePath,
            RegisteredAt = now,
            StartedAt = now,
            LastSeenAt = now,
            EndpointKind = request.EndpointKind,
            EndpointAddr = request.EndpointAddr,
            EndpointSecret = request.EndpointSecret,
            BlockBudgetUsed = 0,
            AnnouncementPending = false,
            IdlePushArmed = false
        };

        _rows.Add(minted);

        return Task.FromResult(AgentSessionStartResult.Minted(minted));
    }

    public Task<bool> TouchSessionAsync(string harness, string sessionId, CancellationToken cancellationToken)
    {
        EnsureAgentHarness(harness);

        var index = _rows.FindIndex(r => r.Harness == harness && r.SessionId == sessionId && !r.IsDeleted);

        if (index < 0)
        {
            return Task.FromResult(false);
        }

        _rows[index] = _rows[index] with { LastSeenAt = timeProvider.GetUtcNow() };

        return Task.FromResult(true);
    }

    public Task<bool> TouchAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var index = _rows.FindIndex(r => r.Name == normalizedName && !r.IsDeleted);

        if (index < 0)
        {
            return Task.FromResult(false);
        }

        _rows[index] = _rows[index] with { LastSeenAt = timeProvider.GetUtcNow() };

        return Task.FromResult(true);
    }

    public Task<bool> EndSessionAsync(string harness, string sessionId, CancellationToken cancellationToken)
    {
        EnsureAgentHarness(harness);

        var index = _rows.FindIndex(r => r.Harness == harness && r.SessionId == sessionId && !r.IsDeleted);

        if (index < 0)
        {
            return Task.FromResult(false);
        }

        _rows[index] = _rows[index] with { EndedAt = timeProvider.GetUtcNow() };

        return Task.FromResult(true);
    }

    public Task<AgentRow?> SetRoleAsync(string name, string role, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var index = _rows.FindIndex(r => r.Name == normalizedName && !r.IsDeleted);

        if (index < 0)
        {
            return Task.FromResult<AgentRow?>(null);
        }

        var updated = _rows[index] with
        {
            Role = AgentRole.Normalize(role),
            LastSeenAt = timeProvider.GetUtcNow()
        };

        _rows[index] = updated;

        return Task.FromResult<AgentRow?>(updated);
    }

    public Task<AgentRow?> FindAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);

        return Task.FromResult(_rows.FirstOrDefault(r => r.Name == normalizedName));
    }

    public Task<AgentRow?> FindBySessionAsync(string harness, string sessionId, CancellationToken cancellationToken)
    {
        EnsureAgentHarness(harness);

        return Task.FromResult(_rows.FirstOrDefault(r => r.Harness == harness && r.SessionId == sessionId));
    }

    public Task<IReadOnlyList<AgentRow>> ListAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<AgentRow>>(_rows.Where(r => !r.IsDeleted).ToList());

    public Task<bool> SetEndpointAsync(
        string name,
        string endpointKind,
        string endpointAddr,
        string? endpointSecret,
        CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var index = _rows.FindIndex(r => r.Name == normalizedName && !r.IsDeleted);

        if (index < 0)
        {
            return Task.FromResult(false);
        }

        var (kind, addr, secret) = EndpointAddress.Normalize(
            _rows[index].Harness ?? string.Empty, endpointKind, endpointAddr, endpointSecret);

        _rows[index] = _rows[index] with
        {
            EndpointKind = kind,
            EndpointAddr = addr,
            EndpointSecret = secret
        };

        return Task.FromResult(true);
    }

    public Task<int> ResetBlockBudgetAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var index = _rows.FindIndex(r => r.Name == normalizedName && !r.IsDeleted);

        if (index >= 0)
        {
            _rows[index] = _rows[index] with { BlockBudgetUsed = 0 };
        }

        return Task.FromResult(0);
    }

    public Task<int> IncrementBlockBudgetAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var index = _rows.FindIndex(r => r.Name == normalizedName && !r.IsDeleted);

        if (index < 0)
        {
            return Task.FromResult(0);
        }

        var updated = _rows[index] with { BlockBudgetUsed = _rows[index].BlockBudgetUsed + 1 };
        _rows[index] = updated;

        return Task.FromResult(updated.BlockBudgetUsed);
    }

    public Task<bool> TryClaimPingCooldownAsync(
        string name, TimeSpan cooldown, string attemptId, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var index = _rows.FindIndex(r => r.Name == normalizedName && !r.IsDeleted);

        if (index < 0)
        {
            return Task.FromResult(false);
        }

        var now = timeProvider.GetUtcNow();
        var existing = _rows[index];

        if (existing.LastPingAt is { } lastPingAt && lastPingAt > now - cooldown)
        {
            return Task.FromResult(false);
        }

        _rows[index] = existing with
        {
            LastPingAt = now,
            LastPingAttempt = attemptId,
            LastPingResult = null,
            LastPingDetail = null
        };

        return Task.FromResult(true);
    }

    public Task WritePingResultAsync(
        string name, string attemptId, string result, string? detail, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var index = _rows.FindIndex(
            r => r.Name == normalizedName && !r.IsDeleted && r.LastPingAttempt == attemptId);

        if (index >= 0)
        {
            _rows[index] = _rows[index] with { LastPingResult = result, LastPingDetail = detail };
        }

        return Task.CompletedTask;
    }

    public Task ArmAnnouncementAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var index = _rows.FindIndex(r => r.Name == normalizedName && !r.IsDeleted);

        if (index >= 0)
        {
            _rows[index] = _rows[index] with { AnnouncementPending = true };
        }

        return Task.CompletedTask;
    }

    public Task<bool> ClaimAnnouncementAsync(string name, CancellationToken cancellationToken)
        => ClaimFlag(name, row => row.AnnouncementPending, (row, cleared) => row with { AnnouncementPending = cleared });

    public Task<bool> IsAnnouncementPendingAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var row = _rows.FirstOrDefault(r => r.Name == normalizedName && !r.IsDeleted);

        return Task.FromResult(row?.AnnouncementPending ?? false);
    }

    public Task RearmIdlePushAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var index = _rows.FindIndex(r => r.Name == normalizedName && !r.IsDeleted);

        if (index >= 0)
        {
            _rows[index] = _rows[index] with { IdlePushArmed = true };
        }

        return Task.CompletedTask;
    }

    public Task<bool> ClaimIdlePushAsync(string name, CancellationToken cancellationToken)
        => ClaimFlag(name, row => row.IdlePushArmed, (row, cleared) => row with { IdlePushArmed = cleared });

    public Task<bool> RecordHarnessVersionAsync(
        string name, string harnessVersion, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var index = _rows.FindIndex(r => r.Name == normalizedName && !r.IsDeleted);

        if (index < 0)
        {
            return Task.FromResult(false);
        }

        _rows[index] = _rows[index] with { HarnessVersion = harnessVersion };

        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var index = _rows.FindIndex(r => r.Name == normalizedName && !r.IsDeleted);

        if (index < 0)
        {
            return Task.FromResult(false);
        }

        _rows[index] = DeletedRow(_rows[index], timeProvider.GetUtcNow());

        return Task.FromResult(true);
    }

    public Task<int> DeleteOfflineAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var deletedCount = 0;

        for (var index = 0; index < _rows.Count; index++)
        {
            var row = _rows[index];

            if (row.IsDeleted || AgentStateResolver.Resolve(row, now) != AgentState.Offline)
            {
                continue;
            }

            _rows[index] = DeletedRow(row, now);
            deletedCount++;
        }

        return Task.FromResult(deletedCount);
    }

    private static AgentRow DeletedRow(AgentRow row, DateTimeOffset now) => row with
    {
        DeletedAt = now,
        EndpointKind = AgentSessionEndpointKind.None,
        EndpointAddr = string.Empty,
        EndpointSecret = null,
        LastPingAt = null,
        LastPingAttempt = null,
        LastPingResult = null,
        LastPingDetail = null,
        AnnouncementPending = false,
        IdlePushArmed = false,
        BlockBudgetUsed = 0
    };

    private Task<bool> ClaimFlag(
        string name, Func<AgentRow, bool> read, Func<AgentRow, bool, AgentRow> write)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var index = _rows.FindIndex(r => r.Name == normalizedName && !r.IsDeleted);

        if (index < 0 || !read(_rows[index]))
        {
            return Task.FromResult(false);
        }

        _rows[index] = write(_rows[index], false);

        return Task.FromResult(true);
    }

    private static void EnsureAgentHarness(string harness)
    {
        if (!AgentSessionHarness.IsAgentHarness(harness))
        {
            throw ThrowHelper.UnknownAgentHarness(harness);
        }
    }

    /// <summary>
    /// Picks the first unused name from <see cref="AgentNamePool"/>, then falls back to a
    /// numeric suffix once the pool is exhausted.
    /// </summary>
    private string AllocateName()
    {
        var occupied = _rows.Select(r => r.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var name in AgentNamePool.Names)
        {
            if (!occupied.Contains(name))
            {
                return name;
            }
        }

        for (var suffix = 2; ; suffix++)
        {
            foreach (var name in AgentNamePool.Names)
            {
                var candidate = $"{name}-{suffix}";

                if (!occupied.Contains(candidate))
                {
                    return candidate;
                }
            }
        }
    }
}
