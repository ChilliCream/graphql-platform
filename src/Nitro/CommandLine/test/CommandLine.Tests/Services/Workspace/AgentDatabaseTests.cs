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

        foreach (var index in new[]
        {
            "idx_mail_wake_outbox_due", "idx_mail_wake_batches_one_active_per_actor",
            "idx_mail_wake_batches_expires", "idx_session_ping_gates_expires"
        })
        {
            var indexCount = await QueryScalarLongAsync(
                connection,
                $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = '{index}'",
                cancellationToken);
            Assert.Equal(1, indexCount);
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
        Assert.DoesNotContain("pid", sessionColumns);
        Assert.DoesNotContain("proc_start", sessionColumns);
    }

    /// <summary>
    /// Seeds a raw v14-shaped database: the old six-column <c>agents</c> table,
    /// the (unchanged) <c>agent_sessions</c> table, a task, and a piece of mail
    /// sent by the seeded agent. InitializeAsync must wipe the agent-domain
    /// tables, lay the new <c>agents</c> columns, and leave tasks and mail
    /// untouched.
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
            await ExecuteAsync(connection, AgentSessionSchema.Create, cancellationToken);

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

                INSERT INTO agent_sessions (
                    harness, session_id, agent_name, binding_kind, host,
                    cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
                    role, harness_version
                ) VALUES (
                    'claude-code', 'session-v14', 'maya', 'explicit', 'host-a',
                    '/tmp/work', '/tmp/work/.nitro/agents', 'none', '', '2026-01-10T12:00:00+00:00',
                    '2026-01-10T12:00:00+00:00', 'backend', '1.2.3'
                );

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
            AgentSessions =
                await QueryScalarLongAsync(upgraded, "SELECT COUNT(*) FROM agent_sessions", cancellationToken),
            AgentColumns = await QueryColumnNamesAsync(upgraded, "agents", cancellationToken)
        };

        state.MatchInlineSnapshot(
            """
            {
              "Version": 16,
              "Tasks": 1,
              "Messages": 1,
              "MessageRecipients": 1,
              "Agents": 0,
              "AgentSessions": 0,
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
                "idle_push_armed",
                "implicit",
                "client"
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
              "Version": 16,
              "Sender": "ghost",
              "Subject": "Old mail",
              "ReadAt": "2026-01-11T09:00:00+00:00"
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

    [Fact]
    public async Task InitializeAsync_Should_UpgradeAgentSessionIdentityHarnessConstraint_When_ConstraintPredatesOpencode()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
            await ExecuteAsync(
                connection,
                """
                DROP TABLE agent_session_identities;
                CREATE TABLE agent_session_identities (
                    harness TEXT NOT NULL CHECK (harness IN ('claude-code', 'codex', 'copilot')),
                    session_id TEXT NOT NULL,
                    actor TEXT NOT NULL UNIQUE REFERENCES agents (name),
                    role TEXT NOT NULL DEFAULT '',
                    actor_revision INTEGER NOT NULL DEFAULT 1 CHECK (actor_revision > 0),
                    created_at TEXT NOT NULL,
                    last_seen_at TEXT NOT NULL,
                    PRIMARY KEY (harness, session_id)
                );
                CREATE INDEX idx_agent_session_identities_actor
                    ON agent_session_identities (actor);
                PRAGMA user_version = 11;
                """,
                cancellationToken);
        }

        // act
        await using var upgraded = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await ExecuteAsync(
            upgraded,
            """
            INSERT INTO agents (name, registered_at, started_at, last_seen_at)
            VALUES ('maya', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            INSERT INTO agent_session_identities (
                harness, session_id, actor, created_at, last_seen_at
            ) VALUES (
                'opencode', 'session-1', 'maya', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00'
            );
            """,
            cancellationToken);

        // assert
        Assert.Equal(AgentDatabase.CurrentVersion,
            await QueryScalarLongAsync(upgraded, "PRAGMA user_version", cancellationToken));
        Assert.Equal("opencode", await QueryScalarStringAsync(
            upgraded,
            "SELECT harness FROM agent_session_identities WHERE session_id = 'session-1'",
            cancellationToken));
    }

    /// <summary>
    /// Tests that a fresh session table accepts <c>unsupported</c> as a ping result.
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

    /// <summary>
    /// Tests that a fresh session table accepts the <c>nitro-board</c> harness
    /// and <c>db-watch</c> endpoint.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_AcceptNitroBoardHarnessAndDbWatchEndpoint_When_DatabaseIsFreshlyCreated()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

        // act
        await ExecuteAsync(
            connection,
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'nitro-board', 'board-fresh', NULL, 'none', 'host-a',
                '/tmp/work', '/tmp/work/.nitro/agents', 'db-watch', 'local', '2026-01-10T12:00:00+00:00',
                '2026-01-10T12:00:00+00:00'
            );
            """,
            cancellationToken);

        // assert
        var harness = await QueryScalarStringAsync(
            connection, "SELECT harness FROM agent_sessions WHERE session_id = 'board-fresh'", cancellationToken);
        var endpointKind = await QueryScalarStringAsync(
            connection,
            "SELECT endpoint_kind FROM agent_sessions WHERE session_id = 'board-fresh'",
            cancellationToken);
        Assert.Equal("nitro-board", harness);
        Assert.Equal("db-watch", endpointKind);
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
                harness, session_id, agent_name, binding_kind, host,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'claude-code', 'session-1', NULL, 'none', 'host-a',
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
            "INSERT INTO agents (name, registered_at, started_at, last_seen_at) VALUES "
            + "('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');",
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
                harness, session_id, agent_name, binding_kind, host,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'claude-code', 'session-2', NULL, 'explicit', 'host-a',
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
                harness, session_id, agent_name, binding_kind, host,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'claude-code', 'session-3', NULL, 'none', 'host-a',
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
                harness, session_id, agent_name, binding_kind, host,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'claude-code', 'session-4', NULL, 'none', 'host-a',
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
    /// <c>agent_sessions</c> row.
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
    /// Tests adding mail-wake and session-ping-gate tables and indexes to a
    /// database stamped v6 that lacks them. Agent-domain rows (agents,
    /// agent_sessions, session_deliveries, ping_leases) are wiped by the
    /// blanket agent-domain reset; mail is a non-agent table and survives.
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
                INSERT INTO agents (name, registered_at, started_at, last_seen_at, role, implicit, client)
                VALUES ('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', 'backend', 0, 'claude-code'),
                       ('codex', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '', 0, 'codex');

                INSERT INTO messages (id, thread_id, sender, subject, body, created_at)
                VALUES ('msg-v6', 'thread-v6', 'claude', 'Status', 'Merged.', '2026-01-10T12:00:00+00:00');

                INSERT INTO message_recipients (message_id, recipient, kind, ordinal)
                VALUES ('msg-v6', 'codex', 'to', 0);

                INSERT INTO agent_sessions (
                    harness, session_id, agent_name, binding_kind, host,
                    cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
                    role, harness_version
                ) VALUES (
                    'claude-code', 'session-v6', 'claude', 'explicit', 'host-a',
                    '/tmp/work', '/tmp/work/.nitro/agents', 'none', '', '2026-01-10T12:00:00+00:00',
                    '2026-01-10T12:00:00+00:00', 'backend', '1.2.3'
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

        foreach (var index in new[]
        {
            "idx_mail_wake_outbox_due", "idx_mail_wake_batches_one_active_per_actor",
            "idx_mail_wake_batches_expires", "idx_session_ping_gates_expires"
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
        var sessionCount =
            await QueryScalarLongAsync(connection2, "SELECT COUNT(*) FROM agent_sessions", cancellationToken);
        var deliveryCount =
            await QueryScalarLongAsync(connection2, "SELECT COUNT(*) FROM session_deliveries", cancellationToken);
        var leaseCount = await QueryScalarLongAsync(connection2, "SELECT COUNT(*) FROM ping_leases", cancellationToken);
        Assert.Equal(0, agentCount);
        Assert.Equal(1, messageCount);
        Assert.Equal(1, recipientCount);
        Assert.Equal(0, sessionCount);
        Assert.Equal(0, deliveryCount);
        Assert.Equal(0, leaseCount);
    }

    /// <summary>
    /// Tests that fresh wake-target and ping-gate tables accept <c>nitro-board</c>.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_Should_AcceptNitroBoardHarnessInMailWakeTargetsAndSessionPingGates_When_DatabaseIsFreshlyCreated()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await ExecuteAsync(
            connection,
            "INSERT INTO agents (name, registered_at, started_at, last_seen_at) VALUES "
            + "('pascal', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');",
            cancellationToken);
        await ExecuteAsync(
            connection,
            """
            INSERT INTO mail_wake_outbox (nitro_instance_id, actor, requested_generation, settled_generation, due_at, updated_at)
            VALUES ('instance-a', 'pascal', 1, 0, '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');

            INSERT INTO mail_wake_batches (
                batch_id, nitro_instance_id, actor, claimed_generation, owner_id, attempt_id,
                status, claimed_at, expires_at
            ) VALUES (
                'batch-fresh', 'instance-a', 'pascal', 1, 'owner-1', 'attempt-1',
                'active', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:30+00:00'
            );
            """,
            cancellationToken);

        // act
        await ExecuteAsync(
            connection,
            """
            INSERT INTO mail_wake_targets (batch_id, harness, session_id, host, status, updated_at)
            VALUES ('batch-fresh', 'nitro-board', 'board-fresh', 'host-a', 'pending', '2026-01-10T12:00:00+00:00');

            INSERT INTO session_ping_gates (harness, session_id, host, attempt_id, acquired_at, expires_at)
            VALUES ('nitro-board', 'board-fresh', 'host-a', 'attempt-1', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:30+00:00');
            """,
            cancellationToken);

        // assert
        var targetHarness = await QueryScalarStringAsync(
            connection, "SELECT harness FROM mail_wake_targets WHERE session_id = 'board-fresh'", cancellationToken);
        var gateHarness = await QueryScalarStringAsync(
            connection, "SELECT harness FROM session_ping_gates WHERE session_id = 'board-fresh'", cancellationToken);
        Assert.Equal("nitro-board", targetHarness);
        Assert.Equal("nitro-board", gateHarness);
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
            INSERT INTO agents (name, registered_at, started_at, last_seen_at) VALUES
                ('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
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
            INSERT INTO agents (name, registered_at, started_at, last_seen_at) VALUES
                ('claude', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            INSERT INTO mail_wake_outbox (nitro_instance_id, actor, requested_generation, settled_generation, due_at, updated_at)
            VALUES ('instance-a', 'claude', 1, 0, '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            INSERT INTO mail_wake_batches (
                batch_id, nitro_instance_id, actor, claimed_generation, owner_id, attempt_id,
                status, claimed_at, expires_at
            ) VALUES (
                'batch-target', 'instance-a', 'claude', 1, 'owner-1', 'attempt-1',
                'active', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:30+00:00'
            );
            INSERT INTO mail_wake_targets (batch_id, harness, session_id, host, status, updated_at)
            VALUES ('batch-target', 'claude-code', 'session-target', 'host-a', 'pending', '2026-01-10T12:00:00+00:00');
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
    /// Tests that a session ping gate survives deletion of the corresponding session row.
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
            INSERT INTO session_ping_gates (harness, session_id, host, attempt_id, acquired_at, expires_at)
            VALUES ('claude-code', 'session-gate', 'host-a', 'attempt-1', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:30+00:00');
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
    /// Tests that the ping-gate primary key rejects a duplicate
    /// (harness, session_id, host) generation.
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
            INSERT INTO session_ping_gates (harness, session_id, host, attempt_id, acquired_at, expires_at)
            VALUES ('claude-code', 'session-dup', 'host-a', 'attempt-1', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:30+00:00');
            """,
            cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO session_ping_gates (harness, session_id, host, attempt_id, acquired_at, expires_at)
            VALUES ('claude-code', 'session-dup', 'host-a', 'attempt-2', '2026-01-10T12:00:01+00:00', '2026-01-10T12:00:31+00:00');
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
                harness, session_id, agent_name, binding_kind, host,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'claude-code', '{sessionId}', NULL, 'none', 'host-a',
                '/tmp/work', '/tmp/work/.nitro/agents', 'none', '', '2026-01-10T12:00:00+00:00',
                '2026-01-10T12:00:00+00:00'
            );
            """,
            cancellationToken);
}
