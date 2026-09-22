using System.Data.Common;
using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class AgentSessionRegistry(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    AgentDatabase database,
    IAgentRegistry agentRegistry,
    INitroInstanceIdProvider instanceIdProvider,
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider) : IAgentSessionRegistry
{
    /// <summary>
    /// The heartbeat age at which current-instance session presence becomes eligible for reaping.
    /// </summary>
    private static readonly TimeSpan s_staleAfter = TimeSpan.FromHours(24);

    /// <summary>
    /// Invoked after a stale reap candidate is read and before its guarded delete.
    /// </summary>
    internal Func<AgentSessionRecord, CancellationToken, Task>? OnStaleReapCandidateCapturedAsync { get; init; }

    public Task<AgentSessionRecord> StartAsync(
        AgentSessionGeneration generation,
        string cwd,
        string workspacePath,
        string endpointKind,
        string endpointAddr,
        string? envActor,
        CancellationToken cancellationToken)
        => StartAsync(
            generation,
            cwd,
            workspacePath,
            endpointKind,
            endpointAddr,
            endpointSecret: null,
            envActor: envActor,
            cancellationToken: cancellationToken);

    public async Task<AgentSessionRecord> StartAsync(
        AgentSessionGeneration generation,
        string cwd,
        string workspacePath,
        string endpointKind,
        string endpointAddr,
        string? endpointSecret,
        string? envActor,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var (normalizedEndpointKind, normalizedEndpointAddr, normalizedEndpointSecret) =
            NormalizeEndpoint(generation.Harness, endpointKind, endpointAddr, endpointSecret);

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction(deferred: false);

        string? boundAgentName;
        string bindingKind;
        string identityRole;

        if (generation.Harness is AgentSessionHarness.ClaudeCode
            or AgentSessionHarness.Codex
            or AgentSessionHarness.Copilot
            or AgentSessionHarness.Opencode)
        {
            var identity = await EnsureCodingIdentityWithinTransactionAsync(
                connection, transaction, generation, now, envActor, cancellationToken);
            boundAgentName = identity.Actor;
            bindingKind = envActor is not null
                && identity.Actor == MailAgentName.Normalize(envActor)
                    ? AgentSessionBindingKind.Env
                    : AgentSessionBindingKind.Explicit;
            identityRole = identity.Role;
        }
        else
        {
            boundAgentName = envActor is null ? null : MailAgentName.Normalize(envActor);
            bindingKind = boundAgentName is null ? AgentSessionBindingKind.None : AgentSessionBindingKind.Env;
            identityRole = string.Empty;

            if (boundAgentName is not null)
            {
                await EnsureImplicitActorWithinTransactionAsync(
                    connection, transaction, boundAgentName, now, cancellationToken);
            }
        }

        var existing = await connection.QueryFirstOrDefaultAsync<AgentSessionRow>(
            $"SELECT {AgentSessionRecord.Columns} FROM agent_sessions "
            + "WHERE harness = @harness AND session_id = @sessionId",
            new { harness = generation.Harness, sessionId = generation.SessionId, cancellationToken },
            transaction);

        if (existing is null)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO agent_sessions (
                    harness, session_id, agent_name, binding_kind, host,
                    cwd, workspace_path, endpoint_kind, endpoint_addr, endpoint_secret, started_at, last_beat_at,
                    block_budget_used, role
                ) VALUES (
                    @harness, @sessionId, @agentName, @bindingKind, @host,
                    @cwd, @workspacePath, @endpointKind, @endpointAddr, @endpointSecret, @now, @now, 0, @role
                );
                """,
                new
                {
                    harness = generation.Harness,
                    sessionId = generation.SessionId,
                    agentName = boundAgentName,
                    bindingKind,
                    host = generation.Host,
                    cwd,
                    workspacePath,
                    endpointKind = normalizedEndpointKind,
                    endpointAddr = normalizedEndpointAddr,
                    endpointSecret = normalizedEndpointSecret,
                    now,
                    role = identityRole,
                    cancellationToken
                },
                transaction);
        }
        else if (IsSameGeneration(existing, generation))
        {
            // A matching host refreshes presence; an actor mismatch also reconciles the
            // binding and role and clears delivery reservations.
            if (existing.AgentName == boundAgentName)
            {
                await connection.ExecuteAsync(
                    "UPDATE agent_sessions SET last_beat_at = @now "
                    + "WHERE harness = @harness AND session_id = @sessionId "
                    + "AND host = @host",
                    new
                    {
                        now,
                        harness = generation.Harness,
                        sessionId = generation.SessionId,
                        host = generation.Host,
                        cancellationToken
                    },
                    transaction);
            }
            else
            {
                await connection.ExecuteAsync(
                    "UPDATE agent_sessions SET agent_name = @agentName, binding_kind = @bindingKind, "
                    + "role = @role, last_beat_at = @now "
                    + "WHERE harness = @harness AND session_id = @sessionId "
                    + "AND host = @host",
                    new
                    {
                        agentName = boundAgentName,
                        bindingKind,
                        role = identityRole,
                        now,
                        harness = generation.Harness,
                        sessionId = generation.SessionId,
                        host = generation.Host,
                        cancellationToken
                    },
                    transaction);
                await connection.ExecuteAsync(
                    "DELETE FROM session_deliveries WHERE harness = @harness AND session_id = @sessionId",
                    new
                    {
                        harness = generation.Harness,
                        sessionId = generation.SessionId,
                        cancellationToken
                    },
                    transaction);
            }
        }
        else
        {
            // A different host replaces session presence and clears delivery, block, and ping state.
            var rowsAffected = await connection.ExecuteAsync(
                """
                UPDATE agent_sessions SET
                    agent_name = @agentName,
                    binding_kind = @bindingKind,
                    host = @host,
                    cwd = @cwd,
                    workspace_path = @workspacePath,
                    endpoint_kind = @endpointKind,
                    endpoint_addr = @endpointAddr,
                    endpoint_secret = @endpointSecret,
                    started_at = @now,
                    last_beat_at = @now,
                    block_budget_used = 0,
                    last_ping_at = NULL,
                    last_ping_attempt = NULL,
                    last_ping_result = NULL,
                    last_ping_detail = NULL,
                    role = @role,
                    harness_version = ''
                WHERE harness = @harness AND session_id = @sessionId
                    AND host = @oldHost;
                """,
                new
                {
                    harness = generation.Harness,
                    sessionId = generation.SessionId,
                    agentName = boundAgentName,
                    bindingKind,
                    host = generation.Host,
                    cwd,
                    workspacePath,
                    endpointKind = normalizedEndpointKind,
                    endpointAddr = normalizedEndpointAddr,
                    endpointSecret = normalizedEndpointSecret,
                    now,
                    role = identityRole,
                    oldHost = existing.Host,
                    cancellationToken
                },
                transaction);

            if (rowsAffected > 0)
            {
                await connection.ExecuteAsync(
                    "DELETE FROM session_deliveries WHERE harness = @harness AND session_id = @sessionId",
                    new { harness = generation.Harness, sessionId = generation.SessionId, cancellationToken },
                    transaction);
            }
        }

        var row = await connection.QueryFirstAsync<AgentSessionRow>(
            $"SELECT {AgentSessionRecord.Columns} FROM agent_sessions "
            + "WHERE harness = @harness AND session_id = @sessionId",
            new { harness = generation.Harness, sessionId = generation.SessionId, cancellationToken },
            transaction);

        await transaction.CommitAsync(cancellationToken);

        return row.ToRecord();
    }

    private async Task<AgentSessionIdentityRecord> EnsureCodingIdentityWithinTransactionAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        AgentSessionGeneration generation,
        DateTimeOffset now,
        string? preferredActor,
        CancellationToken cancellationToken)
    {
        var identity = await connection.QueryFirstOrDefaultAsync<AgentSessionIdentityRecord>(
            $"SELECT {AgentSessionIdentityRecord.Columns} FROM agent_session_identities "
            + "WHERE harness = @harness AND session_id = @sessionId",
            new { harness = generation.Harness, sessionId = generation.SessionId, cancellationToken },
            transaction);

        if (identity is null)
        {
            var actor = preferredActor is null
                ? await AgentActorAllocator.AllocateAsync(connection, transaction)
                : MailAgentName.Normalize(preferredActor);
            await AgentRegistry.UpsertWithinTransactionAsync(
                connection, transaction, timeProvider, actor, string.Empty, generation.Harness, cancellationToken);

            identity = await connection.QueryFirstAsync<AgentSessionIdentityRecord>(
                """
                INSERT INTO agent_session_identities (
                    harness, session_id, actor, role, actor_revision, created_at, last_seen_at)
                VALUES (@harness, @sessionId, @actor, '', 1, @now, @now)
                RETURNING
                    harness AS Harness,
                    session_id AS SessionId,
                    actor AS Actor,
                    role AS Role,
                    actor_revision AS ActorRevision,
                    created_at AS CreatedAt,
                    last_seen_at AS LastSeenAt
                """,
                new
                {
                    harness = generation.Harness,
                    sessionId = generation.SessionId,
                    actor,
                    now,
                    cancellationToken
                },
                transaction);
        }
        else
        {
            await connection.ExecuteAsync(
                "UPDATE agent_session_identities SET last_seen_at = @now "
                + "WHERE harness = @harness AND session_id = @sessionId",
                new { now, harness = generation.Harness, sessionId = generation.SessionId, cancellationToken },
                transaction);
            await AgentRegistry.UpsertWithinTransactionAsync(
                connection, transaction, timeProvider, identity.Actor, identity.Role,
                generation.Harness, cancellationToken);
        }

        return identity;
    }

    private static Task EnsureImplicitActorWithinTransactionAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        string actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => connection.ExecuteAsync(
            """
            INSERT INTO agents (name, registered_at, last_seen_at, role, client, implicit)
            VALUES (@actor, @now, @now, '', '', 1)
            ON CONFLICT (name) DO UPDATE SET name = excluded.name
            """,
            new { actor, now, cancellationToken },
            transaction);

    public async Task<AgentSessionClaimResult> ClaimAsync(
        AgentSessionGeneration generation,
        string actor,
        bool forceRebind,
        CancellationToken cancellationToken)
    {
        var normalizedActor = MailAgentName.Normalize(actor);

        await agentRegistry.EnsureImplicitAsync(normalizedActor, cancellationToken);

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var row = await connection.QueryFirstOrDefaultAsync<AgentSessionRow>(
            $"SELECT {AgentSessionRecord.Columns} FROM agent_sessions "
            + "WHERE harness = @harness AND session_id = @sessionId "
            + "AND host = @host",
            new
            {
                harness = generation.Harness,
                sessionId = generation.SessionId,
                host = generation.Host,
                cancellationToken
            },
            transaction);

        if (row is null)
        {
            throw new ExitException(
                $"No session found for '{generation.Harness}' session '{generation.SessionId}' "
                + "on this host. It may have ended, been reaped, or never started.");
        }

        var previousBindingKind = row.BindingKind;
        var previousAgentName = row.AgentName;

        var durableActor = await connection.QueryFirstOrDefaultAsync<string>(
            "SELECT actor FROM agent_session_identities "
            + "WHERE harness = @harness AND session_id = @sessionId",
            new { harness = generation.Harness, sessionId = generation.SessionId, cancellationToken },
            transaction);
        var effectivePreviousBindingKind = previousBindingKind == AgentSessionBindingKind.Explicit
            && durableActor == previousAgentName
            && durableActor != normalizedActor
                ? AgentSessionBindingKind.None
                : previousBindingKind;

        var (newBindingKind, resetLedger, changed) = ComputeClaimTransition(
            effectivePreviousBindingKind, previousAgentName, normalizedActor, forceRebind, generation.SessionId);

        if (changed)
        {
            await ApplyBindingAsync(
                connection, transaction, generation, normalizedActor, newBindingKind, resetLedger, cancellationToken);
        }

        var updated = await connection.QueryFirstAsync<AgentSessionRow>(
            $"SELECT {AgentSessionRecord.Columns} FROM agent_sessions "
            + "WHERE harness = @harness AND session_id = @sessionId",
            new { harness = generation.Harness, sessionId = generation.SessionId, cancellationToken },
            transaction);

        await transaction.CommitAsync(cancellationToken);

        return new AgentSessionClaimResult(
            updated.ToRecord(),
            changed,
            effectivePreviousBindingKind,
            effectivePreviousBindingKind == AgentSessionBindingKind.None ? null : previousAgentName);
    }

    /// <summary>
    /// Throws <see cref="ExitException"/> when the agent registry does not contain the actor.
    /// </summary>
    private static async Task RequireKnownActorAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        string actor,
        CancellationToken cancellationToken)
    {
        var known = await connection.QueryFirstOrDefaultAsync<string>(
            "SELECT name FROM agents WHERE name = @name",
            new { name = actor, cancellationToken },
            transaction);

        if (known is null)
        {
            throw new ExitException(
                $"Unknown actor '{actor}'. Run `nitro agent login` to allocate one, "
                + "or `nitro agent list` to see the actors this workspace knows.");
        }
    }

    public async Task<AgentSessionRegisterResult> RegisterAsync(
        AgentSessionGeneration generation,
        string actor,
        string role,
        string client,
        bool forceRebind,
        CancellationToken cancellationToken)
    {
        var normalizedActor = MailAgentName.Normalize(actor);
        var normalizedRole = AgentRole.Normalize(role);
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var row = await connection.QueryFirstOrDefaultAsync<AgentSessionRow>(
            $"SELECT {AgentSessionRecord.Columns} FROM agent_sessions "
            + "WHERE harness = @harness AND session_id = @sessionId "
            + "AND host = @host",
            new
            {
                harness = generation.Harness,
                sessionId = generation.SessionId,
                host = generation.Host,
                cancellationToken
            },
            transaction);

        if (row is null)
        {
            throw new ExitException(
                $"No session found for '{generation.Harness}' session '{generation.SessionId}' "
                + "on this host. If hooks were never installed, run "
                + $"`nitro agent hooks {HooksInstallCommandName(generation.Harness)} install` and "
                + "start a new session; otherwise it may have ended or been reaped.");
        }

        await RequireKnownActorAsync(connection, transaction, normalizedActor, cancellationToken);

        var agent = await AgentRegistry.UpsertWithinTransactionAsync(
            connection, transaction, timeProvider, normalizedActor, normalizedRole, client, cancellationToken);

        var previousBindingKind = row.BindingKind;
        var previousAgentName = row.AgentName;

        var durableActor = await connection.QueryFirstOrDefaultAsync<string>(
            "SELECT actor FROM agent_session_identities "
            + "WHERE harness = @harness AND session_id = @sessionId",
            new { harness = generation.Harness, sessionId = generation.SessionId, cancellationToken },
            transaction);
        var effectivePreviousBindingKind = previousBindingKind == AgentSessionBindingKind.Explicit
            && durableActor == previousAgentName
            && durableActor != normalizedActor
                ? AgentSessionBindingKind.None
                : previousBindingKind;

        var (newBindingKind, resetLedger, bindingChanged) = ComputeClaimTransition(
            effectivePreviousBindingKind, previousAgentName, normalizedActor, forceRebind, generation.SessionId);

        if (bindingChanged)
        {
            await ApplyBindingAsync(
                connection, transaction, generation, normalizedActor, newBindingKind, resetLedger, cancellationToken);
        }

        var roleChanged = row.Role != normalizedRole;

        await connection.ExecuteAsync(
            "UPDATE agent_sessions SET role = @role, last_beat_at = @now "
            + "WHERE harness = @harness AND session_id = @sessionId "
            + "AND host = @host",
            new
            {
                role = normalizedRole,
                now,
                harness = generation.Harness,
                sessionId = generation.SessionId,
                host = generation.Host,
                cancellationToken
            },
            transaction);

        var updated = await connection.QueryFirstAsync<AgentSessionRow>(
            $"SELECT {AgentSessionRecord.Columns} FROM agent_sessions "
            + "WHERE harness = @harness AND session_id = @sessionId",
            new { harness = generation.Harness, sessionId = generation.SessionId, cancellationToken },
            transaction);

        await transaction.CommitAsync(cancellationToken);

        return new AgentSessionRegisterResult(
            agent,
            updated.ToRecord(),
            bindingChanged || roleChanged,
            effectivePreviousBindingKind,
            effectivePreviousBindingKind == AgentSessionBindingKind.None ? null : previousAgentName);
    }

    public async Task<AgentSessionRegisterResult> RegisterAsync(
        AgentSessionGeneration generation,
        string? actor,
        bool actorGiven,
        string? role,
        bool roleGiven,
        CancellationToken cancellationToken)
        => await RegisterAsync(
            generation, actor, actorGiven, role, roleGiven, force: false, cancellationToken);

    public async Task<AgentSessionRegisterResult> RegisterAsync(
        AgentSessionGeneration generation,
        string? actor,
        bool actorGiven,
        string? role,
        bool roleGiven,
        bool force,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction(deferred: false);

        var row = await connection.QueryFirstOrDefaultAsync<AgentSessionRow>(
            $"SELECT {AgentSessionRecord.Columns} FROM agent_sessions "
            + "WHERE harness = @harness AND session_id = @sessionId "
            + "AND host = @host",
            new
            {
                harness = generation.Harness,
                sessionId = generation.SessionId,
                host = generation.Host,
                cancellationToken
            },
            transaction);

        if (row is null)
        {
            throw new ExitException(
                $"No session found for '{generation.Harness}' session '{generation.SessionId}' "
                + "on this host. If hooks were never installed, run "
                + $"`nitro agent hooks {HooksInstallCommandName(generation.Harness)} install` and "
                + "start a new session; otherwise it may have ended or been reaped.");
        }

        var identity = await connection.QueryFirstOrDefaultAsync<AgentSessionIdentityRecord>(
            $"SELECT {AgentSessionIdentityRecord.Columns} FROM agent_session_identities "
            + "WHERE harness = @harness AND session_id = @sessionId",
            new { harness = generation.Harness, sessionId = generation.SessionId, cancellationToken },
            transaction)
            ?? await EnsureCodingIdentityWithinTransactionAsync(
                connection, transaction, generation, now, preferredActor: null, cancellationToken);

        var normalizedActor = actorGiven
            ? MailAgentName.Normalize(actor ?? string.Empty)
            : identity.Actor;

        if (actorGiven)
        {
            await RequireKnownActorAsync(connection, transaction, normalizedActor, cancellationToken);
        }

        if (actorGiven)
        {
            var known = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT name FROM agents WHERE name = @name",
                new { name = normalizedActor, cancellationToken },
                transaction);

            if (known is null)
            {
                throw new ExitException(
                    $"Unknown actor '{normalizedActor}'. Run `nitro agent login` to allocate one, "
                    + "or `nitro agent list` to see the actors this workspace knows.");
            }
        }
        var normalizedRole = roleGiven ? AgentRole.Normalize(role) : identity.Role;

        var conflictingIdentity = await connection.QueryFirstOrDefaultAsync<AgentSessionIdentityRecord>(
            """
            SELECT
                harness AS Harness,
                session_id AS SessionId,
                actor AS Actor,
                role AS Role,
                actor_revision AS ActorRevision,
                created_at AS CreatedAt,
                last_seen_at AS LastSeenAt
            FROM agent_session_identities
            WHERE actor = @actor
              AND NOT (harness = @harness AND session_id = @sessionId)
            """,
            new
            {
                actor = normalizedActor,
                harness = generation.Harness,
                sessionId = generation.SessionId,
                cancellationToken
            },
            transaction);

        if (conflictingIdentity is not null && !force)
        {
            throw new ExitException(
                $"Actor '{normalizedActor}' is already assigned to another session.");
        }

        if (conflictingIdentity is not null)
        {
            await connection.ExecuteAsync(
                "DELETE FROM agent_sessions WHERE harness = @harness AND session_id = @sessionId",
                new
                {
                    harness = conflictingIdentity.Harness,
                    sessionId = conflictingIdentity.SessionId,
                    cancellationToken
                },
                transaction);
            await connection.ExecuteAsync(
                "DELETE FROM agent_session_identities WHERE harness = @harness AND session_id = @sessionId",
                new
                {
                    harness = conflictingIdentity.Harness,
                    sessionId = conflictingIdentity.SessionId,
                    cancellationToken
                },
                transaction);
        }

        var agent = await AgentRegistry.UpsertWithinTransactionAsync(
            connection, transaction, timeProvider, normalizedActor, normalizedRole,
            generation.Harness, cancellationToken);

        var previousBindingKind = row.BindingKind;
        var previousAgentName = row.AgentName;
        var actorChanged = identity.Actor != normalizedActor;
        var roleChanged = identity.Role != normalizedRole;

        await connection.ExecuteAsync(
            """
            UPDATE agent_session_identities SET
                actor = @actor,
                role = @role,
                actor_revision = actor_revision + CASE WHEN actor <> @actor THEN 1 ELSE 0 END,
                last_seen_at = @now
            WHERE harness = @harness AND session_id = @sessionId
            """,
            new
            {
                actor = normalizedActor,
                role = normalizedRole,
                now,
                harness = generation.Harness,
                sessionId = generation.SessionId,
                cancellationToken
            },
            transaction);

        await connection.ExecuteAsync(
            "UPDATE agent_sessions SET agent_name = @actor, binding_kind = 'explicit', "
            + "role = @role, last_beat_at = @now "
            + "WHERE harness = @harness AND session_id = @sessionId",
            new
            {
                actor = normalizedActor,
                role = normalizedRole,
                now,
                harness = generation.Harness,
                sessionId = generation.SessionId,
                cancellationToken
            },
            transaction);

        if (actorChanged)
        {
            await connection.ExecuteAsync(
                "DELETE FROM session_deliveries WHERE harness = @harness AND session_id = @sessionId",
                new { harness = generation.Harness, sessionId = generation.SessionId, cancellationToken },
                transaction);
        }

        var updated = await connection.QueryFirstAsync<AgentSessionRow>(
            $"SELECT {AgentSessionRecord.Columns} FROM agent_sessions "
            + "WHERE harness = @harness AND session_id = @sessionId",
            new { harness = generation.Harness, sessionId = generation.SessionId, cancellationToken },
            transaction);

        await transaction.CommitAsync(cancellationToken);

        return new AgentSessionRegisterResult(
            agent, updated.ToRecord(), actorChanged || roleChanged, previousBindingKind, previousAgentName);
    }

    public async Task<AgentSessionRecord?> FindBySessionIdAsync(
        string harness, string host, string sessionId, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var row = await connection.QueryFirstOrDefaultAsync<AgentSessionRow>(
            $"SELECT {AgentSessionRecord.Columns} FROM agent_sessions "
            + "WHERE harness = @harness AND host = @host AND session_id = @sessionId",
            new { harness, host, sessionId, cancellationToken });

        return row?.ToRecord();
    }

    /// <summary>
    /// Computes the explicit binding transition and whether delivery state must reset.
    /// Changing an existing explicit actor requires <paramref name="forceRebind"/>.
    /// </summary>
    private static (string NewBindingKind, bool ResetLedger, bool Changed) ComputeClaimTransition(
        string previousBindingKind,
        string? previousAgentName,
        string normalizedActor,
        bool forceRebind,
        string sessionId)
        => (previousBindingKind, previousAgentName) switch
        {
            (AgentSessionBindingKind.None, _) =>
                (AgentSessionBindingKind.Explicit, true, true),

            (AgentSessionBindingKind.Env, var current) when current == normalizedActor =>
                (AgentSessionBindingKind.Explicit, false, true),

            (AgentSessionBindingKind.Env, _) =>
                (AgentSessionBindingKind.Explicit, true, true),

            (AgentSessionBindingKind.Explicit, var current) when current == normalizedActor =>
                (AgentSessionBindingKind.Explicit, false, false),

            (AgentSessionBindingKind.Explicit, _) when !forceRebind =>
                throw new ExitException(
                    $"Session '{sessionId}' is already explicitly claimed by "
                    + $"'{previousAgentName}'. Use --force-rebind to reclaim it as '{normalizedActor}'."),

            _ => (AgentSessionBindingKind.Explicit, true, true)
        };

    /// <summary>
    /// Updates the matching session's actor binding.
    /// When <paramref name="resetLedger"/> is true, clears its delivery reservations and block budget.
    /// </summary>
    private static async Task ApplyBindingAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        AgentSessionGeneration generation,
        string normalizedActor,
        string newBindingKind,
        bool resetLedger,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(
            "UPDATE agent_sessions SET agent_name = @agentName, binding_kind = @bindingKind "
            + "WHERE harness = @harness AND session_id = @sessionId "
            + "AND host = @host",
            new
            {
                agentName = normalizedActor,
                bindingKind = newBindingKind,
                harness = generation.Harness,
                sessionId = generation.SessionId,
                host = generation.Host,
                cancellationToken
            },
            transaction);

        if (resetLedger)
        {
            await connection.ExecuteAsync(
                "DELETE FROM session_deliveries WHERE harness = @harness AND session_id = @sessionId",
                new { harness = generation.Harness, sessionId = generation.SessionId, cancellationToken },
                transaction);

            await connection.ExecuteAsync(
                "UPDATE agent_sessions SET block_budget_used = 0 "
                + "WHERE harness = @harness AND session_id = @sessionId "
                + "AND host = @host",
                new
                {
                    harness = generation.Harness,
                    sessionId = generation.SessionId,
                    host = generation.Host,
                    cancellationToken
                },
                transaction);
        }
    }

    public async Task<bool> EndAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "DELETE FROM agent_sessions WHERE harness = @harness AND session_id = @sessionId "
            + "AND host = @host";
        command.Parameters.AddWithValue("@harness", generation.Harness);
        command.Parameters.AddWithValue("@sessionId", generation.SessionId);
        command.Parameters.AddWithValue("@host", generation.Host);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

        return rowsAffected > 0;
    }

    public async Task<AgentSessionRecord?> FindByGenerationAsync(
        AgentSessionGeneration generation, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT {AgentSessionRecord.Columns} FROM agent_sessions "
            + "WHERE harness = @harness AND session_id = @sessionId "
            + "AND host = @host";
        command.Parameters.AddWithValue("@harness", generation.Harness);
        command.Parameters.AddWithValue("@sessionId", generation.SessionId);
        command.Parameters.AddWithValue("@host", generation.Host);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return AgentSessionRow.ReadFrom(reader).ToRecord();
    }

    public async Task ResetBlockBudgetAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE agent_sessions SET block_budget_used = 0 "
            + "WHERE harness = @harness AND session_id = @sessionId "
            + "AND host = @host";
        command.Parameters.AddWithValue("@harness", generation.Harness);
        command.Parameters.AddWithValue("@sessionId", generation.SessionId);
        command.Parameters.AddWithValue("@host", generation.Host);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int?> IncrementBlockBudgetAsync(
        AgentSessionGeneration generation, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = (SqliteTransaction)transaction;
        updateCommand.CommandText =
            "UPDATE agent_sessions SET block_budget_used = block_budget_used + 1 "
            + "WHERE harness = @harness AND session_id = @sessionId "
            + "AND host = @host";
        updateCommand.Parameters.AddWithValue("@harness", generation.Harness);
        updateCommand.Parameters.AddWithValue("@sessionId", generation.SessionId);
        updateCommand.Parameters.AddWithValue("@host", generation.Host);

        var rowsAffected = await updateCommand.ExecuteNonQueryAsync(cancellationToken);

        if (rowsAffected == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        await using var selectCommand = connection.CreateCommand();
        selectCommand.Transaction = (SqliteTransaction)transaction;
        selectCommand.CommandText =
            "SELECT block_budget_used FROM agent_sessions "
            + "WHERE harness = @harness AND session_id = @sessionId "
            + "AND host = @host";
        selectCommand.Parameters.AddWithValue("@harness", generation.Harness);
        selectCommand.Parameters.AddWithValue("@sessionId", generation.SessionId);
        selectCommand.Parameters.AddWithValue("@host", generation.Host);

        var updated = (int)(long)(await selectCommand.ExecuteScalarAsync(cancellationToken))!;

        await transaction.CommitAsync(cancellationToken);

        return updated;
    }

    public async Task<IReadOnlyList<AgentSessionRecord>> ReapAsync(CancellationToken cancellationToken)
    {
        var host = await ResolveHostAsync(cancellationToken);
        var cutoff = timeProvider.GetUtcNow() - s_staleAfter;

        await using var connection = await ConnectAsync(cancellationToken);

        var candidates = await connection.QueryAsync<AgentSessionRow>(
            $"SELECT {AgentSessionRecord.Columns} FROM agent_sessions WHERE host = @host",
            new { host, cancellationToken });

        var reaped = new List<AgentSessionRecord>();

        foreach (var candidate in candidates)
        {
            var record = candidate.ToRecord();

            if (record.LastBeatAt > cutoff)
            {
                continue;
            }

            if (OnStaleReapCandidateCapturedAsync is { } onStaleReapCandidateCapturedAsync)
            {
                await onStaleReapCandidateCapturedAsync(record, cancellationToken);
            }

            // Delete only if the recorded host and heartbeat still match.
            var rowsAffected = await connection.ExecuteAsync(
                "DELETE FROM agent_sessions WHERE harness = @harness AND session_id = @sessionId "
                + "AND host = @host AND last_beat_at = @lastBeatAt",
                new
                {
                    harness = record.Harness,
                    sessionId = record.SessionId,
                    host = record.Host,
                    lastBeatAt = record.LastBeatAt,
                    cancellationToken
                });

            if (rowsAffected > 0)
            {
                reaped.Add(record);

                if (record.Harness == AgentSessionHarness.Copilot)
                {
                    await connection.ExecuteAsync(
                        "DELETE FROM agent_session_identities "
                        + "WHERE harness = @harness AND session_id = @sessionId",
                        new
                        {
                            harness = record.Harness,
                            sessionId = record.SessionId,
                            cancellationToken
                        });
                }
            }
        }

        return reaped;
    }

    public async Task<IReadOnlyList<AgentSessionView>> ListAsync(CancellationToken cancellationToken)
    {
        await ReapAsync(cancellationToken);

        var host = await ResolveHostAsync(cancellationToken);

        await using var connection = await ConnectAsync(cancellationToken);

        var rows = await connection.QueryAsync<AgentSessionRow>(
            $"SELECT {AgentSessionRecord.Columns} FROM agent_sessions ORDER BY harness, session_id");

        return rows.Select(row =>
        {
            var record = row.ToRecord();
            return new AgentSessionView(record, ComputeState(record, host));
        }).ToList();
    }

    /// <summary>
    /// Returns remote for another host, unreachable for a local session without an
    /// endpoint, or online otherwise.
    /// </summary>
    private static string ComputeState(AgentSessionRecord record, string host)
        => record.Host != host
            ? AgentSessionState.Remote
            : record.EndpointKind == AgentSessionEndpointKind.None
                ? AgentSessionState.Unreachable
                : AgentSessionState.Online;

    public async Task<bool> TouchAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE agent_sessions SET last_beat_at = @now "
            + "WHERE harness = @harness AND session_id = @sessionId "
            + "AND host = @host";
        command.Parameters.AddWithValue("@now", now);
        command.Parameters.AddWithValue("@harness", generation.Harness);
        command.Parameters.AddWithValue("@sessionId", generation.SessionId);
        command.Parameters.AddWithValue("@host", generation.Host);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

        return rowsAffected > 0;
    }

    public async Task<bool> RecordHarnessVersionAsync(
        AgentSessionGeneration generation, string harnessVersion, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE agent_sessions SET harness_version = @harnessVersion "
            + "WHERE harness = @harness AND session_id = @sessionId "
            + "AND host = @host";
        command.Parameters.AddWithValue("@harnessVersion", harnessVersion);
        command.Parameters.AddWithValue("@harness", generation.Harness);
        command.Parameters.AddWithValue("@sessionId", generation.SessionId);
        command.Parameters.AddWithValue("@host", generation.Host);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

        return rowsAffected > 0;
    }

    /// <summary>
    /// Reaps stale local sessions, then returns each surviving session with its agent
    /// identity when available and its computed presence state.
    /// </summary>
    public async Task<IReadOnlyList<AgentSessionParticipant>> ListParticipantsAsync(
        CancellationToken cancellationToken)
    {
        await ReapAsync(cancellationToken);

        var host = await ResolveHostAsync(cancellationToken);

        await using var connection = await ConnectAsync(cancellationToken);

        var rows = await connection.QueryAsync<AgentSessionRow>(
            $"SELECT {AgentSessionRecord.Columns} FROM agent_sessions ORDER BY harness, session_id");

        var resolvedAgents = new Dictionary<string, AgentRecord>(StringComparer.Ordinal);
        var participants = new List<AgentSessionParticipant>();

        foreach (var row in rows)
        {
            var session = row.ToRecord();
            AgentRecord? agent = null;

            if (session.AgentName is { } agentName)
            {
                if (!resolvedAgents.TryGetValue(agentName, out agent))
                {
                    agent = await agentRegistry.GetAsync(agentName, cancellationToken);

                    if (agent is not null)
                    {
                        resolvedAgents[agentName] = agent;
                    }
                }
            }

            participants.Add(new AgentSessionParticipant(session, agent, ComputeState(session, host)));
        }

        return participants;
    }

    public async Task<IReadOnlyList<AgentSessionIdentityView>> ListIdentitiesAsync(
        CancellationToken cancellationToken)
    {
        var participants = await ListParticipantsAsync(cancellationToken);
        var bySession = participants.ToDictionary(
            participant => (participant.Session.Harness, participant.Session.SessionId));

        await using var connection = await ConnectAsync(cancellationToken);
        var identities = await connection.QueryAsync<AgentSessionIdentityRecord>(
            $"SELECT {AgentSessionIdentityRecord.Columns} FROM agent_session_identities "
            + "ORDER BY actor");

        return identities
            .Select(identity => new AgentSessionIdentityView(
                identity,
                bySession.GetValueOrDefault((identity.Harness, identity.SessionId))))
            .ToList();
    }

    public async Task<IReadOnlyList<AgentSessionRecord>> FindLiveClaimedByAgentNameAsync(
        string agentName, CancellationToken cancellationToken)
    {
        await ReapAsync(cancellationToken);

        var host = await ResolveHostAsync(cancellationToken);

        await using var connection = await ConnectAsync(cancellationToken);

        var rows = await connection.QueryAsync<AgentSessionRow>(
            $"SELECT {AgentSessionRecord.Columns} FROM agent_sessions "
            + "WHERE agent_name = @agentName AND host = @host "
            + "ORDER BY harness, session_id",
            new { agentName, host, cancellationToken });

        return rows.Select(row => row.ToRecord()).ToList();
    }

    public async Task<bool> TryClaimPingCooldownAsync(
        AgentSessionRecord session,
        string attemptId,
        DateTimeOffset now,
        TimeSpan cooldown,
        CancellationToken cancellationToken)
    {
        var cutoff = now - cooldown;

        await using var connection = await ConnectAsync(cancellationToken);

        var rowsAffected = await connection.ExecuteAsync(
            """
            UPDATE agent_sessions SET
                last_ping_at = @now,
                last_ping_attempt = @attemptId,
                last_ping_result = NULL,
                last_ping_detail = NULL
            WHERE harness = @harness AND session_id = @sessionId
                AND host = @host
                AND (last_ping_at IS NULL OR last_ping_at <= @cutoff);
            """,
            new
            {
                now,
                attemptId,
                harness = session.Harness,
                sessionId = session.SessionId,
                host = session.Host,
                cutoff,
                cancellationToken
            });

        return rowsAffected > 0;
    }

    public async Task WritePingResultAsync(
        string harness,
        string sessionId,
        string attemptId,
        string result,
        string? detail,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        await connection.ExecuteAsync(
            """
            UPDATE agent_sessions SET last_ping_result = @result, last_ping_detail = @detail
            WHERE harness = @harness AND session_id = @sessionId AND last_ping_attempt = @attemptId;
            """,
            new { result, detail, harness, sessionId, attemptId, cancellationToken });
    }

    public async Task<bool> SetRoleAsync(
        AgentSessionGeneration generation, string role, CancellationToken cancellationToken)
    {
        var normalizedRole = AgentRole.Normalize(role);

        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE agent_sessions SET role = @role "
            + "WHERE harness = @harness AND session_id = @sessionId "
            + "AND host = @host";
        command.Parameters.AddWithValue("@role", normalizedRole);
        command.Parameters.AddWithValue("@harness", generation.Harness);
        command.Parameters.AddWithValue("@sessionId", generation.SessionId);
        command.Parameters.AddWithValue("@host", generation.Host);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

        return rowsAffected > 0;
    }

    public Task ArmAnnouncementAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => SetSessionFlagAsync(generation, "announcement_pending", value: true, cancellationToken);

    public Task<bool> ClaimAnnouncementAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => ClaimSessionFlagAsync(generation, "announcement_pending", cancellationToken);

    public Task<bool> IsAnnouncementPendingAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => PeekSessionFlagAsync(generation, "announcement_pending", cancellationToken);

    public Task RearmIdlePushAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => SetSessionFlagAsync(generation, "idle_push_armed", value: true, cancellationToken);

    public Task<bool> ClaimIdlePushAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => ClaimSessionFlagAsync(generation, "idle_push_armed", cancellationToken);

    /// <summary>
    /// Sets the named flag for the matching session; a missing session is unchanged.
    /// <paramref name="column"/> must be a trusted session flag column name.
    /// </summary>
    private async Task SetSessionFlagAsync(
        AgentSessionGeneration generation, string column, bool value, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"UPDATE agent_sessions SET {column} = @value "
            + "WHERE harness = @harness AND session_id = @sessionId "
            + "AND host = @host";
        command.Parameters.AddWithValue("@value", value ? 1 : 0);
        command.Parameters.AddWithValue("@harness", generation.Harness);
        command.Parameters.AddWithValue("@sessionId", generation.SessionId);
        command.Parameters.AddWithValue("@host", generation.Host);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Clears the named flag for the matching session and returns whether it was set.
    /// <paramref name="column"/> must be a trusted session flag column name.
    /// </summary>
    private async Task<bool> ClaimSessionFlagAsync(
        AgentSessionGeneration generation, string column, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"UPDATE agent_sessions SET {column} = 0 "
            + "WHERE harness = @harness AND session_id = @sessionId "
            + $"AND host = @host AND {column} = 1";
        command.Parameters.AddWithValue("@harness", generation.Harness);
        command.Parameters.AddWithValue("@sessionId", generation.SessionId);
        command.Parameters.AddWithValue("@host", generation.Host);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

        return rowsAffected > 0;
    }

    /// <summary>
    /// Returns whether the named flag is set for the matching session, or false when
    /// none matches. <paramref name="column"/> must be a trusted session flag column name.
    /// </summary>
    private async Task<bool> PeekSessionFlagAsync(
        AgentSessionGeneration generation, string column, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT {column} FROM agent_sessions "
            + "WHERE harness = @harness AND session_id = @sessionId AND host = @host";
        command.Parameters.AddWithValue("@harness", generation.Harness);
        command.Parameters.AddWithValue("@sessionId", generation.SessionId);
        command.Parameters.AddWithValue("@host", generation.Host);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is not null && (long)result != 0;
    }

    private async Task<string> ResolveHostAsync(CancellationToken cancellationToken)
        => await instanceIdProvider.GetIdAsync(globalConfigDirectoryProvider.GetDirectory(), cancellationToken);

    private static bool IsSameGeneration(AgentSessionRow existing, AgentSessionGeneration generation)
        => existing.Host == generation.Host;

    /// <summary>
    /// Returns the hooks command name for the harness, mapping <c>claude-code</c> to <c>claude</c>.
    /// </summary>
    private static string HooksInstallCommandName(string harness) => harness switch
    {
        AgentSessionHarness.ClaudeCode => "claude",
        _ => harness
    };

    /// <summary>
    /// Normalizes invalid or absent endpoints to kind <c>none</c>, an empty address,
    /// and no credential. Credentials are retained only for valid opencode-server
    /// endpoints belonging to the opencode harness.
    /// </summary>
    private static (string Kind, string Addr, string? Secret) NormalizeEndpoint(
        string harness,
        string endpointKind,
        string endpointAddr,
        string? endpointSecret)
    {
        if (endpointKind == AgentSessionEndpointKind.OpencodeServer)
        {
            return EndpointAddress.IsValidOpencodeServerUrl(endpointAddr)
                ? (endpointKind, endpointAddr, harness == AgentSessionHarness.Opencode ? endpointSecret : null)
                : (AgentSessionEndpointKind.None, string.Empty, null);
        }

        if (endpointKind == AgentSessionEndpointKind.None || !EndpointAddress.IsValid(endpointAddr))
        {
            return (AgentSessionEndpointKind.None, string.Empty, null);
        }

        return (endpointKind, endpointAddr, null);
    }

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }

    internal sealed class AgentSessionRow
    {
        public required string Harness { get; init; }
        public required string SessionId { get; init; }
        public string? AgentName { get; init; }
        public required string BindingKind { get; init; }
        public required string Host { get; init; }
        public required string Cwd { get; init; }
        public required string WorkspacePath { get; init; }
        public required string EndpointKind { get; init; }
        public required string EndpointAddr { get; init; }
        public string? EndpointSecret { get; init; }
        public required string StartedAt { get; init; }
        public required string LastBeatAt { get; init; }
        public required int BlockBudgetUsed { get; init; }
        public string? LastPingAt { get; init; }
        public string? LastPingAttempt { get; init; }
        public string? LastPingResult { get; init; }
        public string? LastPingDetail { get; init; }
        public required string Role { get; init; }
        public required string HarnessVersion { get; init; }

        /// <summary>
        /// Reads the current row using the column names declared by <see cref="AgentSessionRecord.Columns"/>.
        /// </summary>
        public static AgentSessionRow ReadFrom(DbDataReader reader) => new()
        {
            Harness = reader.GetString(reader.GetOrdinal("Harness")),
            SessionId = reader.GetString(reader.GetOrdinal("SessionId")),
            AgentName = reader.IsDBNull(reader.GetOrdinal("AgentName"))
                ? null
                : reader.GetString(reader.GetOrdinal("AgentName")),
            BindingKind = reader.GetString(reader.GetOrdinal("BindingKind")),
            Host = reader.GetString(reader.GetOrdinal("Host")),
            Cwd = reader.GetString(reader.GetOrdinal("Cwd")),
            WorkspacePath = reader.GetString(reader.GetOrdinal("WorkspacePath")),
            EndpointKind = reader.GetString(reader.GetOrdinal("EndpointKind")),
            EndpointAddr = reader.GetString(reader.GetOrdinal("EndpointAddr")),
            EndpointSecret = reader.IsDBNull(reader.GetOrdinal("EndpointSecret"))
                ? null
                : reader.GetString(reader.GetOrdinal("EndpointSecret")),
            StartedAt = reader.GetString(reader.GetOrdinal("StartedAt")),
            LastBeatAt = reader.GetString(reader.GetOrdinal("LastBeatAt")),
            BlockBudgetUsed = reader.GetInt32(reader.GetOrdinal("BlockBudgetUsed")),
            LastPingAt = reader.IsDBNull(reader.GetOrdinal("LastPingAt"))
                ? null
                : reader.GetString(reader.GetOrdinal("LastPingAt")),
            LastPingAttempt = reader.IsDBNull(reader.GetOrdinal("LastPingAttempt"))
                ? null
                : reader.GetString(reader.GetOrdinal("LastPingAttempt")),
            LastPingResult = reader.IsDBNull(reader.GetOrdinal("LastPingResult"))
                ? null
                : reader.GetString(reader.GetOrdinal("LastPingResult")),
            LastPingDetail = reader.IsDBNull(reader.GetOrdinal("LastPingDetail"))
                ? null
                : reader.GetString(reader.GetOrdinal("LastPingDetail")),
            Role = reader.GetString(reader.GetOrdinal("Role")),
            HarnessVersion = reader.GetString(reader.GetOrdinal("HarnessVersion"))
        };

        public AgentSessionRecord ToRecord() => new()
        {
            Harness = Harness,
            SessionId = SessionId,
            AgentName = AgentName,
            BindingKind = BindingKind,
            Host = Host,
            Cwd = Cwd,
            WorkspacePath = WorkspacePath,
            EndpointKind = EndpointKind,
            EndpointAddr = EndpointAddr,
            EndpointSecret = EndpointSecret,
            StartedAt = DateTimeOffset.Parse(StartedAt, CultureInfo.InvariantCulture),
            LastBeatAt = DateTimeOffset.Parse(LastBeatAt, CultureInfo.InvariantCulture),
            BlockBudgetUsed = BlockBudgetUsed,
            LastPingAt = LastPingAt is null ? null : DateTimeOffset.Parse(LastPingAt, CultureInfo.InvariantCulture),
            LastPingAttempt = LastPingAttempt,
            LastPingResult = LastPingResult,
            LastPingDetail = LastPingDetail,
            Role = Role,
            HarnessVersion = HarnessVersion
        };
    }
}
