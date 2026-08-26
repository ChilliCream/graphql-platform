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
    /// How long a session row survives without a heartbeat before
    /// <see cref="ReapAsync"/> removes it. A live session beats on every
    /// hook event it sends.
    /// </summary>
    private static readonly TimeSpan s_staleAfter = TimeSpan.FromHours(24);

    public async Task<AgentSessionRecord> StartAsync(
        AgentSessionGeneration generation,
        string cwd,
        string workspacePath,
        string endpointKind,
        string endpointAddr,
        string? envActor,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var (normalizedEndpointKind, normalizedEndpointAddr) = NormalizeEndpoint(endpointKind, endpointAddr);

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction(deferred: false);

        string? boundAgentName;
        string bindingKind;
        string identityRole;

        if (generation.Harness is AgentSessionHarness.ClaudeCode
            or AgentSessionHarness.Codex
            or AgentSessionHarness.Copilot)
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
                    cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
                    block_budget_used, role
                ) VALUES (
                    @harness, @sessionId, @agentName, @bindingKind, @host,
                    @cwd, @workspacePath, @endpointKind, @endpointAddr, @now, @now, 0, @role
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
                    now,
                    role = identityRole,
                    cancellationToken
                },
                transaction);
        }
        else if (IsSameGeneration(existing, generation))
        {
            // Normal duplicate SessionStart only refreshes the heartbeat.
            // A pre-v9 row can have no durable identity yet, however. When
            // EnsureCodingIdentity created one above, reconcile the copied
            // live-row actor and clear its old delivery ledger atomically.
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
            // A generation change on the same (harness, session_id): a new
            // process replaced the one the row remembered. Treat it as a
            // fresh SessionStart, rebinding exactly as the missing-row case
            // above does, and reset the delivery ledger and counters.
            //
            // Both statements below predicate on the OLD generation
            // (`existing`), not just (harness, session_id): a reader that
            // observed this same old generation is the only writer allowed
            // to act on it. Without the full predicate, two processes
            // racing to rebind the same stale row could both read the same
            // `existing` snapshot, and the second writer would blindly
            // overwrite whatever the first writer already committed instead
            // of affecting zero rows (the plan's "full-generation predicates
            // on all lifecycle mutations" rule, carried forward from the
            // .6 review as a hardening item for this bead).
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

        // Resolved BEFORE opening this method's own connection and
        // transaction: EnsureImplicitAsync opens a separate connection to
        // the same database file, and starting a second writer transaction
        // while this one is already open self-deadlocks SQLite ("database
        // is locked") instead of merely serializing.
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
    /// Fails when no agent carries this name. Actor names are allocated,
    /// never invented: only <c>agent login</c> and the session-start hooks
    /// mint one.
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

        // Actor names are allocated, never invented: `register` binds a name
        // `agent login` or a session-start hook already minted.
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
    /// The claim state machine <see cref="ClaimAsync"/> and registration
    /// both apply: none binds, env promotes to
    /// explicit (resetting the ledger only for a different actor), explicit
    /// for the same actor is a no-op, and explicit for a different actor
    /// requires <paramref name="forceRebind"/>.
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
                // Provenance-only promotion: without it, a later
                // different-actor claim could bypass the force-rebind
                // protection explicit(A) -> explicit(B) enforces below.
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
    /// Applies a changed binding: sets <c>agent_name</c>/<c>binding_kind</c>,
    /// and when <paramref name="resetLedger"/> also clears the delivery
    /// ledger and block budget, all predicated on <paramref
    /// name="generation"/> exactly.
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

            // A session that has not beaten within the stale window is
            // reaped. A live session beats on every hook event it sends, so
            // silence this long means the harness ended without its
            // SessionEnd hook running.
            if (record.LastBeatAt > cutoff)
            {
                continue;
            }

            // The heartbeat predicate guards a TOCTOU race: if the row beat
            // again between the SELECT above and this DELETE, the WHERE
            // clause matches nothing and the live session survives.
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
    /// Computes the same <see cref="AgentSessionState"/> <see cref="ListAsync"/>
    /// and <see cref="ListParticipantsAsync"/> report for <paramref name="record"/>,
    /// relative to the current instance's resolved <paramref name="host"/>.
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
    /// Reaps dead current-instance rows, then returns one
    /// <see cref="AgentSessionParticipant"/> per surviving row, joining the
    /// durable <see cref="AgentRecord"/> its <c>agent_name</c> binds to when
    /// the session is claimed, and computing the same
    /// <see cref="AgentSessionState"/> as <see cref="ListAsync"/>.
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

    private async Task<string> ResolveHostAsync(CancellationToken cancellationToken)
        => await instanceIdProvider.GetIdAsync(globalConfigDirectoryProvider.GetDirectory(), cancellationToken);

    private static bool IsSameGeneration(AgentSessionRow existing, AgentSessionGeneration generation)
        => existing.Host == generation.Host;

    /// <summary>
    /// Maps an <see cref="AgentSessionHarness"/> value to the harness name
    /// its <c>hooks</c> subcommand group uses, which differs from the
    /// harness value only for Claude Code (<c>claude-code</c> installs under
    /// <c>claude</c>).
    /// </summary>
    private static string HooksInstallCommandName(string harness) => harness switch
    {
        AgentSessionHarness.ClaudeCode => "claude",
        _ => harness
    };

    /// <summary>
    /// Demotes an endpoint to <c>none</c> when its kind is already
    /// <c>none</c> or its address fails the write-time grammar
    /// <see cref="EndpointAddress"/> enforces; the table's cross-column
    /// CHECK requires the two to agree.
    /// </summary>
    private static (string Kind, string Addr) NormalizeEndpoint(string endpointKind, string endpointAddr)
    {
        if (endpointKind == AgentSessionEndpointKind.None || !EndpointAddress.IsValid(endpointAddr))
        {
            return (AgentSessionEndpointKind.None, string.Empty);
        }

        return (endpointKind, endpointAddr);
    }

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }

    // Internal, not private: Dapper.AOT's generated interceptors live
    // outside AgentSessionRegistry and cannot reference a private nested
    // type, so a private row type would silently fall back to Dapper's
    // reflection-emit deserializer. Mirrors AgentRegistry.AgentRegistryRow.
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
        /// Maps a row from a <see cref="AgentSessionRecord.Columns"/> query
        /// by column name, for the raw ADO.NET reads Dapper.AOT cannot
        /// intercept (a runtime-assembled <c>CommandDefinition</c>).
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
