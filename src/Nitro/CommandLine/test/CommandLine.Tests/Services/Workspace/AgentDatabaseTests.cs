using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="AgentDatabase"/>'s version state machine and the
/// unified schema it applies, against a real SQLite file: empty/v0
/// initialization, v2-in-place upgrade preserving existing rows, a v3
/// database that predates the client column and the v4 session tables
/// gaining both in place, a v4 database gaining the v5 agent_sessions role,
/// harness_version, and process_scope columns in place without losing its
/// session, delivery, or bound-agent rows, a v5 database gaining the v6
/// agent_sessions proc_start_legacy column in place with every existing row
/// marked legacy (its proc_start predates raw start ticks) without losing
/// its proc_start value, v2/v3/v4/v5 connection rejection, unified-path v1
/// rejection, v7 (newer-than-current) rejection (including re-initializing
/// an already-current file, the shape --force reinit takes), the
/// agent_sessions table's foreign-key and cross-column CHECK constraints
/// under foreign_keys=ON, a v4 database whose last_ping_result CHECK
/// constraint predates 'unsupported' gaining the rebuilt constraint in
/// place without losing rows or its session_deliveries cascade (and every
/// row it carries forward marked proc_start_legacy since it predates raw
/// ticks too), and that task and mail data coexist in one file.
/// </summary>
public sealed class AgentDatabaseTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly AgentDatabase _database;

    public AgentDatabaseTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-agent-database-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _database = new AgentDatabase();
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task InitializeAsync_Should_CreateAllSchemasAndStampCurrentVersion_When_DatabaseIsNew()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // assert
        var version = await QueryScalarLongAsync(connection, "PRAGMA user_version;", cancellationToken);
        Assert.Equal(AgentDatabase.CurrentVersion, version);

        var taskTableCount = await QueryScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'tasks'",
            cancellationToken);
        var mailTableCount = await QueryScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'messages'",
            cancellationToken);
        var agentTableCount = await QueryScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'agents'",
            cancellationToken);
        Assert.Equal(1, taskTableCount);
        Assert.Equal(1, mailTableCount);
        Assert.Equal(1, agentTableCount);

        foreach (var sessionTable in new[]
        {
            "agent_sessions", "session_deliveries", "ping_leases",
            "mail_wake_outbox", "mail_wake_batches", "mail_wake_targets", "mail_wake_daemons",
            "session_ping_gates"
        })
        {
            var sessionTableCount = await QueryScalarLongAsync(
                connection,
                $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{sessionTable}'",
                cancellationToken);
            Assert.Equal(1, sessionTableCount);
        }

        var columns = (await QueryColumnNamesAsync(connection, "agents", cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("role", columns);
        Assert.Contains("implicit", columns);
        Assert.Contains("client", columns);

        var sessionColumns = (await QueryColumnNamesAsync(connection, "agent_sessions", cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("role", sessionColumns);
        Assert.Contains("harness_version", sessionColumns);
        Assert.Contains("process_scope", sessionColumns);
        Assert.Contains("proc_start_legacy", sessionColumns);
    }

    /// <summary>
    /// A row created fresh against the current schema defaults to
    /// <c>proc_start_legacy = 0</c> without any caller needing to set it
    /// explicitly, since a fresh row's <c>proc_start</c> is always raw
    /// ticks, never the pre-v6 legacy format.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_DefaultProcStartLegacyToFalse_When_RowIsFreshlyInserted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await InsertAgentSessionAsync(connection, "session-fresh-legacy", cancellationToken);

        // act
        var procStartLegacy = await QueryScalarLongAsync(
            connection,
            "SELECT proc_start_legacy FROM agent_sessions WHERE session_id = 'session-fresh-legacy'",
            cancellationToken);

        // assert
        Assert.Equal(0, procStartLegacy);
    }

    [Fact]
    public async Task InitializeAsync_Should_BeIdempotent_When_CalledAgainOnCurrentVersion()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }

        // act
        await using var second = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // assert
        var version = await QueryScalarLongAsync(second, "PRAGMA user_version;", cancellationToken);
        Assert.Equal(AgentDatabase.CurrentVersion, version);
    }

    /// <summary>
    /// Seeds a raw v2-shaped agents table, predating the role and implicit
    /// columns, with one row, mirroring a database left by a pre-.8 CLI.
    /// InitializeAsync must add the columns in place, without losing the
    /// row, and stamp the current version.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_UpgradeAgentsTableInPlace_When_ExistingVersionIsUpgradable()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = new SqliteConnection(
            $"Data Source={AgentWorkspace.GetDatabasePath(_workspaceDirectory)};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
            await ExecuteAsync(
                connection,
                """
                CREATE TABLE agents (
                    name TEXT PRIMARY KEY,
                    registered_at TEXT NOT NULL,
                    last_seen_at TEXT NOT NULL
                );
                INSERT INTO agents (name, registered_at, last_seen_at)
                VALUES ('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
                PRAGMA user_version = 2;
                """,
                cancellationToken);
        }

        // act
        await using var connection2 = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // assert
        var version = await QueryScalarLongAsync(connection2, "PRAGMA user_version;", cancellationToken);
        Assert.Equal(AgentDatabase.CurrentVersion, version);

        var name = await QueryScalarStringAsync(
            connection2, "SELECT name FROM agents WHERE name = 'claude'", cancellationToken);
        var role = await QueryScalarStringAsync(
            connection2, "SELECT role FROM agents WHERE name = 'claude'", cancellationToken);
        var isImplicit = await QueryScalarLongAsync(
            connection2, "SELECT implicit FROM agents WHERE name = 'claude'", cancellationToken);
        var client = await QueryScalarStringAsync(
            connection2, "SELECT client FROM agents WHERE name = 'claude'", cancellationToken);
        Assert.Equal("claude", name);
        Assert.Equal("", role);
        Assert.Equal(0, isImplicit);
        Assert.Equal("", client);

        var agentSessionsTableCount = await QueryScalarLongAsync(
            connection2,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'agent_sessions'",
            cancellationToken);
        Assert.Equal(1, agentSessionsTableCount);
    }

    /// <summary>
    /// Seeds a v3-shaped agents table (role and implicit present, client
    /// absent) with no session tables, mirroring an existing workspace at
    /// this bead's start, such as this repo's own `.nitro/agents/` before
    /// `init --force`. Column upgrades are never gated on
    /// version == a single UpgradableVersion, so a v3 database gains both
    /// the client column and the new v4 session tables in the same pass,
    /// and reads through <see cref="AgentRecord.Columns"/> succeed
    /// afterward instead of failing with "no such column: client".
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_AddClientColumn_When_ExistingVersionIsCurrentButPredatesClient()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = new SqliteConnection(
            $"Data Source={AgentWorkspace.GetDatabasePath(_workspaceDirectory)};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
            await ExecuteAsync(
                connection,
                """
                CREATE TABLE agents (
                    name TEXT PRIMARY KEY,
                    registered_at TEXT NOT NULL,
                    last_seen_at TEXT NOT NULL,
                    role TEXT NOT NULL DEFAULT '',
                    implicit INTEGER NOT NULL DEFAULT 0 CHECK (implicit IN (0, 1))
                );
                INSERT INTO agents (name, registered_at, last_seen_at, role, implicit)
                VALUES ('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', 'backend', 0);
                PRAGMA user_version = 3;
                """,
                cancellationToken);
        }

        // act
        await using var connection2 = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // assert
        var version = await QueryScalarLongAsync(connection2, "PRAGMA user_version;", cancellationToken);
        Assert.Equal(AgentDatabase.CurrentVersion, version);

        var columns = (await QueryColumnNamesAsync(connection2, "agents", cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("client", columns);

        var role = await QueryScalarStringAsync(
            connection2, "SELECT role FROM agents WHERE name = 'claude'", cancellationToken);
        var client = await QueryScalarStringAsync(
            connection2, "SELECT client FROM agents WHERE name = 'claude'", cancellationToken);
        Assert.Equal("backend", role);
        Assert.Equal("", client);

        var agentSessionsTableCount = await QueryScalarLongAsync(
            connection2,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'agent_sessions'",
            cancellationToken);
        Assert.Equal(1, agentSessionsTableCount);
    }

    /// <summary>
    /// Seeds a fully v4-shaped database, predating the v5
    /// <c>role</c>/<c>harness_version</c>/<c>process_scope</c> columns, with
    /// a populated <c>agent_sessions</c> row bound to a real agent and a
    /// cascading <c>session_deliveries</c> row, mirroring an existing
    /// workspace at this bead's start. InitializeAsync must add the three
    /// new columns in place, defaulted to the empty string, without losing
    /// the session row, the delivery ledger row, or the bound agent
    /// identity.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_UpgradeAgentSessionsMetadataColumns_When_ExistingVersionIsV4()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = new SqliteConnection(
            $"Data Source={AgentWorkspace.GetDatabasePath(_workspaceDirectory)};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
            await ExecuteAsync(connection, "PRAGMA foreign_keys = ON;", cancellationToken);
            await ExecuteAsync(
                connection,
                """
                CREATE TABLE agents (
                    name TEXT PRIMARY KEY,
                    registered_at TEXT NOT NULL,
                    last_seen_at TEXT NOT NULL,
                    role TEXT NOT NULL DEFAULT '',
                    implicit INTEGER NOT NULL DEFAULT 0 CHECK (implicit IN (0, 1)),
                    client TEXT NOT NULL DEFAULT ''
                );

                CREATE TABLE agent_sessions (
                    harness TEXT NOT NULL CHECK (harness IN ('claude-code', 'codex', 'copilot')),
                    session_id TEXT NOT NULL,
                    agent_name TEXT NULL REFERENCES agents (name),
                    binding_kind TEXT NOT NULL DEFAULT 'none' CHECK (binding_kind IN ('none', 'env', 'explicit')),
                    host TEXT NOT NULL,
                    pid INTEGER NOT NULL CHECK (pid > 0),
                    proc_start TEXT NOT NULL,
                    cwd TEXT NOT NULL,
                    workspace_path TEXT NOT NULL,
                    endpoint_kind TEXT NOT NULL CHECK (endpoint_kind IN ('claude-peer', 'codex-thread', 'copilot-extension', 'none')),
                    endpoint_addr TEXT NOT NULL,
                    started_at TEXT NOT NULL,
                    last_beat_at TEXT NOT NULL,
                    block_budget_used INTEGER NOT NULL DEFAULT 0 CHECK (block_budget_used >= 0),
                    last_ping_at TEXT NULL,
                    last_ping_attempt TEXT NULL,
                    last_ping_result TEXT NULL CHECK (last_ping_result IN ('ok', 'spawn-failed', 'endpoint-gone', 'timeout', 'capacity-dropped', 'error', 'unsupported') OR last_ping_result IS NULL),
                    last_ping_detail TEXT NULL CHECK (last_ping_detail IS NULL OR length(last_ping_detail) <= 200),
                    CHECK ((binding_kind = 'none') = (agent_name IS NULL)),
                    CHECK ((endpoint_kind = 'none') = (endpoint_addr = '')),
                    PRIMARY KEY (harness, session_id)
                );

                CREATE INDEX idx_agent_sessions_name ON agent_sessions (agent_name);
                CREATE INDEX idx_agent_sessions_pid ON agent_sessions (host, pid);

                CREATE TABLE session_deliveries (
                    harness TEXT NOT NULL,
                    session_id TEXT NOT NULL,
                    message_id TEXT NOT NULL,
                    channel TEXT NOT NULL CHECK (channel IN ('digest', 'gate', 'ping')),
                    delivered_at TEXT NOT NULL,
                    PRIMARY KEY (harness, session_id, message_id, channel),
                    FOREIGN KEY (harness, session_id)
                        REFERENCES agent_sessions (harness, session_id) ON DELETE CASCADE
                );

                CREATE TABLE ping_leases (
                    slot INTEGER PRIMARY KEY CHECK (slot BETWEEN 1 AND 4),
                    attempt_id TEXT NOT NULL,
                    acquired_at TEXT NOT NULL,
                    expires_at TEXT NOT NULL
                );

                INSERT INTO agents (name, registered_at, last_seen_at, role, implicit, client)
                VALUES ('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', 'backend', 0, 'claude-code');

                INSERT INTO agent_sessions (
                    harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                    cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
                ) VALUES (
                    'claude-code', 'session-v4', 'claude', 'explicit', 'host-a', 4242, '2026-01-10T12:00:00+00:00',
                    '/tmp/work', '/tmp/work/.nitro/agents', 'none', '', '2026-01-10T12:00:00+00:00',
                    '2026-01-10T12:00:00+00:00'
                );

                INSERT INTO session_deliveries (harness, session_id, message_id, channel, delivered_at)
                VALUES ('claude-code', 'session-v4', 'msg-1', 'digest', '2026-01-10T12:00:00+00:00');

                PRAGMA user_version = 4;
                """,
                cancellationToken);
        }

        // act
        await using var connection2 = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // assert
        var version = await QueryScalarLongAsync(connection2, "PRAGMA user_version;", cancellationToken);
        Assert.Equal(AgentDatabase.CurrentVersion, version);

        var sessionColumns = (await QueryColumnNamesAsync(connection2, "agent_sessions", cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("role", sessionColumns);
        Assert.Contains("harness_version", sessionColumns);
        Assert.Contains("process_scope", sessionColumns);

        var agentName = await QueryScalarStringAsync(
            connection2, "SELECT agent_name FROM agent_sessions WHERE session_id = 'session-v4'", cancellationToken);
        Assert.Equal("claude", agentName);

        var role = await QueryScalarStringAsync(
            connection2, "SELECT role FROM agent_sessions WHERE session_id = 'session-v4'", cancellationToken);
        var harnessVersion = await QueryScalarStringAsync(
            connection2,
            "SELECT harness_version FROM agent_sessions WHERE session_id = 'session-v4'",
            cancellationToken);
        var processScope = await QueryScalarStringAsync(
            connection2,
            "SELECT process_scope FROM agent_sessions WHERE session_id = 'session-v4'",
            cancellationToken);
        Assert.Equal("", role);
        Assert.Equal("", harnessVersion);
        Assert.Equal("", processScope);

        var deliveryCount = await QueryScalarLongAsync(
            connection2,
            "SELECT COUNT(*) FROM session_deliveries WHERE session_id = 'session-v4'",
            cancellationToken);
        Assert.Equal(1, deliveryCount);

        var agentRole = await QueryScalarStringAsync(
            connection2, "SELECT role FROM agents WHERE name = 'claude'", cancellationToken);
        Assert.Equal("backend", agentRole);
    }

    /// <summary>
    /// Seeds a fully v5-shaped database (role, harness_version, and
    /// process_scope already present), predating the v6
    /// <c>proc_start_legacy</c> column, with a populated <c>agent_sessions</c>
    /// row whose <c>proc_start</c> still carries the pre-v6 DateTimeOffset
    /// text (this is exactly what every real v5 row looks like, since raw
    /// ticks did not exist yet). InitializeAsync must add the column, mark
    /// that existing row legacy (it cannot be converted to ticks without the
    /// writing host's boot time), and leave its <c>proc_start</c> value
    /// completely untouched so the legacy wall-clock liveness rule keeps
    /// reading it correctly until the row's own next SessionStart rewrites
    /// it fresh.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_UpgradeAgentSessionsProcStartLegacyColumn_When_ExistingVersionIsV5()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = new SqliteConnection(
            $"Data Source={AgentWorkspace.GetDatabasePath(_workspaceDirectory)};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
            await ExecuteAsync(connection, "PRAGMA foreign_keys = ON;", cancellationToken);
            await ExecuteAsync(
                connection,
                """
                CREATE TABLE agents (
                    name TEXT PRIMARY KEY,
                    registered_at TEXT NOT NULL,
                    last_seen_at TEXT NOT NULL,
                    role TEXT NOT NULL DEFAULT '',
                    implicit INTEGER NOT NULL DEFAULT 0 CHECK (implicit IN (0, 1)),
                    client TEXT NOT NULL DEFAULT ''
                );

                CREATE TABLE agent_sessions (
                    harness TEXT NOT NULL CHECK (harness IN ('claude-code', 'codex', 'copilot')),
                    session_id TEXT NOT NULL,
                    agent_name TEXT NULL REFERENCES agents (name),
                    binding_kind TEXT NOT NULL DEFAULT 'none' CHECK (binding_kind IN ('none', 'env', 'explicit')),
                    host TEXT NOT NULL,
                    pid INTEGER NOT NULL CHECK (pid > 0),
                    proc_start TEXT NOT NULL,
                    cwd TEXT NOT NULL,
                    workspace_path TEXT NOT NULL,
                    endpoint_kind TEXT NOT NULL CHECK (endpoint_kind IN ('claude-peer', 'codex-thread', 'copilot-extension', 'none')),
                    endpoint_addr TEXT NOT NULL,
                    started_at TEXT NOT NULL,
                    last_beat_at TEXT NOT NULL,
                    block_budget_used INTEGER NOT NULL DEFAULT 0 CHECK (block_budget_used >= 0),
                    last_ping_at TEXT NULL,
                    last_ping_attempt TEXT NULL,
                    last_ping_result TEXT NULL CHECK (last_ping_result IN ('ok', 'spawn-failed', 'endpoint-gone', 'timeout', 'capacity-dropped', 'error', 'unsupported') OR last_ping_result IS NULL),
                    last_ping_detail TEXT NULL CHECK (last_ping_detail IS NULL OR length(last_ping_detail) <= 200),
                    role TEXT NOT NULL DEFAULT '',
                    harness_version TEXT NOT NULL DEFAULT '',
                    process_scope TEXT NOT NULL DEFAULT '',
                    CHECK ((binding_kind = 'none') = (agent_name IS NULL)),
                    CHECK ((endpoint_kind = 'none') = (endpoint_addr = '')),
                    PRIMARY KEY (harness, session_id)
                );

                CREATE INDEX idx_agent_sessions_name ON agent_sessions (agent_name);
                CREATE INDEX idx_agent_sessions_pid ON agent_sessions (host, pid);

                CREATE TABLE session_deliveries (
                    harness TEXT NOT NULL,
                    session_id TEXT NOT NULL,
                    message_id TEXT NOT NULL,
                    channel TEXT NOT NULL CHECK (channel IN ('digest', 'gate', 'ping')),
                    delivered_at TEXT NOT NULL,
                    PRIMARY KEY (harness, session_id, message_id, channel),
                    FOREIGN KEY (harness, session_id)
                        REFERENCES agent_sessions (harness, session_id) ON DELETE CASCADE
                );

                CREATE TABLE ping_leases (
                    slot INTEGER PRIMARY KEY CHECK (slot BETWEEN 1 AND 4),
                    attempt_id TEXT NOT NULL,
                    acquired_at TEXT NOT NULL,
                    expires_at TEXT NOT NULL
                );

                INSERT INTO agents (name, registered_at, last_seen_at, role, implicit, client)
                VALUES ('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', 'backend', 0, 'claude-code');

                INSERT INTO agent_sessions (
                    harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                    cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
                    role, harness_version, process_scope
                ) VALUES (
                    'claude-code', 'session-v5', 'claude', 'explicit', 'host-a', 4242, '2026-01-10T12:00:00.000000+00:00',
                    '/tmp/work', '/tmp/work/.nitro/agents', 'none', '', '2026-01-10T12:00:00+00:00',
                    '2026-01-10T12:00:00+00:00', 'backend', '1.2.3', 'pidns:4242'
                );

                INSERT INTO session_deliveries (harness, session_id, message_id, channel, delivered_at)
                VALUES ('claude-code', 'session-v5', 'msg-1', 'digest', '2026-01-10T12:00:00+00:00');

                PRAGMA user_version = 5;
                """,
                cancellationToken);
        }

        // act
        await using var connection2 = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // assert
        var version = await QueryScalarLongAsync(connection2, "PRAGMA user_version;", cancellationToken);
        Assert.Equal(AgentDatabase.CurrentVersion, version);

        var sessionColumns = (await QueryColumnNamesAsync(connection2, "agent_sessions", cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("proc_start_legacy", sessionColumns);

        var procStartLegacy = await QueryScalarLongAsync(
            connection2,
            "SELECT proc_start_legacy FROM agent_sessions WHERE session_id = 'session-v5'",
            cancellationToken);
        Assert.Equal(1, procStartLegacy);

        var procStart = await QueryScalarStringAsync(
            connection2, "SELECT proc_start FROM agent_sessions WHERE session_id = 'session-v5'", cancellationToken);
        Assert.Equal("2026-01-10T12:00:00.000000+00:00", procStart);

        var deliveryCount = await QueryScalarLongAsync(
            connection2,
            "SELECT COUNT(*) FROM session_deliveries WHERE session_id = 'session-v5'",
            cancellationToken);
        Assert.Equal(1, deliveryCount);
    }

    /// <summary>
    /// Seeds a v4 database whose <c>agent_sessions</c> table carries the
    /// original CHECK constraint on <c>last_ping_result</c>, from before
    /// <c>unsupported</c> was added to the enum, with one row already
    /// present and a <c>session_deliveries</c> row that cascades from it.
    /// SQLite cannot ALTER a CHECK constraint in place, so
    /// InitializeAsync must detect the stale constraint and rebuild the
    /// table: the existing row must survive, the new value must be
    /// writable afterward where it was rejected before, the cascade to
    /// session_deliveries must still fire, and the constraint must still
    /// reject a genuinely invalid value.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_RebuildAgentSessionsCheckConstraint_When_ExistingV4DatabasePredatesUnsupported()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = new SqliteConnection(
            $"Data Source={AgentWorkspace.GetDatabasePath(_workspaceDirectory)};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
            await ExecuteAsync(connection, "PRAGMA foreign_keys = ON;", cancellationToken);
            await ExecuteAsync(
                connection,
                """
                CREATE TABLE agents (
                    name TEXT PRIMARY KEY,
                    registered_at TEXT NOT NULL,
                    last_seen_at TEXT NOT NULL,
                    role TEXT NOT NULL DEFAULT '',
                    implicit INTEGER NOT NULL DEFAULT 0 CHECK (implicit IN (0, 1)),
                    client TEXT NOT NULL DEFAULT ''
                );

                CREATE TABLE agent_sessions (
                    harness TEXT NOT NULL CHECK (harness IN ('claude-code', 'codex', 'copilot')),
                    session_id TEXT NOT NULL,
                    agent_name TEXT NULL REFERENCES agents (name),
                    binding_kind TEXT NOT NULL DEFAULT 'none' CHECK (binding_kind IN ('none', 'env', 'explicit')),
                    host TEXT NOT NULL,
                    pid INTEGER NOT NULL CHECK (pid > 0),
                    proc_start TEXT NOT NULL,
                    cwd TEXT NOT NULL,
                    workspace_path TEXT NOT NULL,
                    endpoint_kind TEXT NOT NULL CHECK (endpoint_kind IN ('claude-peer', 'codex-thread', 'copilot-extension', 'none')),
                    endpoint_addr TEXT NOT NULL,
                    started_at TEXT NOT NULL,
                    last_beat_at TEXT NOT NULL,
                    block_budget_used INTEGER NOT NULL DEFAULT 0 CHECK (block_budget_used >= 0),
                    last_ping_at TEXT NULL,
                    last_ping_attempt TEXT NULL,
                    last_ping_result TEXT NULL CHECK (last_ping_result IN ('ok', 'spawn-failed', 'endpoint-gone', 'timeout', 'capacity-dropped', 'error') OR last_ping_result IS NULL),
                    last_ping_detail TEXT NULL CHECK (last_ping_detail IS NULL OR length(last_ping_detail) <= 200),
                    CHECK ((binding_kind = 'none') = (agent_name IS NULL)),
                    CHECK ((endpoint_kind = 'none') = (endpoint_addr = '')),
                    PRIMARY KEY (harness, session_id)
                );

                CREATE INDEX idx_agent_sessions_name ON agent_sessions (agent_name);
                CREATE INDEX idx_agent_sessions_pid ON agent_sessions (host, pid);

                CREATE TABLE session_deliveries (
                    harness TEXT NOT NULL,
                    session_id TEXT NOT NULL,
                    message_id TEXT NOT NULL,
                    channel TEXT NOT NULL CHECK (channel IN ('digest', 'gate', 'ping')),
                    delivered_at TEXT NOT NULL,
                    PRIMARY KEY (harness, session_id, message_id, channel),
                    FOREIGN KEY (harness, session_id)
                        REFERENCES agent_sessions (harness, session_id) ON DELETE CASCADE
                );

                CREATE TABLE ping_leases (
                    slot INTEGER PRIMARY KEY CHECK (slot BETWEEN 1 AND 4),
                    attempt_id TEXT NOT NULL,
                    acquired_at TEXT NOT NULL,
                    expires_at TEXT NOT NULL
                );

                INSERT INTO agent_sessions (
                    harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                    cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
                    last_ping_result
                ) VALUES (
                    'claude-code', 'session-old', NULL, 'none', 'host-a', 4242, '2026-01-10T12:00:00+00:00',
                    '/tmp/work', '/tmp/work/.nitro/agents', 'none', '', '2026-01-10T12:00:00+00:00',
                    '2026-01-10T12:00:00+00:00', 'ok'
                );

                INSERT INTO session_deliveries (harness, session_id, message_id, channel, delivered_at)
                VALUES ('claude-code', 'session-old', 'msg-1', 'digest', '2026-01-10T12:00:00+00:00');

                PRAGMA user_version = 4;
                """,
                cancellationToken);
        }

        // act
        await using var connection2 = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // assert: version unchanged (this was never a version-keyed gap), row and cascade-owned
        // delivery survive, and the constraint now accepts 'unsupported'.
        var version = await QueryScalarLongAsync(connection2, "PRAGMA user_version;", cancellationToken);
        Assert.Equal(AgentDatabase.CurrentVersion, version);

        var existingLastPingResult = await QueryScalarStringAsync(
            connection2,
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-old'",
            cancellationToken);
        Assert.Equal("ok", existingLastPingResult);

        // This row went through the CHECK-constraint rebuild, which predates
        // proc_start_legacy entirely, so it must come out marked legacy
        // rather than silently defaulting to "carries raw ticks".
        var procStartLegacy = await QueryScalarLongAsync(
            connection2,
            "SELECT proc_start_legacy FROM agent_sessions WHERE session_id = 'session-old'",
            cancellationToken);
        Assert.Equal(1, procStartLegacy);

        var deliveryCount = await QueryScalarLongAsync(
            connection2,
            "SELECT COUNT(*) FROM session_deliveries WHERE session_id = 'session-old'",
            cancellationToken);
        Assert.Equal(1, deliveryCount);

        await ExecuteAsync(
            connection2,
            "UPDATE agent_sessions SET last_ping_result = 'unsupported' WHERE session_id = 'session-old';",
            cancellationToken);
        var updatedLastPingResult = await QueryScalarStringAsync(
            connection2,
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-old'",
            cancellationToken);
        Assert.Equal("unsupported", updatedLastPingResult);

        // The FK to agent_sessions must still enforce (not left dangling by the rebuild):
        // cascading the delivery row still works, and a bad reference still fails.
        await ExecuteAsync(
            connection2, "DELETE FROM agent_sessions WHERE session_id = 'session-old';", cancellationToken);
        var deliveryCountAfterCascade = await QueryScalarLongAsync(
            connection2,
            "SELECT COUNT(*) FROM session_deliveries WHERE session_id = 'session-old'",
            cancellationToken);
        Assert.Equal(0, deliveryCountAfterCascade);

        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection2,
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
                last_ping_result
            ) VALUES (
                'claude-code', 'session-invalid', NULL, 'none', 'host-a', 4242, '2026-01-10T12:00:00+00:00',
                '/tmp/work', '/tmp/work/.nitro/agents', 'none', '', '2026-01-10T12:00:00+00:00',
                '2026-01-10T12:00:00+00:00', 'not-a-real-result'
            );
            """,
            cancellationToken));
    }

    /// <summary>
    /// A freshly created database is unaffected by the constraint-rebuild
    /// path: <see cref="AgentSessionSchema.Create"/> already carries
    /// <c>unsupported</c>, so InitializeAsync detects the current
    /// constraint and skips the rebuild, and the value is writable on the
    /// first attempt.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_AcceptUnsupportedLastPingResult_When_DatabaseIsFreshlyCreated()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await InsertAgentSessionAsync(connection, "session-fresh", cancellationToken);

        // act
        await ExecuteAsync(
            connection,
            "UPDATE agent_sessions SET last_ping_result = 'unsupported' WHERE session_id = 'session-fresh';",
            cancellationToken);

        // assert
        var lastPingResult = await QueryScalarStringAsync(
            connection,
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-fresh'",
            cancellationToken);
        Assert.Equal("unsupported", lastPingResult);
    }

    [Fact]
    public async Task InitializeAsync_Should_Throw_When_ExistingVersionIsGreaterThanCurrent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await StampVersionOnNewFileAsync(8, cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _database.InitializeAsync(_workspaceDirectory, cancellationToken));
    }

    /// <summary>
    /// Mirrors what a <c>--force</c> reinitialize does today: it calls the
    /// same InitializeAsync path unconditionally. A database newer than this
    /// CLI understands must still be rejected, force or not.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_Throw_When_ForceReinitializingAgainstVersion8()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection =
            await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
            await ExecuteAsync(connection, "PRAGMA user_version = 8;", cancellationToken);
        }

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _database.InitializeAsync(_workspaceDirectory, cancellationToken));
    }

    [Fact]
    public async Task InitializeAsync_Should_Throw_When_UnifiedPathHasVersion1()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await StampVersionOnNewFileAsync(1, cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _database.InitializeAsync(_workspaceDirectory, cancellationToken));
    }

    [Fact]
    public async Task ConnectAsync_Should_Succeed_When_VersionIsCurrent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }

        // act
        await using var connected = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);

        // assert
        var version = await QueryScalarLongAsync(connected, "PRAGMA user_version;", cancellationToken);
        Assert.Equal(AgentDatabase.CurrentVersion, version);
    }

    [Fact]
    public async Task ConnectAsync_Should_Throw_When_VersionIsZero()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = new SqliteConnection(
            $"Data Source={AgentWorkspace.GetDatabasePath(_workspaceDirectory)};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
        }

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _database.ConnectAsync(_workspaceDirectory, cancellationToken));
    }

    [Fact]
    public async Task ConnectAsync_Should_Throw_When_UnifiedPathHasVersion1()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await StampVersionOnNewFileAsync(1, cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _database.ConnectAsync(_workspaceDirectory, cancellationToken));
    }

    [Fact]
    public async Task ConnectAsync_Should_Throw_When_VersionIsGreaterThanCurrent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection =
            await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
            await ExecuteAsync(connection, "PRAGMA user_version = 8;", cancellationToken);
        }

        // act & assert
        var exception = await Assert.ThrowsAsync<ExitException>(
            () => _database.ConnectAsync(_workspaceDirectory, cancellationToken));
        Assert.Contains("newer version", exception.Message);
    }

    /// <summary>
    /// A v2, v3, v4, v5, or v6 database is only upgraded in place by
    /// <see cref="AgentDatabase.InitializeAsync"/>; connecting directly
    /// against it still requires exactly <see cref="AgentDatabase.CurrentVersion"/>.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task ConnectAsync_Should_Throw_When_VersionIsUpgradable(int upgradableVersion)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await StampVersionOnNewFileAsync(upgradableVersion, cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _database.ConnectAsync(_workspaceDirectory, cancellationToken));
    }

    /// <summary>
    /// Round trip against the real schema with foreign_keys=ON (verified
    /// explicitly): insert an unclaimed <c>agent_sessions</c> row (no agent,
    /// <c>binding_kind = 'none'</c>), then claim it by pointing
    /// <c>agent_name</c> at a real row in <c>agents</c> and flipping
    /// <c>binding_kind</c> to <c>'explicit'</c>. Also proves the FK actually
    /// enforces: claiming with a name that is not in <c>agents</c> fails.
    /// </summary>
    [Fact]
    public async Task AgentSessionsTable_Should_InsertUnclaimedRowThenClaimIt_When_ForeignKeysAreOn()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        var foreignKeysEnabled = await QueryScalarLongAsync(connection, "PRAGMA foreign_keys;", cancellationToken);
        Assert.Equal(1, foreignKeysEnabled);

        await ExecuteAsync(
            connection,
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'claude-code', 'session-1', NULL, 'none', 'host-a', 4242, '2026-01-10T12:00:00+00:00',
                '/tmp/work', '/tmp/work/.nitro/agents', 'none', '', '2026-01-10T12:00:00+00:00',
                '2026-01-10T12:00:00+00:00'
            );
            """,
            cancellationToken);

        // act: the row starts unclaimed.
        var unclaimedAgentName = await QueryScalarStringAsync(
            connection, "SELECT agent_name FROM agent_sessions WHERE session_id = 'session-1'", cancellationToken);
        var unclaimedBindingKind = await QueryScalarStringAsync(
            connection, "SELECT binding_kind FROM agent_sessions WHERE session_id = 'session-1'", cancellationToken);
        Assert.Null(unclaimedAgentName);
        Assert.Equal("none", unclaimedBindingKind);

        // Claiming with an unregistered agent must fail the FK check.
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            "UPDATE agent_sessions SET agent_name = 'ghost', binding_kind = 'explicit' "
            + "WHERE session_id = 'session-1';",
            cancellationToken));

        await ExecuteAsync(
            connection,
            "INSERT INTO agents (name, registered_at, last_seen_at) VALUES "
            + "('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');",
            cancellationToken);

        await ExecuteAsync(
            connection,
            "UPDATE agent_sessions SET agent_name = 'claude', binding_kind = 'explicit' "
            + "WHERE session_id = 'session-1';",
            cancellationToken);

        // assert: claiming against a real agent succeeds.
        var claimedAgentName = await QueryScalarStringAsync(
            connection, "SELECT agent_name FROM agent_sessions WHERE session_id = 'session-1'", cancellationToken);
        var claimedBindingKind = await QueryScalarStringAsync(
            connection, "SELECT binding_kind FROM agent_sessions WHERE session_id = 'session-1'", cancellationToken);
        Assert.Equal("claude", claimedAgentName);
        Assert.Equal("explicit", claimedBindingKind);
    }

    /// <summary>
    /// The cross-column CHECK <c>(binding_kind = 'none') = (agent_name IS NULL)</c>
    /// rejects a row that claims to be bound (<c>binding_kind = 'explicit'</c>)
    /// while carrying no agent name.
    /// </summary>
    [Fact]
    public async Task AgentSessionsTable_Should_RejectMismatchedBindingKindAndAgentName_When_CheckConstraintFires()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'claude-code', 'session-2', NULL, 'explicit', 'host-a', 4242, '2026-01-10T12:00:00+00:00',
                '/tmp/work', '/tmp/work/.nitro/agents', 'none', '', '2026-01-10T12:00:00+00:00',
                '2026-01-10T12:00:00+00:00'
            );
            """,
            cancellationToken));
    }

    /// <summary>
    /// The cross-column CHECK <c>(endpoint_kind = 'none') = (endpoint_addr = '')</c>
    /// rejects a row that names a real endpoint kind while carrying no
    /// address.
    /// </summary>
    [Fact]
    public async Task AgentSessionsTable_Should_RejectActiveEndpointKindWithEmptyAddress_When_CheckConstraintFires()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'claude-code', 'session-3', NULL, 'none', 'host-a', 4242, '2026-01-10T12:00:00+00:00',
                '/tmp/work', '/tmp/work/.nitro/agents', 'claude-peer', '', '2026-01-10T12:00:00+00:00',
                '2026-01-10T12:00:00+00:00'
            );
            """,
            cancellationToken));
    }

    /// <summary>
    /// The same cross-column CHECK also rejects the opposite mismatch: a
    /// non-empty address while <c>endpoint_kind</c> claims there is none.
    /// </summary>
    [Fact]
    public async Task AgentSessionsTable_Should_RejectNoneEndpointKindWithNonEmptyAddress_When_CheckConstraintFires()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'claude-code', 'session-4', NULL, 'none', 'host-a', 4242, '2026-01-10T12:00:00+00:00',
                '/tmp/work', '/tmp/work/.nitro/agents', 'none', 'peer-a', '2026-01-10T12:00:00+00:00',
                '2026-01-10T12:00:00+00:00'
            );
            """,
            cancellationToken));
    }

    /// <summary>
    /// <c>session_deliveries.channel</c> is CHECK-constrained to
    /// <c>digest</c>, <c>gate</c>, or <c>ping</c>.
    /// </summary>
    [Fact]
    public async Task SessionDeliveriesTable_Should_RejectUnknownChannel_When_CheckConstraintFires()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await InsertAgentSessionAsync(connection, "session-5", cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO session_deliveries (harness, session_id, message_id, channel, delivered_at)
            VALUES ('claude-code', 'session-5', 'msg-1', 'unknown-channel', '2026-01-10T12:00:00+00:00');
            """,
            cancellationToken));
    }

    /// <summary>
    /// <c>session_deliveries</c> rows cascade-delete with their owning
    /// <c>agent_sessions</c> row, so reaping or ending a session clears its
    /// ledger in the same statement instead of leaking orphaned rows.
    /// </summary>
    [Fact]
    public async Task SessionDeliveriesTable_Should_CascadeDelete_When_OwningAgentSessionRowIsDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await InsertAgentSessionAsync(connection, "session-6", cancellationToken);
        await ExecuteAsync(
            connection,
            """
            INSERT INTO session_deliveries (harness, session_id, message_id, channel, delivered_at)
            VALUES ('claude-code', 'session-6', 'msg-1', 'digest', '2026-01-10T12:00:00+00:00');
            """,
            cancellationToken);

        // act
        await ExecuteAsync(
            connection, "DELETE FROM agent_sessions WHERE session_id = 'session-6';", cancellationToken);

        // assert
        var remaining = await QueryScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM session_deliveries WHERE session_id = 'session-6'",
            cancellationToken);
        Assert.Equal(0, remaining);
    }

    /// <summary>
    /// <c>ping_leases.slot</c> is CHECK-constrained to the fixed four-slot
    /// concurrency cap; 0 and 5 both fall outside it.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public async Task PingLeasesTable_Should_RejectSlotOutsideRange_When_CheckConstraintFires(int slot)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            $"""
            INSERT INTO ping_leases (slot, attempt_id, acquired_at, expires_at)
            VALUES ({slot}, 'attempt-1', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:05+00:00');
            """,
            cancellationToken));
    }

    /// <summary>
    /// The inclusive ends of the range, 1 and 4, are accepted.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task PingLeasesTable_Should_AcceptSlotWithinRange_When_AtTheInclusiveBounds(int slot)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // act
        await ExecuteAsync(
            connection,
            $"""
            INSERT INTO ping_leases (slot, attempt_id, acquired_at, expires_at)
            VALUES ({slot}, 'attempt-1', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:05+00:00');
            """,
            cancellationToken);

        // assert
        var count = await QueryScalarLongAsync(
            connection, $"SELECT COUNT(*) FROM ping_leases WHERE slot = {slot}", cancellationToken);
        Assert.Equal(1, count);
    }

    /// <summary>
    /// Proves the unified workspace: a task created through
    /// <see cref="ITaskStore"/> and a message sent through
    /// <see cref="IMailStore"/> land in the same database file, both visible
    /// afterward, with no separate discovery walk or database per feature.
    /// </summary>
    [Fact]
    public async Task TaskAndMail_Should_ShareOneDatabaseFile_When_UsingTheUnifiedWorkspace()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
        var fileSystem = new TestFileSystem(_tempRoot.FullName);
        var taskStore = new TaskStore(fileSystem, timeProvider, _database);
        var agentRegistry = new AgentRegistry(fileSystem, timeProvider, _database);
        var mailStore = new MailStore(fileSystem, timeProvider, _database, agentRegistry);

        await taskStore.InitializeWorkspaceAsync(_workspaceDirectory, "acme", cancellationToken);

        // act
        var creationResult = await taskStore.CreateTaskAsync(
            new TaskCreation
            {
                Title = "Ship the merge",
                Priority = 2,
                Type = TaskTypes.Task,
                Actor = "claude"
            },
            cancellationToken);
        await agentRegistry.RegisterAsync("codex", role: "", client: "", cancellationToken);
        var message = await mailStore.SendMessageAsync(
            new MailMessageCreation
            {
                Sender = "claude",
                Subject = "Status",
                Body = "Merged.",
                To = ["codex"]
            },
            cancellationToken);

        // assert
        var task = await taskStore.GetRequiredTaskAsync(creationResult.Id, cancellationToken);
        Assert.Equal("Ship the merge", task.Title);

        var storedMessage = await mailStore.GetRequiredMessageAsync(message.Id, cancellationToken);
        Assert.Equal("Status", storedMessage.Subject);

        await using var connection = new SqliteConnection(
            $"Data Source={AgentWorkspace.GetDatabasePath(_workspaceDirectory)};Pooling=False");
        await connection.OpenAsync(cancellationToken);
        var taskCount = await QueryScalarLongAsync(connection, "SELECT COUNT(*) FROM tasks", cancellationToken);
        var messageCount =
            await QueryScalarLongAsync(connection, "SELECT COUNT(*) FROM messages", cancellationToken);
        Assert.Equal(1, taskCount);
        Assert.Equal(1, messageCount);
    }

    /// <summary>
    /// Seeds a fully v6-shaped database (every table through
    /// <c>proc_start_legacy</c>, none of the v7 mail-wake or
    /// session-ping-gate tables) with one row in each of agents, messages,
    /// message_recipients, agent_sessions, session_deliveries, and
    /// ping_leases, mirroring a real workspace at this bead's start (fo9's
    /// schema v6, merged). InitializeAsync must add every v7 table without
    /// losing any of those rows, and stamp the current version.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_AddMailWakeAndSessionPingGateTables_When_ExistingVersionIsV6()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = new SqliteConnection(
            $"Data Source={AgentWorkspace.GetDatabasePath(_workspaceDirectory)};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
            await ExecuteAsync(connection, "PRAGMA foreign_keys = ON;", cancellationToken);
            await ExecuteAsync(connection, TaskStoreSchema.Create, cancellationToken);
            await ExecuteAsync(connection, AgentRegistrySchema.Create, cancellationToken);
            await ExecuteAsync(connection, MailStoreSchema.Create, cancellationToken);
            await ExecuteAsync(connection, AgentSessionSchema.Create, cancellationToken);

            await ExecuteAsync(
                connection,
                """
                INSERT INTO agents (name, registered_at, last_seen_at, role, implicit, client)
                VALUES ('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', 'backend', 0, 'claude-code'),
                       ('codex', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '', 0, 'codex');

                INSERT INTO messages (id, thread_id, sender, subject, body, created_at)
                VALUES ('msg-v6', 'thread-v6', 'claude', 'Status', 'Merged.', '2026-01-10T12:00:00+00:00');

                INSERT INTO message_recipients (message_id, recipient, kind, ordinal)
                VALUES ('msg-v6', 'codex', 'to', 0);

                INSERT INTO agent_sessions (
                    harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                    cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
                    role, harness_version, process_scope, proc_start_legacy
                ) VALUES (
                    'claude-code', 'session-v6', 'claude', 'explicit', 'host-a', 4242, '123456',
                    '/tmp/work', '/tmp/work/.nitro/agents', 'none', '', '2026-01-10T12:00:00+00:00',
                    '2026-01-10T12:00:00+00:00', 'backend', '1.2.3', 'pidns:4242', 0
                );

                INSERT INTO session_deliveries (harness, session_id, message_id, channel, delivered_at)
                VALUES ('claude-code', 'session-v6', 'msg-v6', 'digest', '2026-01-10T12:00:00+00:00');

                INSERT INTO ping_leases (slot, attempt_id, acquired_at, expires_at)
                VALUES (1, 'attempt-v6', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:05+00:00');

                PRAGMA user_version = 6;
                """,
                cancellationToken);
        }

        // act
        await using var connection2 = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // assert
        var version = await QueryScalarLongAsync(connection2, "PRAGMA user_version;", cancellationToken);
        Assert.Equal(AgentDatabase.CurrentVersion, version);

        foreach (var newTable in new[]
        {
            "mail_wake_outbox", "mail_wake_batches", "mail_wake_targets", "mail_wake_daemons",
            "session_ping_gates"
        })
        {
            var tableCount = await QueryScalarLongAsync(
                connection2,
                $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{newTable}'",
                cancellationToken);
            Assert.Equal(1, tableCount);
        }

        var agentCount = await QueryScalarLongAsync(connection2, "SELECT COUNT(*) FROM agents", cancellationToken);
        var messageCount =
            await QueryScalarLongAsync(connection2, "SELECT COUNT(*) FROM messages", cancellationToken);
        var recipientCount =
            await QueryScalarLongAsync(connection2, "SELECT COUNT(*) FROM message_recipients", cancellationToken);
        var sessionCount =
            await QueryScalarLongAsync(connection2, "SELECT COUNT(*) FROM agent_sessions", cancellationToken);
        var deliveryCount =
            await QueryScalarLongAsync(connection2, "SELECT COUNT(*) FROM session_deliveries", cancellationToken);
        var leaseCount = await QueryScalarLongAsync(connection2, "SELECT COUNT(*) FROM ping_leases", cancellationToken);
        Assert.Equal(2, agentCount);
        Assert.Equal(1, messageCount);
        Assert.Equal(1, recipientCount);
        Assert.Equal(1, sessionCount);
        Assert.Equal(1, deliveryCount);
        Assert.Equal(1, leaseCount);
    }

    /// <summary>
    /// <c>mail_wake_outbox.settled_generation</c> can never exceed
    /// <c>requested_generation</c>.
    /// </summary>
    [Fact]
    public async Task MailWakeOutboxTable_Should_RejectSettledGenerationAboveRequested_When_CheckConstraintFires()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await ExecuteAsync(
            connection,
            "INSERT INTO agents (name, registered_at, last_seen_at) VALUES "
            + "('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');",
            cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO mail_wake_outbox (nitro_instance_id, actor, requested_generation, settled_generation, due_at, updated_at)
            VALUES ('instance-a', 'claude', 1, 2, '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            """,
            cancellationToken));
    }

    /// <summary>
    /// At most one <c>active</c> <c>mail_wake_batches</c> row exists per
    /// (nitro_instance_id, actor), enforced by
    /// <c>idx_mail_wake_batches_one_active_per_actor</c>.
    /// </summary>
    [Fact]
    public async Task MailWakeBatchesTable_Should_RejectSecondActiveBatch_When_UniqueIndexFires()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await ExecuteAsync(
            connection,
            """
            INSERT INTO agents (name, registered_at, last_seen_at) VALUES
                ('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            INSERT INTO mail_wake_outbox (nitro_instance_id, actor, requested_generation, settled_generation, due_at, updated_at)
            VALUES ('instance-a', 'claude', 1, 0, '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            INSERT INTO mail_wake_batches (
                batch_id, nitro_instance_id, actor, claimed_generation, owner_id, attempt_id,
                status, claimed_at, expires_at
            ) VALUES (
                'batch-1', 'instance-a', 'claude', 1, 'owner-1', 'attempt-1',
                'active', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:30+00:00'
            );
            """,
            cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO mail_wake_batches (
                batch_id, nitro_instance_id, actor, claimed_generation, owner_id, attempt_id,
                status, claimed_at, expires_at
            ) VALUES (
                'batch-2', 'instance-a', 'claude', 1, 'owner-2', 'attempt-2',
                'active', '2026-01-10T12:00:01+00:00', '2026-01-10T12:00:31+00:00'
            );
            """,
            cancellationToken));
    }

    /// <summary>
    /// <c>mail_wake_targets</c> rows cascade-delete with their owning
    /// <c>mail_wake_batches</c> row, but a target row's own generation
    /// columns carry no foreign key against <c>agent_sessions</c>: deleting
    /// the live session row must never touch the durable target row.
    /// </summary>
    [Fact]
    public async Task MailWakeTargetsTable_Should_SurviveAgentSessionDeletion_When_OwningBatchStillExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await InsertAgentSessionAsync(connection, "session-target", cancellationToken);
        await ExecuteAsync(
            connection,
            """
            INSERT INTO agents (name, registered_at, last_seen_at) VALUES
                ('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            INSERT INTO mail_wake_outbox (nitro_instance_id, actor, requested_generation, settled_generation, due_at, updated_at)
            VALUES ('instance-a', 'claude', 1, 0, '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            INSERT INTO mail_wake_batches (
                batch_id, nitro_instance_id, actor, claimed_generation, owner_id, attempt_id,
                status, claimed_at, expires_at
            ) VALUES (
                'batch-target', 'instance-a', 'claude', 1, 'owner-1', 'attempt-1',
                'active', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:30+00:00'
            );
            INSERT INTO mail_wake_targets (batch_id, harness, session_id, host, pid, proc_start, status, updated_at)
            VALUES ('batch-target', 'claude-code', 'session-target', 'host-a', 4242, '2026-01-10T12:00:00+00:00',
                    'pending', '2026-01-10T12:00:00+00:00');
            """,
            cancellationToken);

        // act
        await ExecuteAsync(
            connection, "DELETE FROM agent_sessions WHERE session_id = 'session-target';", cancellationToken);

        // assert
        var targetCount = await QueryScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM mail_wake_targets WHERE batch_id = 'batch-target'",
            cancellationToken);
        Assert.Equal(1, targetCount);
    }

    /// <summary>
    /// <c>mail_wake_daemons.epoch</c> must be at least 1.
    /// </summary>
    [Fact]
    public async Task MailWakeDaemonsTable_Should_RejectEpochBelowOne_When_CheckConstraintFires()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO mail_wake_daemons (nitro_instance_id, owner_id, epoch, leased_at, expires_at)
            VALUES ('instance-a', 'owner-1', 0, '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:30+00:00');
            """,
            cancellationToken));
    }

    /// <summary>
    /// <c>session_ping_gates</c> is keyed by the full session generation
    /// (harness, session_id, host, pid, proc_start), independent of
    /// <c>agent_sessions</c>: no foreign key ties the two, so a gate row
    /// survives the session it names being deleted.
    /// </summary>
    [Fact]
    public async Task SessionPingGatesTable_Should_SurviveAgentSessionDeletion_When_NoForeignKeyTiesThem()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await InsertAgentSessionAsync(connection, "session-gate", cancellationToken);
        await ExecuteAsync(
            connection,
            """
            INSERT INTO session_ping_gates (harness, session_id, host, pid, proc_start, attempt_id, acquired_at, expires_at)
            VALUES ('claude-code', 'session-gate', 'host-a', 4242, '2026-01-10T12:00:00+00:00',
                    'attempt-1', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:30+00:00');
            """,
            cancellationToken);

        // act
        await ExecuteAsync(
            connection, "DELETE FROM agent_sessions WHERE session_id = 'session-gate';", cancellationToken);

        // assert
        var gateCount = await QueryScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM session_ping_gates WHERE session_id = 'session-gate'",
            cancellationToken);
        Assert.Equal(1, gateCount);
    }

    /// <summary>
    /// The full session generation is exactly the primary key: a second row
    /// naming the same (harness, session_id, host, pid, proc_start) is
    /// rejected, the schema-level half of the mutual exclusion
    /// <see cref="ISessionPingGateStore"/> provides at the store level.
    /// </summary>
    [Fact]
    public async Task SessionPingGatesTable_Should_RejectDuplicateGeneration_When_PrimaryKeyFires()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await ExecuteAsync(
            connection,
            """
            INSERT INTO session_ping_gates (harness, session_id, host, pid, proc_start, attempt_id, acquired_at, expires_at)
            VALUES ('claude-code', 'session-dup', 'host-a', 4242, '2026-01-10T12:00:00+00:00',
                    'attempt-1', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:30+00:00');
            """,
            cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO session_ping_gates (harness, session_id, host, pid, proc_start, attempt_id, acquired_at, expires_at)
            VALUES ('claude-code', 'session-dup', 'host-a', 4242, '2026-01-10T12:00:00+00:00',
                    'attempt-2', '2026-01-10T12:00:01+00:00', '2026-01-10T12:00:31+00:00');
            """,
            cancellationToken));
    }

    /// <summary>
    /// Writes a database file that carries only a stamped PRAGMA
    /// user_version, no schema, mirroring a raw file at the unified path
    /// with a version this bead's state machine must reject before touching
    /// DDL.
    /// </summary>
    private async Task StampVersionOnNewFileAsync(int version, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(
            $"Data Source={AgentWorkspace.GetDatabasePath(_workspaceDirectory)};Pooling=False");
        await connection.OpenAsync(cancellationToken);
        await ExecuteAsync(connection, $"PRAGMA user_version = {version};", cancellationToken);
    }

    private static async Task<long> QueryScalarLongAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt64(result);
    }

    private static async Task<string?> QueryScalarStringAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is null or DBNull ? null : result.ToString();
    }

    private static async Task<List<string>> QueryColumnNamesAsync(
        SqliteConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT name FROM pragma_table_info('{tableName}');";

        var names = new List<string>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts a minimal, otherwise-valid <c>agent_sessions</c> row with the
    /// given session id, for tests that only care about a dependent table.
    /// </summary>
    private static Task InsertAgentSessionAsync(
        SqliteConnection connection, string sessionId, CancellationToken cancellationToken)
        => ExecuteAsync(
            connection,
            $"""
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'claude-code', '{sessionId}', NULL, 'none', 'host-a', 4242, '2026-01-10T12:00:00+00:00',
                '/tmp/work', '/tmp/work/.nitro/agents', 'none', '', '2026-01-10T12:00:00+00:00',
                '2026-01-10T12:00:00+00:00'
            );
            """,
            cancellationToken);
}
