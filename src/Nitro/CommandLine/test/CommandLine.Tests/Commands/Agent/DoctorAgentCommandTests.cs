using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <c>agent doctor</c>'s static checks: schema version, orphaned
/// or unclaimed session rows, dead-generation rows pending reap, and
/// mixed-instance rows with the explicit <c>--clean-mixed-instance</c>
/// cleanup. All rows are read directly, never through
/// <see cref="IAgentSessionRegistry.ListAsync"/>, so these tests also
/// confirm that a plain `agent doctor` run never mutates or reaps anything
/// on its own (the ticket's "no fixes beyond the explicit mixed-instance
/// cleanup" non-goal).
/// </summary>
public sealed class DoctorAgentCommandTests : AgentCommandTestBase
{
    private const string FixedHost = "host-doctor-tests";

    public DoctorAgentCommandTests(NitroCommandFixture fixture) : base(fixture)
    {
        SetupInstanceId(FixedHost);

        // Doctor checks the Claude user-scope hook entries unconditionally,
        // with no --scope flag to opt out; without this override it reads
        // whatever happens to be installed at the real ~/.claude/settings.json
        // on the machine running the test, not this fixture's sandbox.
        SetupClaudeSettingsPathResolver(
            userScopePath: Path.Combine(WorkingDirectory, "..", "claude-home", ".claude", "settings.json"),
            projectScopePath: Path.Combine(WorkingDirectory, ".claude", "settings.json"));

        // Doctor now checks Codex hooks unconditionally too; without this
        // override it reads whatever happens to be at the real CODEX_HOME
        // on the machine running the test, not this fixture's sandbox.
        SetupCodexPathResolver(
            hooksJsonPath: Path.Combine(WorkingDirectory, "..", "codex-home", ".codex", "hooks.json"),
            configTomlPath: Path.Combine(WorkingDirectory, "..", "codex-home", ".codex", "config.toml"));

        // The Claude hooks sidecar (which doctor's hooks-consistency check
        // reads and cross-references against the installed settings.json
        // entries) lives under the real machine's application-data
        // directory by default. Without this override every test here that
        // installs hooks reads and writes that one real, shared file
        // concurrently with every other parallel test process and TFM host
        // doing the same, so one test's freshly written sidecar entry can
        // be silently lost to another test's concurrent, unguarded
        // read-modify-write (ClaudeHooksInstallerService.InstallAsync has
        // no concurrency guard on the sidecar write, unlike its
        // settings.json write). That surfaces here as a spurious "no
        // matching sidecar record" finding, flipping healthy to false.
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "global-config"));
    }

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "doctor", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Check the agent workspace's schema and session presence for problems.

            Usage:
              nitro agent doctor [options]

            Options:
              --clean-mixed-instance  Delete session rows stranded from a previous Nitro instance id (a regenerated fallback id, or a different host sharing this workspace); these rows are never reaped automatically
              --probe <claude>        Run the live round-trip probe for a harness (register a scratch actor, send mail, verify the digest/gate ledger claims, fire the ping): 'claude'. Requires a live claimed session; not part of the default, free checks.
              --output <json>         The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help          Show help and usage information

            Example:
              nitro agent doctor
              nitro agent doctor --clean-mixed-instance
              nitro agent doctor --probe claude
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }

    [Fact]
    public async Task HealthyWorkspace_NoSessions_ReturnsSuccess()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        result.AssertSuccess(
            $"""
            Workspace: {WorkspaceDirectory}
            Schema: v{AgentDatabase.CurrentVersion} (current)

            ✓ Schema version
            ✓ Mail-wake
              Leader: none
              Work: pending=0 accepted=0 deferred=0
            ✓ Mixed-instance sessions

            - claude-code: no ancestor process detected here, skipped.

            - codex: no ancestor process detected here, skipped.

            - copilot: no ancestor process detected here, skipped.
            """);
    }

    [Fact]
    public async Task JsonOutput_HealthyWorkspace_ReturnsStructuredReport()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        Assert.Equal(WorkspaceDirectory, root.GetProperty("workspacePath").GetString());
        Assert.Equal(AgentDatabase.CurrentVersion, root.GetProperty("schemaVersion").GetInt64());
        Assert.True(root.GetProperty("schemaCurrent").GetBoolean());
        Assert.True(root.GetProperty("healthy").GetBoolean());
        Assert.Empty(root.GetProperty("unclaimedSessions").EnumerateArray());
        Assert.Empty(root.GetProperty("deadGenerationSessions").EnumerateArray());
        Assert.Empty(root.GetProperty("mixedInstanceSessions").EnumerateArray());
        Assert.Equal(0, root.GetProperty("mixedInstanceSessionsCleaned").GetInt32());

        // no harness has ever been installed and no probe was requested in
        // this workspace: all findings are absent, not empty.
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("claudeUserHooks").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("claudeProjectHooks").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("copilotHooks").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("codexHooks").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("probe").ValueKind);

        // one entry per harness, none with a detected ancestor in this
        // fixture (the ancestor resolvers default to none found).
        var participants = root.GetProperty("participants").EnumerateArray().ToArray();
        Assert.Equal(3, participants.Length);
        Assert.All(participants, p => Assert.False(p.GetProperty("ancestorDetected").GetBoolean()));

        var mailWake = root.GetProperty("mailWake");
        Assert.True(mailWake.GetProperty("schemaCurrent").GetBoolean());
        Assert.Equal("none", mailWake.GetProperty("leaderState").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, mailWake.GetProperty("epoch").ValueKind);
        Assert.Equal(0, mailWake.GetProperty("pendingActorCount").GetInt32());
        Assert.Equal(0, mailWake.GetProperty("acceptedActorCount").GetInt32());
        Assert.Equal(0, mailWake.GetProperty("deferredActorCount").GetInt32());
        Assert.Equal(0, mailWake.GetProperty("accessDeniedPendingTargets").GetInt32());
        Assert.True(mailWake.GetProperty("healthy").GetBoolean());
    }

    [Fact]
    public async Task InvalidProbeValue_ReturnsParseError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "doctor", "--probe", "codex");

        // assert: system.commandline's own AcceptOnlyFromAmong validation,
        // before this command's action ever runs.
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("codex", result.StdErr + result.StdOut);
    }

    [Fact]
    public async Task ProbeClaude_UpgradableSchema_ReturnsActionableError_NeverAttemptsTheProbe()
    {
        // arrange
        await SeedLegacySchemaVersionAsync(3);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor", "--probe", "claude");

        // assert
        result.AssertError(
            "`--probe claude` requires the current schema; run `nitro agent init` first.");
    }

    [Fact]
    public async Task UpgradableSchema_ReportedAndReturnsError_SessionChecksSkipped()
    {
        // arrange: a v3-shaped database, mirroring an existing workspace
        // from before the session tables shipped (this repo's own
        // .nitro/agents/ at the time this bead was written).
        await SeedLegacySchemaVersionAsync(3);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Schema version:", result.StdOut);
        Assert.Contains("upgradable; run `nitro agent init` to migrate", result.StdOut);
        Assert.Contains("Session checks skipped: the schema is not current.", result.StdOut);
    }

    [Fact]
    public async Task NewerSchema_ReportedAndReturnsError()
    {
        // arrange
        await SeedLegacySchemaVersionAsync(AgentDatabase.CurrentVersion + 1);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Schema version:", result.StdOut);
        Assert.Contains("newer than this CLI supports", result.StdOut);
    }

    [Fact]
    public async Task UnclaimedAliveSession_ReportedAsWarning_DoesNotFailHealth()
    {
        // arrange
        await InitWorkspaceAsync();
        await InsertSessionRowAsync(
            FixedHost, "session-1", agentName: null, bindingKind: "none", pid: CurrentAlivePid());

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("WARN Unclaimed sessions (informational, no action needed):", result.StdOut);
        Assert.Contains("session-1", result.StdOut);
        Assert.DoesNotContain("last-ping=", result.StdOut);
        Assert.DoesNotContain("WARN Dead-generation", result.StdOut);
    }

    [Fact]
    public async Task UnclaimedSession_WithLastPingResult_SurfacesIt_DistinguishingUnsupportedFromNone()
    {
        // arrange: one session the notifier has already pinged (an ordinary
        // outcome) and one with no transport for its endpoint kind
        // (last_ping_result 'unsupported', e.g. copilot-extension), mirroring the
        // distinction `agent session list` surfaces.
        await InitWorkspaceAsync();
        await InsertSessionRowAsync(
            FixedHost, "session-pinged", agentName: null, bindingKind: "none",
            pid: CurrentAlivePid(), lastPingResult: "ok");
        await InsertSessionRowAsync(
            FixedHost, "session-unsupported", agentName: null, bindingKind: "none",
            pid: CurrentAlivePid(), lastPingResult: "unsupported");

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("session-pinged", result.StdOut);
        Assert.Contains("last-ping=ok", result.StdOut);
        Assert.Contains("session-unsupported", result.StdOut);
        Assert.Contains("last-ping=unsupported", result.StdOut);
    }

    [Fact]
    public async Task DeadGenerationSession_ReportedAsWarning_DoesNotFailHealth_And_IsNotReaped()
    {
        // arrange
        await InitWorkspaceAsync();
        await InsertSessionRowAsync(
            FixedHost, "session-dead", agentName: null, bindingKind: "none", pid: DeadPid);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(
            "WARN Dead-generation sessions pending reap "
            + "(run `nitro agent session list` to clean up):",
            result.StdOut);
        Assert.Contains("session-dead", result.StdOut);

        // doctor is read-only for anything short of the explicit
        // mixed-instance cleanup: the dead row must still be there.
        var remaining = await QueryScalarAsync(
            "SELECT COUNT(*) FROM agent_sessions WHERE session_id = 'session-dead'");
        Assert.Equal("1", remaining);
    }

    [Fact]
    public async Task MixedInstanceSession_ReportedAndReturnsError_WithoutCleanFlag()
    {
        // arrange
        await InitWorkspaceAsync();
        await InsertSessionRowAsync(
            "some-other-host", "session-remote", agentName: null, bindingKind: "none", pid: 12345);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Mixed-instance sessions:", result.StdOut);
        Assert.Contains("session-remote", result.StdOut);
        Assert.Contains("Rerun with --clean-mixed-instance to delete these rows.", result.StdOut);

        var remaining = await QueryScalarAsync(
            "SELECT COUNT(*) FROM agent_sessions WHERE session_id = 'session-remote'");
        Assert.Equal("1", remaining);
    }

    [Fact]
    public async Task MixedInstanceSession_CleanedWithFlag_ReturnsSuccess_And_DeletesOnlyThatRow()
    {
        // arrange
        await InitWorkspaceAsync();
        await InsertSessionRowAsync(
            "some-other-host", "session-remote", agentName: null, bindingKind: "none", pid: 12345);
        await InsertSessionRowAsync(
            FixedHost, "session-local", agentName: null, bindingKind: "none", pid: CurrentAlivePid());

        // act
        var result = await ExecuteCommandAsync("agent", "doctor", "--clean-mixed-instance");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("✓ Mixed-instance sessions", result.StdOut);
        Assert.Contains("Cleaned 1 mixed-instance row.", result.StdOut);

        var remainingRemote = await QueryScalarAsync(
            "SELECT COUNT(*) FROM agent_sessions WHERE session_id = 'session-remote'");
        Assert.Equal("0", remainingRemote);

        var remainingLocal = await QueryScalarAsync(
            "SELECT COUNT(*) FROM agent_sessions WHERE session_id = 'session-local'");
        Assert.Equal("1", remainingLocal);
    }

    // ---------- Participant checks ----------

    [Fact]
    public async Task Participant_Claude_ReportsMissingHooks_When_AncestorDetectedButHooksNeverInstalled()
    {
        // arrange: an ancestor is detected, but this fixture's sandbox
        // settings.json was never installed to.
        await InitWorkspaceAsync();
        SetupAncestorSessionResolvers(
            claude: new ClaudeAncestorSession(CurrentAlivePid(), "session-claude", WorkingDirectory, "claude"));

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL claude-code participant:", result.StdOut);
        Assert.Contains(
            "Hooks were never installed for claude-code. Run `nitro agent hooks claude install`, "
            + "then start a new claude-code session.",
            result.StdOut);
    }

    [Fact]
    public async Task Participant_Claude_ReportsSessionNotYetFired_When_HooksInstalledButNoRowExists()
    {
        // arrange: hooks genuinely installed (through the real installer,
        // against this fixture's sandbox path), but no SessionStart ever
        // wrote a row for this process.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "claude", "install", "--scope", "user");
        SetupAncestorSessionResolvers(
            claude: new ClaudeAncestorSession(CurrentAlivePid(), "session-claude", WorkingDirectory, "claude"));

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL claude-code participant:", result.StdOut);
        Assert.Contains(
            "Hooks are installed, but no claude-code session row was found for this process.",
            result.StdOut);
    }

    [Fact]
    public async Task Participant_Claude_ReportsUnboundSession_WithoutFailingHealth()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "claude", "install", "--scope", "user");
        SetupAncestorSessionResolvers(
            claude: new ClaudeAncestorSession(CurrentAlivePid(), "session-claude", WorkingDirectory, "claude"));
        await InsertSessionRowAsync(
            FixedHost, "session-claude", agentName: null, bindingKind: "none", pid: CurrentAlivePid());

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("✓ claude-code participant", result.StdOut);
        Assert.Contains(
            "This session is not yet bound to an agent identity. Run `nitro agent register` to bind it.",
            result.StdOut);
    }

    [Fact]
    public async Task Participant_Claude_ReportsBlankRole_WithoutFailingHealth()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "claude", "install", "--scope", "user");
        await ExecuteCommandAsync("agent", "register", "--actor", "pascal");
        SetupAncestorSessionResolvers(
            claude: new ClaudeAncestorSession(CurrentAlivePid(), "session-claude", WorkingDirectory, "claude"));
        await InsertSessionRowAsync(
            FixedHost, "session-claude", agentName: "pascal", bindingKind: "explicit", pid: CurrentAlivePid());

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("✓ claude-code participant", result.StdOut);
        Assert.Contains(
            "This session has no role yet. Run `nitro agent register --role <role>` to promote it.",
            result.StdOut);
    }

    [Fact]
    public async Task Participant_Claude_ReportsHealthySession_WithRoleAndEndpoint_JsonOutput()
    {
        // arrange: the successful Claude path - bound, roled, an endpoint,
        // and a recorded harness version, nothing left to remediate.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "claude", "install", "--scope", "user");
        await ExecuteCommandAsync("agent", "register", "--actor", "pascal");
        SetupAncestorSessionResolvers(
            claude: new ClaudeAncestorSession(CurrentAlivePid(), "session-claude", WorkingDirectory, "claude"));
        await InsertSessionRowAsync(
            FixedHost, "session-claude", agentName: "pascal", bindingKind: "explicit", pid: CurrentAlivePid(),
            lastPingResult: "ok", role: "orchestrator", harnessVersion: "2.1.0", endpointKind: "claude-peer",
            endpointAddr: "peer-1");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var participant = document.RootElement.GetProperty("participants").EnumerateArray()
            .Single(p => p.GetProperty("harness").GetString() == "claude-code");
        Assert.True(participant.GetProperty("healthy").GetBoolean());
        Assert.True(participant.GetProperty("sessionRowFound").GetBoolean());
        Assert.Equal("pascal", participant.GetProperty("agentName").GetString());
        Assert.Equal("orchestrator", participant.GetProperty("role").GetString());
        Assert.Equal("ok", participant.GetProperty("lastPingResult").GetString());
        Assert.Empty(participant.GetProperty("remediation").EnumerateArray());
    }

    [Fact]
    public async Task Participant_Claude_ReportsUnobservableProcessScope()
    {
        // arrange: the row's recorded process_scope disagrees with this
        // reader's own, so the row's process cannot be verified.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "claude", "install", "--scope", "user");
        await ExecuteCommandAsync("agent", "register", "--actor", "pascal");
        SetupAncestorSessionResolvers(
            claude: new ClaudeAncestorSession(CurrentAlivePid(), "session-claude", WorkingDirectory, "claude"));
        await InsertSessionRowAsync(
            FixedHost, "session-claude", agentName: "pascal", bindingKind: "explicit", pid: CurrentAlivePid(),
            role: "orchestrator", processScope: "a-different-process-scope-than-this-reader-has");

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL claude-code participant:", result.StdOut);
        Assert.Contains("This reader cannot verify the claude-code session's process", result.StdOut);
    }

    [Fact]
    public async Task Participant_Claude_ReportsStaleHeartbeat_AsInformationalOnly()
    {
        // arrange: a heartbeat from an hour ago - reported, but never a
        // failure on its own.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "claude", "install", "--scope", "user");
        await ExecuteCommandAsync("agent", "register", "--actor", "pascal");
        SetupAncestorSessionResolvers(
            claude: new ClaudeAncestorSession(CurrentAlivePid(), "session-claude", WorkingDirectory, "claude"));
        await InsertSessionRowAsync(
            FixedHost, "session-claude", agentName: "pascal", bindingKind: "explicit", pid: CurrentAlivePid(),
            role: "orchestrator", lastBeatAt: FakeTime.GetUtcNow().AddHours(-1));
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var participant = document.RootElement.GetProperty("participants").EnumerateArray()
            .Single(p => p.GetProperty("harness").GetString() == "claude-code");
        Assert.True(participant.GetProperty("healthy").GetBoolean());
        Assert.True(participant.GetProperty("lastHeardSeconds").GetDouble() > 3000);
    }

    [Fact]
    public async Task Participant_Claude_ReportsMissingVersionSignal_WithoutFailingHealth()
    {
        // arrange: an otherwise healthy session that never had a harness
        // version captured (harness_version defaults to blank).
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "claude", "install", "--scope", "user");
        await ExecuteCommandAsync("agent", "register", "--actor", "pascal");
        SetupAncestorSessionResolvers(
            claude: new ClaudeAncestorSession(CurrentAlivePid(), "session-claude", WorkingDirectory, "claude"));
        await InsertSessionRowAsync(
            FixedHost, "session-claude", agentName: "pascal", bindingKind: "explicit", pid: CurrentAlivePid(),
            role: "orchestrator");

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("✓ claude-code participant", result.StdOut);
        Assert.Contains("No claude-code version was recorded for this session.", result.StdOut);
    }

    [Fact]
    public async Task Participant_Codex_ReportsHealthySession_ResolvedByProcess_JsonOutput()
    {
        // arrange: Codex exposes no session id directly, so doctor resolves
        // the row by (host, pid, proc-start) the same way register does.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "pascal");
        SetupAncestorSessionResolvers(codex: new CodexAncestorSession(CurrentAlivePid()));
        await InsertSessionRowAsync(
            FixedHost, "session-codex", agentName: "pascal", bindingKind: "explicit", pid: CurrentAlivePid(),
            harness: "codex", role: "orchestrator");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert: hooks were never installed here, so overall health still
        // fails, but the codex participant's own session resolution is
        // exactly what a healthy match looks like.
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var participant = document.RootElement.GetProperty("participants").EnumerateArray()
            .Single(p => p.GetProperty("harness").GetString() == "codex");
        Assert.True(participant.GetProperty("sessionRowFound").GetBoolean());
        Assert.False(participant.GetProperty("sessionAmbiguous").GetBoolean());
        Assert.Equal("session-codex", participant.GetProperty("sessionId").GetString());
        Assert.Equal("orchestrator", participant.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Participant_Codex_ReportsAmbiguousSessions()
    {
        // arrange: two rows share this exact (host, pid, proc-start) -
        // doctor cannot pick one to diagnose.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "pascal");
        await ExecuteCommandAsync("agent", "register", "--actor", "zeta");
        SetupAncestorSessionResolvers(codex: new CodexAncestorSession(CurrentAlivePid()));
        await InsertSessionRowAsync(
            FixedHost, "session-codex-1", agentName: "pascal", bindingKind: "explicit", pid: CurrentAlivePid(),
            harness: "codex", role: "orchestrator");
        await InsertSessionRowAsync(
            FixedHost, "session-codex-2", agentName: "zeta", bindingKind: "explicit", pid: CurrentAlivePid(),
            harness: "codex", role: "backend");

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL codex participant:", result.StdOut);
        Assert.Contains(
            "Found more than one codex session for this process; this reader cannot pick one to "
            + "diagnose.",
            result.StdOut);
    }

    [Fact]
    public async Task Participant_Copilot_ReportsHealthySession_ResolvedByProcess_JsonOutput()
    {
        // arrange: Copilot resolves the same way Codex does, by process.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "pascal");
        SetupAncestorSessionResolvers(copilot: new CopilotAncestorSession(CurrentAlivePid()));
        await InsertSessionRowAsync(
            FixedHost, "session-copilot", agentName: "pascal", bindingKind: "explicit", pid: CurrentAlivePid(),
            harness: "copilot", role: "orchestrator");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var participant = document.RootElement.GetProperty("participants").EnumerateArray()
            .Single(p => p.GetProperty("harness").GetString() == "copilot");
        Assert.True(participant.GetProperty("sessionRowFound").GetBoolean());
        Assert.Equal("session-copilot", participant.GetProperty("sessionId").GetString());
        Assert.Equal("orchestrator", participant.GetProperty("role").GetString());
    }

    [Fact]
    public async Task CleanFlag_NoMixedInstanceRows_IsNoOp()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "doctor", "--clean-mixed-instance");

        // assert
        result.AssertSuccess(
            $"""
            Workspace: {WorkspaceDirectory}
            Schema: v{AgentDatabase.CurrentVersion} (current)

            ✓ Schema version
            ✓ Mail-wake
              Leader: none
              Work: pending=0 accepted=0 deferred=0
            ✓ Mixed-instance sessions

            - claude-code: no ancestor process detected here, skipped.

            - codex: no ancestor process detected here, skipped.

            - copilot: no ancestor process detected here, skipped.
            """);
    }

    // ---------- Mail-wake checks ----------

    [Fact]
    public async Task MailWake_HealthyLeader_ReportsReadyState_WithEpochAndLease()
    {
        // arrange
        await InitWorkspaceAsync();
        await InsertMailWakeLeaderRowAsync(
            FixedHost, epoch: 1, expiresAt: FakeTime.GetUtcNow() + TimeSpan.FromSeconds(30));

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("✓ Mail-wake", result.StdOut);
        Assert.Contains("Leader: ready (epoch=1, lease expires in 30s)", result.StdOut);
        Assert.Contains("Work: pending=0 accepted=0 deferred=0", result.StdOut);
    }

    [Fact]
    public async Task MailWake_ExpiredLease_NoPendingWork_ReportsExpiredState_ButStaysHealthy()
    {
        // arrange: a previous leader's lease lapsed and nobody has claimed
        // it since, but there is no work left waiting on it.
        await InitWorkspaceAsync();
        await InsertMailWakeLeaderRowAsync(
            FixedHost, epoch: 2, expiresAt: FakeTime.GetUtcNow() - TimeSpan.FromSeconds(10));

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("✓ Mail-wake", result.StdOut);
        Assert.Contains("Leader: expired (epoch=2, lease expired 10s ago)", result.StdOut);
        Assert.Contains("Work: pending=0 accepted=0 deferred=0", result.StdOut);
    }

    [Fact]
    public async Task MailWake_PendingWork_NoLeader_ReportsUnhealthy_WithStartDashboardRemediation()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "pascal");
        await InsertMailWakeOutboxRowAsync(
            FixedHost, "pascal", requestedGeneration: 1, settledGeneration: 0, dueAt: FakeTime.GetUtcNow());

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Mail-wake:", result.StdOut);
        Assert.Contains(
            "1 actor(s) have pending mail-wake work, but no dashboard leader is currently "
            + "running for this Nitro instance. Start the dashboard to process it.",
            result.StdOut);
        Assert.Contains("Leader: none", result.StdOut);
        Assert.Contains("Work: pending=1 accepted=0 deferred=0, oldest due 0s ago", result.StdOut);
    }

    [Fact]
    public async Task MailWake_PendingWork_ExpiredLease_ReportsUnhealthy_WithRestartDashboardRemediation()
    {
        // arrange: the same stuck-work signal, but a lease that once existed
        // has since expired rather than never having existed at all.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "pascal");
        await InsertMailWakeLeaderRowAsync(
            FixedHost, epoch: 3, expiresAt: FakeTime.GetUtcNow() - TimeSpan.FromSeconds(5));
        await InsertMailWakeOutboxRowAsync(
            FixedHost, "pascal", requestedGeneration: 1, settledGeneration: 0, dueAt: FakeTime.GetUtcNow());

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Mail-wake:", result.StdOut);
        Assert.Contains(
            "1 actor(s) have pending mail-wake work, but the dashboard leader's lease has "
            + "expired and nobody has re-acquired it. Start (or restart) the dashboard.",
            result.StdOut);
    }

    [Fact]
    public async Task MailWake_PendingWork_ReadyLeader_StaysHealthy_LeaderCanStillClaimIt()
    {
        // arrange: a live leader has not admitted this actor's work yet, but
        // it is still able to, so this is not yet a problem.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "pascal");
        await InsertMailWakeLeaderRowAsync(
            FixedHost, epoch: 1, expiresAt: FakeTime.GetUtcNow() + TimeSpan.FromSeconds(15));
        await InsertMailWakeOutboxRowAsync(
            FixedHost, "pascal", requestedGeneration: 1, settledGeneration: 0, dueAt: FakeTime.GetUtcNow());

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("✓ Mail-wake", result.StdOut);
        Assert.Contains("Work: pending=1 accepted=0 deferred=0, oldest due 0s ago", result.StdOut);
    }

    [Fact]
    public async Task MailWake_AcceptedWork_ActiveBatch_ReportsAcceptedCount_JsonOutput()
    {
        // arrange: a batch has already claimed this actor's generation, so
        // it counts as accepted rather than pending, independent of whether
        // the persistent daemon leader itself is running.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "pascal");
        await InsertMailWakeOutboxRowAsync(
            FixedHost, "pascal", requestedGeneration: 1, settledGeneration: 0, dueAt: FakeTime.GetUtcNow());
        await InsertMailWakeActiveBatchRowAsync(
            FixedHost, "pascal", claimedGeneration: 1, expiresAt: FakeTime.GetUtcNow() + TimeSpan.FromSeconds(30));
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var mailWake = document.RootElement.GetProperty("mailWake");
        Assert.True(mailWake.GetProperty("healthy").GetBoolean());
        Assert.Equal(0, mailWake.GetProperty("pendingActorCount").GetInt32());
        Assert.Equal(1, mailWake.GetProperty("acceptedActorCount").GetInt32());
        Assert.Equal(0, mailWake.GetProperty("deferredActorCount").GetInt32());
    }

    [Fact]
    public async Task MailWake_DeferredWork_DueInFuture_ReportsDeferredCount_JsonOutput()
    {
        // arrange: a retry backoff pushed this actor's due time into the
        // future; nobody has claimed it and it is not yet due.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "pascal");
        await InsertMailWakeOutboxRowAsync(
            FixedHost, "pascal", requestedGeneration: 1, settledGeneration: 0,
            dueAt: FakeTime.GetUtcNow() + TimeSpan.FromSeconds(60));
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var mailWake = document.RootElement.GetProperty("mailWake");
        Assert.True(mailWake.GetProperty("healthy").GetBoolean());
        Assert.Equal(0, mailWake.GetProperty("pendingActorCount").GetInt32());
        Assert.Equal(0, mailWake.GetProperty("acceptedActorCount").GetInt32());
        Assert.Equal(1, mailWake.GetProperty("deferredActorCount").GetInt32());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, mailWake.GetProperty("oldestPendingAgeSeconds").ValueKind);
    }

    [Fact]
    public async Task MailWake_NoWork_FullySettledOutboxRow_NotCounted_JsonOutput()
    {
        // arrange: a fully settled outbox row (nothing outstanding) is not
        // pending, accepted, or deferred work.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "pascal");
        await InsertMailWakeOutboxRowAsync(
            FixedHost, "pascal", requestedGeneration: 1, settledGeneration: 1, dueAt: FakeTime.GetUtcNow());
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var mailWake = document.RootElement.GetProperty("mailWake");
        Assert.True(mailWake.GetProperty("healthy").GetBoolean());
        Assert.Equal(0, mailWake.GetProperty("pendingActorCount").GetInt32());
        Assert.Equal(0, mailWake.GetProperty("acceptedActorCount").GetInt32());
        Assert.Equal(0, mailWake.GetProperty("deferredActorCount").GetInt32());
    }

    [Fact]
    public async Task MailWake_AccessDeniedTarget_ReportsUnhealthy_WithDegradedRemediation()
    {
        // arrange: a target durably stuck offered on a Claude access-denied
        // handoff, the read-only signal for a degraded dashboard daemon.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "pascal");
        await InsertMailWakeOutboxRowAsync(
            FixedHost, "pascal", requestedGeneration: 1, settledGeneration: 0, dueAt: FakeTime.GetUtcNow());
        var batchId = await InsertMailWakeActiveBatchRowAsync(
            FixedHost, "pascal", claimedGeneration: 1, expiresAt: FakeTime.GetUtcNow() + TimeSpan.FromSeconds(30));
        await InsertMailWakeTargetRowAsync(batchId, sessionId: "session-1", lastError: "access-denied");

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Mail-wake:", result.StdOut);
        Assert.Contains(
            "1 target(s) are stuck pending on a Claude access-denied handoff; the dashboard "
            + "daemon degraded and released leadership. Verify the dashboard's Claude access, "
            + "then it will re-elect and retry.",
            result.StdOut);
    }

    [Fact]
    public async Task MailWake_SchemaMismatch_ReportsFailure_WithoutLeaderOrWorkLines()
    {
        // arrange: a v3-shaped database predates the mail-wake tables
        // outright; the check must never query them.
        await SeedLegacySchemaVersionAsync(3);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Mail-wake:", result.StdOut);
        Assert.Contains(
            $"Mail-wake diagnostics require the current schema (v{AgentDatabase.CurrentVersion}); "
            + "this workspace is v3. Run `nitro agent init` to migrate.",
            result.StdOut);
        Assert.Contains("Session checks skipped: the schema is not current.", result.StdOut);
        Assert.DoesNotContain("Leader:", result.StdOut);
        Assert.DoesNotContain("Work:", result.StdOut);
    }

    [Fact]
    public async Task MailWake_DifferentNitroInstance_RowsNotCounted_JsonOutput()
    {
        // arrange: another Nitro instance sharing this workspace has its own
        // leader and pending work; neither belongs to this instance's report.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "zeta");
        await InsertMailWakeLeaderRowAsync(
            "some-other-instance", epoch: 5, expiresAt: FakeTime.GetUtcNow() + TimeSpan.FromSeconds(30));
        await InsertMailWakeOutboxRowAsync(
            "some-other-instance", "zeta", requestedGeneration: 1, settledGeneration: 0,
            dueAt: FakeTime.GetUtcNow());
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var mailWake = document.RootElement.GetProperty("mailWake");
        Assert.True(mailWake.GetProperty("healthy").GetBoolean());
        Assert.Equal("none", mailWake.GetProperty("leaderState").GetString());
        Assert.Equal(0, mailWake.GetProperty("pendingActorCount").GetInt32());
        Assert.Equal(0, mailWake.GetProperty("acceptedActorCount").GetInt32());
        Assert.Equal(0, mailWake.GetProperty("deferredActorCount").GetInt32());
    }

    private async Task InsertMailWakeLeaderRowAsync(
        string nitroInstanceId, long epoch, DateTimeOffset expiresAt, string? lastError = null)
    {
        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO mail_wake_daemons (nitro_instance_id, owner_id, epoch, leased_at, expires_at, last_error)
            VALUES ($nitroInstanceId, 'owner-test', $epoch, $leasedAt, $expiresAt, $lastError);
            """;
        command.Parameters.AddWithValue("$nitroInstanceId", nitroInstanceId);
        command.Parameters.AddWithValue("$epoch", epoch);
        command.Parameters.AddWithValue("$leasedAt", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("$expiresAt", expiresAt);
        command.Parameters.AddWithValue("$lastError", (object?)lastError ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private async Task InsertMailWakeOutboxRowAsync(
        string nitroInstanceId, string actor, long requestedGeneration, long settledGeneration, DateTimeOffset dueAt)
    {
        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO mail_wake_outbox (
                nitro_instance_id, actor, requested_generation, settled_generation, due_at, updated_at
            ) VALUES ($nitroInstanceId, $actor, $requestedGeneration, $settledGeneration, $dueAt, $now);
            """;
        command.Parameters.AddWithValue("$nitroInstanceId", nitroInstanceId);
        command.Parameters.AddWithValue("$actor", actor);
        command.Parameters.AddWithValue("$requestedGeneration", requestedGeneration);
        command.Parameters.AddWithValue("$settledGeneration", settledGeneration);
        command.Parameters.AddWithValue("$dueAt", dueAt);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Inserts a live <c>mail_wake_batches</c> row directly (bypassing
    /// <c>IMailWakeBatchStore</c>, which only ever claims against a due
    /// outbox row), so accepted-work scenarios can be seeded independently
    /// of the outbox row's own due time. Returns the generated batch id, for
    /// tests that also need to attach a target row.
    /// </summary>
    private async Task<string> InsertMailWakeActiveBatchRowAsync(
        string nitroInstanceId, string actor, long claimedGeneration, DateTimeOffset expiresAt)
    {
        var batchId = Guid.NewGuid().ToString("N");

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO mail_wake_batches (
                batch_id, nitro_instance_id, actor, claimed_generation, owner_id, attempt_id,
                status, claimed_at, expires_at
            ) VALUES (
                $batchId, $nitroInstanceId, $actor, $claimedGeneration, 'owner-test', 'attempt-test',
                'active', $now, $expiresAt
            );
            """;
        command.Parameters.AddWithValue("$batchId", batchId);
        command.Parameters.AddWithValue("$nitroInstanceId", nitroInstanceId);
        command.Parameters.AddWithValue("$actor", actor);
        command.Parameters.AddWithValue("$claimedGeneration", claimedGeneration);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("$expiresAt", expiresAt);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        return batchId;
    }

    private async Task InsertMailWakeTargetRowAsync(string batchId, string sessionId, string lastError)
    {
        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO mail_wake_targets (
                batch_id, harness, session_id, host, pid, proc_start, status, last_error, updated_at
            ) VALUES (
                $batchId, 'claude-code', $sessionId, $host, 12345, '999999999', 'pending', $lastError, $now
            );
            """;
        command.Parameters.AddWithValue("$batchId", batchId);
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$host", FixedHost);
        command.Parameters.AddWithValue("$lastError", lastError);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// A pid guaranteed to be alive for the duration of the test: the test
    /// host process itself, mirroring the convention in
    /// <c>AgentSessionRegistryTests</c> and <c>ListSessionCommandTests</c>.
    /// </summary>
    private static int CurrentAlivePid() => Environment.ProcessId;

    /// <summary>
    /// A pid that (barring extraordinary pid-space exhaustion) belongs to no
    /// running process, so it is reported dead.
    /// </summary>
    private const int DeadPid = 999_999;

    private async Task InsertSessionRowAsync(
        string host, string sessionId, string? agentName, string bindingKind, int pid,
        string? lastPingResult = null, string harness = "claude-code", string role = "",
        string harnessVersion = "", string processScope = "", string endpointKind = "none",
        string endpointAddr = "", DateTimeOffset? lastBeatAt = null)
    {
        var procStart = pid == CurrentAlivePid()
            ? ProcStat.ReadStartTicks(pid)!
            : "999999999";

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
                last_ping_result, role, harness_version, process_scope
            ) VALUES (
                $harness, $sessionId, $agentName, $bindingKind, $host, $pid, $procStart,
                '/work', '/work/.nitro/agents', $endpointKind, $endpointAddr, $now, $lastBeatAt,
                $lastPingResult, $role, $harnessVersion, $processScope
            );
            """;
        command.Parameters.AddWithValue("$harness", harness);
        command.Parameters.AddWithValue("$endpointAddr", endpointAddr);
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$agentName", (object?)agentName ?? DBNull.Value);
        command.Parameters.AddWithValue("$bindingKind", bindingKind);
        command.Parameters.AddWithValue("$host", host);
        command.Parameters.AddWithValue("$pid", pid);
        command.Parameters.AddWithValue("$procStart", procStart);
        command.Parameters.AddWithValue("$endpointKind", endpointKind);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("$lastBeatAt", lastBeatAt ?? DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("$lastPingResult", (object?)lastPingResult ?? DBNull.Value);
        command.Parameters.AddWithValue("$role", role);
        command.Parameters.AddWithValue("$harnessVersion", harnessVersion);
        command.Parameters.AddWithValue("$processScope", processScope);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Creates the workspace directory and a bare database file stamped
    /// with the given <c>user_version</c>, without running a real `agent
    /// init`, mirroring how <c>AgentDatabaseTests</c> seeds a legacy schema
    /// version. Doctor's version check only reads the pragma, so no table
    /// needs to exist for these tests.
    /// </summary>
    private async Task SeedLegacySchemaVersionAsync(long version)
    {
        Directory.CreateDirectory(WorkspaceDirectory);

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA user_version = {version};";
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
