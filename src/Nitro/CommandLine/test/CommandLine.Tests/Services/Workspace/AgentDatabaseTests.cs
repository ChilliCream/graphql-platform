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
/// gaining both in place, v4 connection, unified-path v1 rejection, v5
/// (newer-than-current) rejection (including re-initializing an
/// already-current file, the shape --force reinit takes), the agent_sessions
/// table's foreign-key and cross-column CHECK constraints under
/// foreign_keys=ON, and that task and mail data coexist in one file.
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

        foreach (var sessionTable in new[] { "agent_sessions", "session_deliveries", "ping_leases" })
        {
            var sessionTableCount = await QueryScalarLongAsync(
                connection,
                $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{sessionTable}'",
                cancellationToken);
            Assert.Equal(1, sessionTableCount);
        }

        var columns = (await QueryColumnNamesAsync(connection, cancellationToken)).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("role", columns);
        Assert.Contains("implicit", columns);
        Assert.Contains("client", columns);
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

        var columns = (await QueryColumnNamesAsync(connection2, cancellationToken)).ToHashSet(StringComparer.Ordinal);
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

    [Fact]
    public async Task InitializeAsync_Should_Throw_When_ExistingVersionIsGreaterThanCurrent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await StampVersionOnNewFileAsync(5, cancellationToken);

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
    public async Task InitializeAsync_Should_Throw_When_ForceReinitializingAgainstVersion5()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection =
            await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
            await ExecuteAsync(connection, "PRAGMA user_version = 5;", cancellationToken);
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
            await ExecuteAsync(connection, "PRAGMA user_version = 5;", cancellationToken);
        }

        // act & assert
        var exception = await Assert.ThrowsAsync<ExitException>(
            () => _database.ConnectAsync(_workspaceDirectory, cancellationToken));
        Assert.Contains("newer version", exception.Message);
    }

    /// <summary>
    /// A v2 or v3 database is only upgraded in place by
    /// <see cref="AgentDatabase.InitializeAsync"/>; connecting directly
    /// against it still requires exactly <see cref="AgentDatabase.CurrentVersion"/>.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
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
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM pragma_table_info('agents');";

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
