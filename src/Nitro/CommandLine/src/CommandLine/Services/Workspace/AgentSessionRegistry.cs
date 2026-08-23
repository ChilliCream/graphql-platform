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
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider,
    IProcessInfoProvider processInfoProvider,
    IClaudeAncestorSessionResolver ancestorResolver) : IAgentSessionRegistry
{
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

        string? boundAgentName = null;

        if (envActor is not null)
        {
            boundAgentName = MailAgentName.Normalize(envActor);
            await agentRegistry.EnsureImplicitAsync(boundAgentName, cancellationToken);
        }

        var bindingKind = boundAgentName is null ? AgentSessionBindingKind.None : AgentSessionBindingKind.Env;

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

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
                    harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                    cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
                    block_budget_used
                ) VALUES (
                    @harness, @sessionId, @agentName, @bindingKind, @host, @pid, @procStart,
                    @cwd, @workspacePath, @endpointKind, @endpointAddr, @now, @now, 0
                );
                """,
                new
                {
                    harness = generation.Harness,
                    sessionId = generation.SessionId,
                    agentName = boundAgentName,
                    bindingKind,
                    host = generation.Host,
                    pid = generation.Pid,
                    procStart = generation.ProcStart,
                    cwd,
                    workspacePath,
                    endpointKind = normalizedEndpointKind,
                    endpointAddr = normalizedEndpointAddr,
                    now,
                    cancellationToken
                },
                transaction);
        }
        else if (IsSameGeneration(existing, generation))
        {
            // Same-generation duplicate SessionStart: preserves binding,
            // ledger, and counters, only refreshing the heartbeat.
            await connection.ExecuteAsync(
                "UPDATE agent_sessions SET last_beat_at = @now "
                + "WHERE harness = @harness AND session_id = @sessionId "
                + "AND pid = @pid AND proc_start = @procStart AND host = @host",
                new
                {
                    now,
                    harness = generation.Harness,
                    sessionId = generation.SessionId,
                    pid = generation.Pid,
                    procStart = generation.ProcStart,
                    host = generation.Host,
                    cancellationToken
                },
                transaction);
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
                    pid = @pid,
                    proc_start = @procStart,
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
                    last_ping_detail = NULL
                WHERE harness = @harness AND session_id = @sessionId
                    AND pid = @oldPid AND proc_start = @oldProcStart AND host = @oldHost;
                """,
                new
                {
                    harness = generation.Harness,
                    sessionId = generation.SessionId,
                    agentName = boundAgentName,
                    bindingKind,
                    host = generation.Host,
                    pid = generation.Pid,
                    procStart = generation.ProcStart,
                    cwd,
                    workspacePath,
                    endpointKind = normalizedEndpointKind,
                    endpointAddr = normalizedEndpointAddr,
                    now,
                    oldPid = existing.Pid,
                    oldProcStart = existing.ProcStart,
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
            + "AND pid = @pid AND proc_start = @procStart AND host = @host",
            new
            {
                harness = generation.Harness,
                sessionId = generation.SessionId,
                pid = generation.Pid,
                procStart = generation.ProcStart,
                host = generation.Host,
                cancellationToken
            },
            transaction);

        if (row is null)
        {
            throw new ExitException(
                $"No session found for '{generation.Harness}' session '{generation.SessionId}' "
                + $"at pid {generation.Pid} on this host. It may have ended, been reaped, "
                + "or never started.");
        }

        var previousBindingKind = row.BindingKind;
        var previousAgentName = row.AgentName;

        var (newBindingKind, resetLedger, changed) = (previousBindingKind, previousAgentName) switch
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
                    $"Session '{generation.SessionId}' is already explicitly claimed by "
                    + $"'{previousAgentName}'. Use --force-rebind to reclaim it as '{normalizedActor}'."),

            _ => (AgentSessionBindingKind.Explicit, true, true)
        };

        if (changed)
        {
            await connection.ExecuteAsync(
                "UPDATE agent_sessions SET agent_name = @agentName, binding_kind = @bindingKind "
                + "WHERE harness = @harness AND session_id = @sessionId "
                + "AND pid = @pid AND proc_start = @procStart AND host = @host",
                new
                {
                    agentName = normalizedActor,
                    bindingKind = newBindingKind,
                    harness = generation.Harness,
                    sessionId = generation.SessionId,
                    pid = generation.Pid,
                    procStart = generation.ProcStart,
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
                    + "AND pid = @pid AND proc_start = @procStart AND host = @host",
                    new
                    {
                        harness = generation.Harness,
                        sessionId = generation.SessionId,
                        pid = generation.Pid,
                        procStart = generation.ProcStart,
                        host = generation.Host,
                        cancellationToken
                    },
                    transaction);
            }
        }

        var updated = await connection.QueryFirstAsync<AgentSessionRow>(
            $"SELECT {AgentSessionRecord.Columns} FROM agent_sessions "
            + "WHERE harness = @harness AND session_id = @sessionId",
            new { harness = generation.Harness, sessionId = generation.SessionId, cancellationToken },
            transaction);

        await transaction.CommitAsync(cancellationToken);

        return new AgentSessionClaimResult(updated.ToRecord(), changed, previousBindingKind, previousAgentName);
    }

    public async Task<AgentSessionClaimResult> SelfClaimAsync(
        string actor,
        bool forceRebind,
        CancellationToken cancellationToken)
    {
        var ancestorSession = ancestorResolver.Resolve()
            ?? throw new ExitException(
                "Could not identify a Claude Code ancestor session for this process. "
                + "Self-claim is zero-config on Linux with Claude Code only; other harnesses and "
                + "platforms need a session row created by `nitro agent hook` first.");

        var procStart = processInfoProvider.GetStartTime(ancestorSession.Pid)
            ?? throw new ExitException(
                $"Process {ancestorSession.Pid} for the detected Claude Code session is no longer running.");

        var host = await ResolveHostAsync(cancellationToken);

        var workspacePath = AgentWorkspace.Find(fileSystem, ancestorSession.Cwd)
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        // This process's own cwd-resolved workspace must agree with the
        // ancestor session's workspace before any write happens: every write
        // below (including AgentRegistry.EnsureImplicitAsync) targets the
        // database ConnectAsync resolves from THIS process's cwd, so a
        // mismatch here would silently claim a session into the wrong
        // workspace's database.
        var cwdWorkspacePath = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory());

        if (cwdWorkspacePath is null || cwdWorkspacePath != workspacePath)
        {
            throw new ExitException(
                $"This process's workspace ('{cwdWorkspacePath ?? "none"}') does not match the "
                + $"Claude Code session's workspace ('{workspacePath}'). Run `nitro agent session "
                + "claim` from the session's workspace.");
        }

        var (endpointKind, endpointAddr) = EndpointAddress.IsValid(ancestorSession.Name)
            ? (AgentSessionEndpointKind.ClaudePeer, ancestorSession.Name)
            : (AgentSessionEndpointKind.None, string.Empty);

        var generation = new AgentSessionGeneration(
            AgentSessionHarness.ClaudeCode, ancestorSession.SessionId, host, ancestorSession.Pid, procStart);

        await StartAsync(
            generation, ancestorSession.Cwd, workspacePath, endpointKind, endpointAddr,
            envActor: null, cancellationToken);

        return await ClaimAsync(generation, actor, forceRebind, cancellationToken);
    }

    public async Task<bool> EndAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "DELETE FROM agent_sessions WHERE harness = @harness AND session_id = @sessionId "
            + "AND pid = @pid AND proc_start = @procStart AND host = @host";
        command.Parameters.AddWithValue("@harness", generation.Harness);
        command.Parameters.AddWithValue("@sessionId", generation.SessionId);
        command.Parameters.AddWithValue("@pid", generation.Pid);
        command.Parameters.AddWithValue("@procStart", generation.ProcStart);
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
            + "AND pid = @pid AND proc_start = @procStart AND host = @host";
        command.Parameters.AddWithValue("@harness", generation.Harness);
        command.Parameters.AddWithValue("@sessionId", generation.SessionId);
        command.Parameters.AddWithValue("@pid", generation.Pid);
        command.Parameters.AddWithValue("@procStart", generation.ProcStart);
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
            + "AND pid = @pid AND proc_start = @procStart AND host = @host";
        command.Parameters.AddWithValue("@harness", generation.Harness);
        command.Parameters.AddWithValue("@sessionId", generation.SessionId);
        command.Parameters.AddWithValue("@pid", generation.Pid);
        command.Parameters.AddWithValue("@procStart", generation.ProcStart);
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
            + "AND pid = @pid AND proc_start = @procStart AND host = @host";
        updateCommand.Parameters.AddWithValue("@harness", generation.Harness);
        updateCommand.Parameters.AddWithValue("@sessionId", generation.SessionId);
        updateCommand.Parameters.AddWithValue("@pid", generation.Pid);
        updateCommand.Parameters.AddWithValue("@procStart", generation.ProcStart);
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
            + "AND pid = @pid AND proc_start = @procStart AND host = @host";
        selectCommand.Parameters.AddWithValue("@harness", generation.Harness);
        selectCommand.Parameters.AddWithValue("@sessionId", generation.SessionId);
        selectCommand.Parameters.AddWithValue("@pid", generation.Pid);
        selectCommand.Parameters.AddWithValue("@procStart", generation.ProcStart);
        selectCommand.Parameters.AddWithValue("@host", generation.Host);

        var updated = (int)(long)(await selectCommand.ExecuteScalarAsync(cancellationToken))!;

        await transaction.CommitAsync(cancellationToken);

        return updated;
    }

    public async Task<IReadOnlyList<AgentSessionRecord>> ReapAsync(CancellationToken cancellationToken)
    {
        var host = await ResolveHostAsync(cancellationToken);

        await using var connection = await ConnectAsync(cancellationToken);

        var candidates = await connection.QueryAsync<AgentSessionRow>(
            $"SELECT {AgentSessionRecord.Columns} FROM agent_sessions WHERE host = @host",
            new { host, cancellationToken });

        var reaped = new List<AgentSessionRecord>();

        foreach (var candidate in candidates)
        {
            var record = candidate.ToRecord();

            if (processInfoProvider.IsAlive(record.Pid, record.ProcStart))
            {
                continue;
            }

            // The generation predicate guards a TOCTOU race: if the row was
            // reclaimed or restarted with a new generation between the
            // SELECT above and this DELETE, the WHERE clause matches
            // nothing and the newer generation survives untouched.
            var rowsAffected = await connection.ExecuteAsync(
                "DELETE FROM agent_sessions WHERE harness = @harness AND session_id = @sessionId "
                + "AND pid = @pid AND proc_start = @procStart AND host = @host",
                new
                {
                    harness = record.Harness,
                    sessionId = record.SessionId,
                    pid = record.Pid,
                    procStart = record.ProcStart,
                    host = record.Host,
                    cancellationToken
                });

            if (rowsAffected > 0)
            {
                reaped.Add(record);
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

            var state = record.Host != host
                ? AgentSessionState.Remote
                : record.EndpointKind == AgentSessionEndpointKind.None
                    ? AgentSessionState.Unreachable
                    : AgentSessionState.Online;

            return new AgentSessionView(record, state);
        }).ToList();
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
                AND pid = @pid AND proc_start = @procStart AND host = @host
                AND (last_ping_at IS NULL OR last_ping_at <= @cutoff);
            """,
            new
            {
                now,
                attemptId,
                harness = session.Harness,
                sessionId = session.SessionId,
                pid = session.Pid,
                procStart = session.ProcStart,
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

    private async Task<string> ResolveHostAsync(CancellationToken cancellationToken)
        => await instanceIdProvider.GetIdAsync(globalConfigDirectoryProvider.GetDirectory(), cancellationToken);

    private static bool IsSameGeneration(AgentSessionRow existing, AgentSessionGeneration generation)
        => existing.Pid == generation.Pid
            && existing.Host == generation.Host
            && DateTimeOffset.Parse(existing.ProcStart, CultureInfo.InvariantCulture) == generation.ProcStart;

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
        public required int Pid { get; init; }
        public required string ProcStart { get; init; }
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
            Pid = reader.GetInt32(reader.GetOrdinal("Pid")),
            ProcStart = reader.GetString(reader.GetOrdinal("ProcStart")),
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
                : reader.GetString(reader.GetOrdinal("LastPingDetail"))
        };

        public AgentSessionRecord ToRecord() => new()
        {
            Harness = Harness,
            SessionId = SessionId,
            AgentName = AgentName,
            BindingKind = BindingKind,
            Host = Host,
            Pid = Pid,
            ProcStart = DateTimeOffset.Parse(ProcStart, CultureInfo.InvariantCulture),
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
            LastPingDetail = LastPingDetail
        };
    }
}
