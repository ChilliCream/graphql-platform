using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Tests <see cref="AgentDatabase"/> initialization, supported schema upgrades,
/// version rejection, and schema constraints against real SQLite files.
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
            "ping_leases", "agent_deliveries", "agent_ping_gates",
            "mail_wake_outbox", "mail_wake_batches", "mail_wake_targets", "mail_wake_daemons"
        })
        {
            var sessionTableCount = await QueryScalarLongAsync(
                connection,
                $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{sessionTable}'",
                cancellationToken);
            Assert.Equal(1, sessionTableCount);
        }

        foreach (var index in new[]
        {
            "idx_mail_wake_outbox_due", "idx_mail_wake_batches_one_active_per_actor",
            "idx_mail_wake_batches_expires", "idx_agent_ping_gates_expires"
        })
        {
            var indexCount = await QueryScalarLongAsync(
                connection,
                $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = '{index}'",
                cancellationToken);
            Assert.Equal(1, indexCount);
        }

        var columns = await QueryColumnNamesAsync(connection, "agents", cancellationToken);
        Assert.Equal(
            [
                "name", "role", "harness", "harness_version", "session_id", "cwd", "workspace_path",
                "registered_at", "started_at", "last_seen_at", "ended_at", "deleted_at",
                "endpoint_kind", "endpoint_addr", "endpoint_secret", "block_budget_used",
                "last_ping_at", "last_ping_attempt", "last_ping_result", "last_ping_detail",
                "announcement_pending", "idle_push_armed"
            ],
            columns);
    }

    /// <summary>
    /// Seeds a raw v14-shaped database: the old six-column <c>agents</c> table, a
    /// task, and a piece of mail sent by the seeded agent. InitializeAsync must
    /// wipe the agent-domain tables, lay the new <c>agents</c> columns, and leave
    /// tasks and mail untouched.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_WipeAgentDomainTablesAndPreserveTasksAndMail_When_ExistingVersionIsV14()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = new SqliteConnection(
            $"Data Source={AgentWorkspace.GetDatabasePath(_workspaceDirectory)};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
            await ExecuteAsync(connection, "PRAGMA foreign_keys = ON;", cancellationToken);
            await ExecuteAsync(connection, TaskStoreSchema.Create, cancellationToken);
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
                """,
                cancellationToken);
            await ExecuteAsync(connection, MailStoreSchema.Create, cancellationToken);

            await ExecuteAsync(
                connection,
                """
                INSERT INTO tasks (id, title, created_at, updated_at)
                VALUES ('task-1', 'Keep me', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');

                INSERT INTO agents (name, registered_at, last_seen_at, role, implicit, client)
                VALUES ('maya', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', 'backend', 0, 'claude-code');

                INSERT INTO messages (id, thread_id, sender, subject, body, created_at)
                VALUES ('mail-1', 'mail-1', 'maya', 'Old mail', 'Keep me too', '2026-01-10T12:00:00+00:00');
                INSERT INTO message_recipients (message_id, recipient, ordinal)
                VALUES ('mail-1', 'maya', 0);

                PRAGMA user_version = 14;
                """,
                cancellationToken);
        }

        // act
        await using var upgraded = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // assert
        var state = new
        {
            Version = await QueryScalarLongAsync(upgraded, "PRAGMA user_version", cancellationToken),
            Tasks = await QueryScalarLongAsync(upgraded, "SELECT COUNT(*) FROM tasks", cancellationToken),
            Messages = await QueryScalarLongAsync(upgraded, "SELECT COUNT(*) FROM messages", cancellationToken),
            MessageRecipients =
                await QueryScalarLongAsync(upgraded, "SELECT COUNT(*) FROM message_recipients", cancellationToken),
            Agents = await QueryScalarLongAsync(upgraded, "SELECT COUNT(*) FROM agents", cancellationToken),
            AgentColumns = await QueryColumnNamesAsync(upgraded, "agents", cancellationToken)
        };

        state.MatchInlineSnapshot(
            """
            {
              "Version": 18,
              "Tasks": 1,
              "Messages": 1,
              "MessageRecipients": 1,
              "Agents": 0,
              "AgentColumns": [
                "name",
                "role",
                "harness",
                "harness_version",
                "session_id",
                "cwd",
                "workspace_path",
                "registered_at",
                "started_at",
                "last_seen_at",
                "ended_at",
                "deleted_at",
                "endpoint_kind",
                "endpoint_addr",
                "endpoint_secret",
                "block_budget_used",
                "last_ping_at",
                "last_ping_attempt",
                "last_ping_result",
                "last_ping_detail",
                "announcement_pending",
                "idle_push_armed"
              ]
            }
            """);
    }

    /// <summary>
    /// Seeds a raw v15-shaped database: mail tables still built with the old
    /// <c>REFERENCES agents (name)</c> constraints, holding a message from an
    /// agent that no longer exists (what the agent-domain rebuild that runs
    /// on every schema bump used to leave behind as a foreign key orphan).
    /// InitializeAsync must rebuild the mail tables without those foreign
    /// keys, and the pre-existing message and its read state must survive
    /// untouched.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_RemoveMailForeignKeysAndPreserveOrphanedMail_When_ExistingVersionIsV15()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = new SqliteConnection(
            $"Data Source={AgentWorkspace.GetDatabasePath(_workspaceDirectory)};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
            await ExecuteAsync(connection, "PRAGMA foreign_keys = OFF;", cancellationToken);
            await ExecuteAsync(
                connection,
                """
                CREATE TABLE messages (
                    id TEXT PRIMARY KEY,
                    thread_id TEXT NOT NULL,
                    in_reply_to TEXT REFERENCES messages (id),
                    sender TEXT NOT NULL REFERENCES agents (name),
                    subject TEXT NOT NULL CHECK (length(subject) BETWEEN 1 AND 500),
                    body TEXT NOT NULL,
                    created_at TEXT NOT NULL
                );

                CREATE TABLE message_recipients (
                    message_id TEXT NOT NULL REFERENCES messages (id) ON DELETE CASCADE,
                    recipient TEXT NOT NULL REFERENCES agents (name),
                    kind TEXT NOT NULL DEFAULT 'to' CHECK (kind IN ('to', 'cc')),
                    ordinal INTEGER NOT NULL CHECK (ordinal >= 0),
                    read_at TEXT,
                    archived_at TEXT,
                    PRIMARY KEY (message_id, recipient),
                    UNIQUE (message_id, ordinal)
                );

                INSERT INTO messages (id, thread_id, sender, subject, body, created_at)
                VALUES ('mail-1', 'mail-1', 'ghost', 'Old mail', 'Keep me', '2026-01-10T12:00:00+00:00');
                INSERT INTO message_recipients (message_id, recipient, ordinal, read_at)
                VALUES ('mail-1', 'ghost', 0, '2026-01-11T09:00:00+00:00');

                PRAGMA user_version = 15;
                """,
                cancellationToken);
        }

        // act
        await using var upgraded = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // assert
        var orphanCount = await QueryScalarLongAsync(
            upgraded, "SELECT COUNT(*) FROM pragma_foreign_key_check", cancellationToken);
        Assert.Equal(0, orphanCount);

        var state = new
        {
            Version = await QueryScalarLongAsync(upgraded, "PRAGMA user_version", cancellationToken),
            Sender = await QueryScalarStringAsync(
                upgraded, "SELECT sender FROM messages WHERE id = 'mail-1'", cancellationToken),
            Subject = await QueryScalarStringAsync(
                upgraded, "SELECT subject FROM messages WHERE id = 'mail-1'", cancellationToken),
            ReadAt = await QueryScalarStringAsync(
                upgraded,
                "SELECT read_at FROM message_recipients WHERE message_id = 'mail-1' AND recipient = 'ghost'",
                cancellationToken)
        };

        state.MatchInlineSnapshot(
            """
            {
              "Version": 18,
              "Sender": "ghost",
              "Subject": "Old mail",
              "ReadAt": "2026-01-11T09:00:00+00:00"
            }
            """);
    }

    /// <summary>
    /// Seeds a raw v16-shaped database with the pre-agent-model wake tables:
    /// a Nitro instance id on the outbox, batches, and daemon lease, and
    /// targets keyed by (harness, session_id, host). InitializeAsync must
    /// wipe them and lay down the current agent-keyed shape.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_ReplaceOldShapedWakeTables_When_ExistingVersionIsV16()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = new SqliteConnection(
            $"Data Source={AgentWorkspace.GetDatabasePath(_workspaceDirectory)};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
            await ExecuteAsync(connection, "PRAGMA foreign_keys = OFF;", cancellationToken);
            await ExecuteAsync(
                connection,
                """
                CREATE TABLE mail_wake_outbox (
                    nitro_instance_id TEXT NOT NULL,
                    actor TEXT NOT NULL REFERENCES agents (name),
                    requested_generation INTEGER NOT NULL DEFAULT 0 CHECK (requested_generation >= 0),
                    settled_generation INTEGER NOT NULL DEFAULT 0
                        CHECK (settled_generation >= 0 AND settled_generation <= requested_generation),
                    due_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    PRIMARY KEY (nitro_instance_id, actor)
                );

                CREATE TABLE mail_wake_batches (
                    batch_id TEXT PRIMARY KEY,
                    nitro_instance_id TEXT NOT NULL,
                    actor TEXT NOT NULL,
                    claimed_generation INTEGER NOT NULL CHECK (claimed_generation >= 0),
                    owner_id TEXT NOT NULL,
                    attempt_id TEXT NOT NULL,
                    status TEXT NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'completed', 'released')),
                    claimed_at TEXT NOT NULL,
                    expires_at TEXT NOT NULL,
                    completed_at TEXT NULL,
                    last_error TEXT NULL CHECK (last_error IS NULL OR length(last_error) <= 200),
                    FOREIGN KEY (nitro_instance_id, actor) REFERENCES mail_wake_outbox (nitro_instance_id, actor)
                );

                CREATE TABLE mail_wake_targets (
                    batch_id TEXT NOT NULL REFERENCES mail_wake_batches (batch_id) ON DELETE CASCADE,
                    harness TEXT NOT NULL
                        CHECK (harness IN ('claude-code', 'codex', 'copilot', 'opencode', 'nitro-board')),
                    session_id TEXT NOT NULL,
                    host TEXT NOT NULL,
                    status TEXT NOT NULL DEFAULT 'pending'
                        CHECK (status IN ('pending', 'delivered', 'satisfied', 'delegated', 'skipped', 'failed')),
                    offered_generation INTEGER NULL CHECK (offered_generation IS NULL OR offered_generation >= 0),
                    accepted_generation INTEGER NULL CHECK (accepted_generation IS NULL OR accepted_generation >= 0),
                    last_error TEXT NULL CHECK (last_error IS NULL OR length(last_error) <= 200),
                    updated_at TEXT NOT NULL,
                    PRIMARY KEY (batch_id, harness, session_id, host)
                );

                CREATE TABLE mail_wake_daemons (
                    nitro_instance_id TEXT PRIMARY KEY,
                    owner_id TEXT NOT NULL,
                    epoch INTEGER NOT NULL CHECK (epoch >= 1),
                    leased_at TEXT NOT NULL,
                    expires_at TEXT NOT NULL,
                    last_error TEXT NULL CHECK (last_error IS NULL OR length(last_error) <= 200)
                );

                PRAGMA user_version = 16;
                """,
                cancellationToken);
        }

        // act
        await using var upgraded = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // assert
        var state = new
        {
            Version = await QueryScalarLongAsync(upgraded, "PRAGMA user_version", cancellationToken),
            OutboxColumns = await QueryColumnNamesAsync(upgraded, "mail_wake_outbox", cancellationToken),
            BatchColumns = await QueryColumnNamesAsync(upgraded, "mail_wake_batches", cancellationToken),
            TargetColumns = await QueryColumnNamesAsync(upgraded, "mail_wake_targets", cancellationToken),
            DaemonColumns = await QueryColumnNamesAsync(upgraded, "mail_wake_daemons", cancellationToken)
        };

        state.MatchInlineSnapshot(
            """
            {
              "Version": 18,
              "OutboxColumns": [
                "actor",
                "requested_generation",
                "settled_generation",
                "due_at",
                "updated_at"
              ],
              "BatchColumns": [
                "batch_id",
                "actor",
                "claimed_generation",
                "owner_id",
                "attempt_id",
                "status",
                "claimed_at",
                "expires_at",
                "completed_at",
                "last_error"
              ],
              "TargetColumns": [
                "batch_id",
                "agent",
                "status",
                "offered_generation",
                "accepted_generation",
                "last_error",
                "updated_at"
              ],
              "DaemonColumns": [
                "id",
                "owner_token",
                "acquired_at",
                "heartbeat_at",
                "expires_at"
              ]
            }
            """);
    }

    /// <summary>
    /// Opening an already-current database, whether for the first time after
    /// creation or a second time in a row, never wipes the agents already in it.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_PreserveAgentRows_When_ReopeningACurrentDatabase()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
            await ExecuteAsync(
                connection,
                """
                INSERT INTO agents (name, registered_at, started_at, last_seen_at, harness, session_id)
                VALUES ('leia', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00',
                        '2026-01-10T12:00:00+00:00', 'claude-code', 'session-current');
                """,
                cancellationToken);
        }

        // act: open the already-current database twice more.
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }

        await using var reopened = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // assert
        Assert.Equal(AgentDatabase.CurrentVersion,
            await QueryScalarLongAsync(reopened, "PRAGMA user_version", cancellationToken));
        Assert.Equal(1, await QueryScalarLongAsync(
            reopened, "SELECT COUNT(*) FROM agents WHERE name = 'leia'", cancellationToken));
    }

    [Fact]
    public async Task InitializeAsync_Should_UpgradeV11AndPreserveRows_When_TakeoverTablesAreMissing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
            await ExecuteAsync(
                connection,
                """
                INSERT INTO tasks (id, title, created_at, updated_at)
                VALUES ('task-v11', 'Preserve me', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
                DROP TABLE agent_takeover_items;
                DROP TABLE agent_takeovers;
                PRAGMA user_version = 11;
                """,
                cancellationToken);
        }

        // act
        await using var upgraded = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // assert
        Assert.Equal(AgentDatabase.CurrentVersion,
            await QueryScalarLongAsync(upgraded, "PRAGMA user_version", cancellationToken));
        Assert.Equal("Preserve me", await QueryScalarStringAsync(
            upgraded, "SELECT title FROM tasks WHERE id = 'task-v11'", cancellationToken));
        Assert.Equal(1, await QueryScalarLongAsync(
            upgraded,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'agent_takeovers'",
            cancellationToken));
        Assert.Equal(1, await QueryScalarLongAsync(
            upgraded,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'agent_takeover_items'",
            cancellationToken));
    }

    [Fact]
    public async Task InitializeAsync_Should_LeaveTakeoverRowsUnchanged_When_DatabaseIsCurrent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
            await ExecuteAsync(
                connection,
                """
                INSERT INTO agent_takeovers (
                    id, from_actor, to_actor, actor, created_at, forced, role, reason
                ) VALUES (
                    'to-current', 'maya', 'nora', 'maya', '2026-01-10T12:00:00+00:00', 1, NULL, NULL
                );
                """,
                cancellationToken);
        }

        // act
        await using var reopened = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // assert
        Assert.Equal(AgentDatabase.CurrentVersion,
            await QueryScalarLongAsync(reopened, "PRAGMA user_version", cancellationToken));
        Assert.Equal(1, await QueryScalarLongAsync(
            reopened, "SELECT COUNT(*) FROM agent_takeovers WHERE id = 'to-current'", cancellationToken));
    }

    [Fact]
    public void IsUpgradableVersion_Should_ReturnTrue_When_VersionIs11()
    {
        // act
        var isUpgradable = AgentDatabase.IsUpgradableVersion(11);

        // assert
        Assert.True(isUpgradable);
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
    /// Tests that the unified agents table accepts <c>unsupported</c> as a ping result.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_AcceptUnsupportedLastPingResult_When_DatabaseIsFreshlyCreated()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await ExecuteAsync(
            connection,
            "INSERT INTO agents (name, registered_at, started_at, last_seen_at) VALUES "
            + "('maya', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');",
            cancellationToken);

        // act
        await ExecuteAsync(
            connection,
            "UPDATE agents SET last_ping_result = 'unsupported' WHERE name = 'maya';",
            cancellationToken);

        // assert
        var lastPingResult = await QueryScalarStringAsync(
            connection, "SELECT last_ping_result FROM agents WHERE name = 'maya'", cancellationToken);
        Assert.Equal("unsupported", lastPingResult);
    }

    [Fact]
    public async Task InitializeAsync_Should_Throw_When_ExistingVersionIsGreaterThanCurrent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await StampVersionOnNewFileAsync(AgentDatabase.CurrentVersion + 1, cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _database.InitializeAsync(_workspaceDirectory, cancellationToken));
    }

    /// <summary>
    /// Tests rejection when an initialized database is restamped with a schema
    /// version newer than the CLI supports.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_Throw_When_ForceReinitializingAgainstAVersionNewerThanCurrent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection =
            await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
            await ExecuteAsync(connection, $"PRAGMA user_version = {AgentDatabase.CurrentVersion + 1};", cancellationToken);
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
        await Assert.ThrowsAsync<AgentWorkspaceSchemaMismatchException>(
            () => _database.ConnectAsync(_workspaceDirectory, cancellationToken));
    }

    [Fact]
    public async Task ConnectAsync_Should_Throw_When_UnifiedPathHasVersion1()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await StampVersionOnNewFileAsync(1, cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<AgentWorkspaceSchemaMismatchException>(
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
            await ExecuteAsync(connection, $"PRAGMA user_version = {AgentDatabase.CurrentVersion + 1};", cancellationToken);
        }

        // act & assert
        var exception = await Assert.ThrowsAsync<ExitException>(
            () => _database.ConnectAsync(_workspaceDirectory, cancellationToken));
        Assert.Contains("newer version", exception.Message);
    }

    /// <summary>
    /// A v2, v3, v4, v5, v6, or v7 database is only upgraded in place by
    /// <see cref="AgentDatabase.InitializeAsync"/>; connecting directly
    /// against it still requires exactly <see cref="AgentDatabase.CurrentVersion"/>.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(17)]
    public async Task ConnectAsync_Should_Throw_When_VersionIsUpgradable(int upgradableVersion)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await StampVersionOnNewFileAsync(upgradableVersion, cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<AgentWorkspaceSchemaMismatchException>(
            () => _database.ConnectAsync(_workspaceDirectory, cancellationToken));
    }

    /// <summary>
    /// <c>agent_deliveries.channel</c> is CHECK-constrained to
    /// <c>digest</c>, <c>gate</c>, or <c>ping</c>.
    /// </summary>
    [Fact]
    public async Task AgentDeliveriesTable_Should_RejectUnknownChannel_When_CheckConstraintFires()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await ExecuteAsync(
            connection,
            "INSERT INTO agents (name, registered_at, started_at, last_seen_at) VALUES "
            + "('maya', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');",
            cancellationToken);

        // act
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(
            connection,
            """
            INSERT INTO agent_deliveries (agent, message_id, channel, delivered_at)
            VALUES ('maya', 'msg-1', 'unknown-channel', '2026-01-10T12:00:00+00:00');
            """,
            cancellationToken));

        // assert
        Assert.IsType<SqliteException>(exception);
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
    /// The partial unique index <c>idx_agents_session</c> rejects a second row
    /// that claims the same (harness, session_id) pair.
    /// </summary>
    [Fact]
    public async Task AgentsTable_Should_RejectDuplicateHarnessSessionId_When_PartialUniqueIndexFires()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await ExecuteAsync(
            connection,
            """
            INSERT INTO agents (name, registered_at, started_at, last_seen_at, harness, session_id)
            VALUES ('maya', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00',
                    '2026-01-10T12:00:00+00:00', 'claude-code', 'session-dup');
            """,
            cancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO agents (name, registered_at, started_at, last_seen_at, harness, session_id)
            VALUES ('nora', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00',
                    '2026-01-10T12:00:00+00:00', 'claude-code', 'session-dup');
            """,
            cancellationToken));

        // assert
        Assert.Equal(2067, exception.SqliteExtendedErrorCode);
    }

    /// <summary>
    /// <c>idx_agents_session</c> only covers rows with a non-null harness, so
    /// any number of login-only rows (both columns null) coexist.
    /// </summary>
    [Fact]
    public async Task AgentsTable_Should_AllowMultipleLoginOnlyRows_When_HarnessIsNull()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // act
        await ExecuteAsync(
            connection,
            """
            INSERT INTO agents (name, registered_at, started_at, last_seen_at)
            VALUES ('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            INSERT INTO agents (name, registered_at, started_at, last_seen_at)
            VALUES ('codex', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            """,
            cancellationToken);

        // assert
        var loginOnlyCount = await QueryScalarLongAsync(
            connection, "SELECT COUNT(*) FROM agents WHERE harness IS NULL", cancellationToken);
        Assert.Equal(2, loginOnlyCount);
    }

    /// <summary>
    /// <c>agent_deliveries.agent</c> references <c>agents (name)</c>; an unknown
    /// agent name is rejected.
    /// </summary>
    [Fact]
    public async Task AgentDeliveriesTable_Should_RejectUnknownAgent_When_ForeignKeyFires()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO agent_deliveries (agent, message_id, channel, delivered_at)
            VALUES ('ghost', 'msg-1', 'digest', '2026-01-10T12:00:00+00:00');
            """,
            cancellationToken));

        // assert
        Assert.Equal(787, exception.SqliteExtendedErrorCode);
    }

    /// <summary>
    /// <c>agent_ping_gates.agent</c> references <c>agents (name)</c>; an unknown
    /// agent name is rejected.
    /// </summary>
    [Fact]
    public async Task AgentPingGatesTable_Should_RejectUnknownAgent_When_ForeignKeyFires()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO agent_ping_gates (agent, attempt_id, acquired_at, expires_at)
            VALUES ('ghost', 'attempt-1', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:30+00:00');
            """,
            cancellationToken));

        // assert
        Assert.Equal(787, exception.SqliteExtendedErrorCode);
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
        var agentStore = new AgentStore(fileSystem, timeProvider, _database);
        var mailStore = new MailStore(fileSystem, timeProvider, _database, agentStore);

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

        await using (var agentSeedConnection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken))
        {
            await ExecuteAsync(
                agentSeedConnection,
                "INSERT INTO agents (name, registered_at, started_at, last_seen_at) VALUES "
                + "('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00'), "
                + "('codex', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');",
                cancellationToken);
        }

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
    /// Tests adding mail-wake and ping-gate tables and indexes to a database
    /// stamped v6 that lacks them. Agent-domain rows (agents, ping_leases) are
    /// wiped by the blanket agent-domain reset; mail is a non-agent table and
    /// survives.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_AddMailWakeAndAgentPingGateTables_When_ExistingVersionIsV6()
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
            await ExecuteAsync(connection, PingLeaseSchema.Create, cancellationToken);

            await ExecuteAsync(
                connection,
                """
                INSERT INTO agents (name, registered_at, started_at, last_seen_at, role)
                VALUES ('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', 'backend'),
                       ('codex', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '');

                INSERT INTO messages (id, thread_id, sender, subject, body, created_at)
                VALUES ('msg-v6', 'thread-v6', 'claude', 'Status', 'Merged.', '2026-01-10T12:00:00+00:00');

                INSERT INTO message_recipients (message_id, recipient, kind, ordinal)
                VALUES ('msg-v6', 'codex', 'to', 0);

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
            "agent_ping_gates"
        })
        {
            var tableCount = await QueryScalarLongAsync(
                connection2,
                $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{newTable}'",
                cancellationToken);
            Assert.Equal(1, tableCount);
        }

        foreach (var index in new[]
        {
            "idx_mail_wake_outbox_due", "idx_mail_wake_batches_one_active_per_actor",
            "idx_mail_wake_batches_expires", "idx_agent_ping_gates_expires"
        })
        {
            var indexCount = await QueryScalarLongAsync(
                connection2,
                $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = '{index}'",
                cancellationToken);
            Assert.Equal(1, indexCount);
        }

        var agentCount = await QueryScalarLongAsync(connection2, "SELECT COUNT(*) FROM agents", cancellationToken);
        var messageCount =
            await QueryScalarLongAsync(connection2, "SELECT COUNT(*) FROM messages", cancellationToken);
        var recipientCount =
            await QueryScalarLongAsync(connection2, "SELECT COUNT(*) FROM message_recipients", cancellationToken);
        var leaseCount = await QueryScalarLongAsync(connection2, "SELECT COUNT(*) FROM ping_leases", cancellationToken);
        Assert.Equal(0, agentCount);
        Assert.Equal(1, messageCount);
        Assert.Equal(1, recipientCount);
        Assert.Equal(0, leaseCount);
    }

    /// <summary>
    /// <c>mail_wake_targets.agent</c> references <c>agents (name)</c>; an
    /// unknown agent name is rejected.
    /// </summary>
    [Fact]
    public async Task MailWakeTargetsTable_Should_RejectUnknownAgent_When_ForeignKeyFires()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await ExecuteAsync(
            connection,
            """
            INSERT INTO agents (name, registered_at, started_at, last_seen_at) VALUES
                ('pascal', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            INSERT INTO mail_wake_outbox (actor, requested_generation, settled_generation, due_at, updated_at)
            VALUES ('pascal', 1, 0, '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            INSERT INTO mail_wake_batches (
                batch_id, actor, claimed_generation, owner_id, attempt_id, status, claimed_at, expires_at
            ) VALUES (
                'batch-fresh', 'pascal', 1, 'owner-1', 'attempt-1',
                'active', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:30+00:00'
            );
            """,
            cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO mail_wake_targets (batch_id, agent, status, updated_at)
            VALUES ('batch-fresh', 'ghost', 'pending', '2026-01-10T12:00:00+00:00');
            """,
            cancellationToken));
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
            "INSERT INTO agents (name, registered_at, started_at, last_seen_at) VALUES "
            + "('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');",
            cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO mail_wake_outbox (actor, requested_generation, settled_generation, due_at, updated_at)
            VALUES ('claude', 1, 2, '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            """,
            cancellationToken));
    }

    /// <summary>
    /// At most one <c>active</c> <c>mail_wake_batches</c> row exists per
    /// actor, enforced by <c>idx_mail_wake_batches_one_active_per_actor</c>.
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
            INSERT INTO agents (name, registered_at, started_at, last_seen_at) VALUES
                ('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            INSERT INTO mail_wake_outbox (actor, requested_generation, settled_generation, due_at, updated_at)
            VALUES ('claude', 1, 0, '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            INSERT INTO mail_wake_batches (
                batch_id, actor, claimed_generation, owner_id, attempt_id, status, claimed_at, expires_at
            ) VALUES (
                'batch-1', 'claude', 1, 'owner-1', 'attempt-1',
                'active', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:30+00:00'
            );
            """,
            cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO mail_wake_batches (
                batch_id, actor, claimed_generation, owner_id, attempt_id, status, claimed_at, expires_at
            ) VALUES (
                'batch-2', 'claude', 1, 'owner-2', 'attempt-2',
                'active', '2026-01-10T12:00:01+00:00', '2026-01-10T12:00:31+00:00'
            );
            """,
            cancellationToken));
    }

    /// <summary>
    /// <c>mail_wake_targets</c> rows cascade-delete with their owning
    /// <c>mail_wake_batches</c> row when the batch itself is deleted.
    /// </summary>
    [Fact]
    public async Task MailWakeTargetsTable_Should_CascadeDelete_When_TheOwningBatchIsDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await ExecuteAsync(
            connection,
            """
            INSERT INTO agents (name, registered_at, started_at, last_seen_at) VALUES
                ('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            INSERT INTO mail_wake_outbox (actor, requested_generation, settled_generation, due_at, updated_at)
            VALUES ('claude', 1, 0, '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            INSERT INTO mail_wake_batches (
                batch_id, actor, claimed_generation, owner_id, attempt_id, status, claimed_at, expires_at
            ) VALUES (
                'batch-target', 'claude', 1, 'owner-1', 'attempt-1',
                'active', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:30+00:00'
            );
            INSERT INTO mail_wake_targets (batch_id, agent, status, updated_at)
            VALUES ('batch-target', 'claude', 'pending', '2026-01-10T12:00:00+00:00');
            """,
            cancellationToken);

        // act
        await ExecuteAsync(
            connection, "DELETE FROM mail_wake_batches WHERE batch_id = 'batch-target';", cancellationToken);

        // assert
        var targetCount = await QueryScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM mail_wake_targets WHERE batch_id = 'batch-target'",
            cancellationToken);
        Assert.Equal(0, targetCount);
    }

    /// <summary>
    /// <c>mail_wake_daemons.id</c> only ever accepts <c>1</c>: the table
    /// holds a single row.
    /// </summary>
    [Fact]
    public async Task MailWakeDaemonsTable_Should_RejectASecondRow_When_CheckConstraintFires()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO mail_wake_daemons (id, owner_token, acquired_at, heartbeat_at, expires_at)
            VALUES (2, 'owner-1', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:30+00:00');
            """,
            cancellationToken));
    }

    /// <summary>
    /// Opens a database file and sets its <c>PRAGMA user_version</c> without creating tables.
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
}
