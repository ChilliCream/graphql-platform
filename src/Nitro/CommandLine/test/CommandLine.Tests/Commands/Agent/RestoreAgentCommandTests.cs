using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers <c>agent restore</c>: replacing both workspace folders from an archive, the checks that
/// reject an archive before anything is deleted, and the confirmation and liveness guards.
/// </summary>
public sealed partial class RestoreAgentCommandTests(NitroCommandFixture fixture)
    : AgentCommandTestBase(fixture)
{
    private const string EntryNotAllowedMessage =
        "The archive entry '{0}' is not allowed. Entries must be relative paths under a root "
        + "listed in the manifest, without '..' segments.";

    private string ArchivePath => Path.Combine(Directory.GetParent(WorkingDirectory)!.FullName, "backup.zip");

    private string ProjectNitroDirectory => Path.Combine(WorkingDirectory, ".nitro");

    private string GitWorkspaceDirectory => Path.Combine(WorkingDirectory, ".git", "nitro");

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "restore", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Replace the agent workspace with a backup archive. Deletes '.nitro' and '.git/nitro' first.

            Usage:
              nitro agent restore [options]

            Options:
              --archive <archive> (REQUIRED)  The path of the workspace archive (.zip), relative to the current directory
              --actor <actor>                 The acting agent, which does not block the restore; allocate one with `nitro agent login`
              --force                         Restore without confirmation, even while other agents use the workspace or their activity cannot be checked
              --output <json>                 The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                  Show help and usage information

            Example:
              nitro agent restore --archive "./nitro-backup.zip"
              nitro agent restore --archive "./nitro-backup.zip" --actor "maya"
              nitro agent restore --archive "./nitro-backup.zip" --force
            """);
    }

    [Fact]
    public async Task Execute_Should_RestoreSeededStateAndDropStrayFiles_When_ArchiveCoversBothRoots()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();

        await ExecuteCommandAsync("agent", "tasks", "create", "Stray task");
        File.Delete(Path.Combine(ProjectNitroDirectory, "research", "epic-1-topic", "notes.md"));
        WriteFile(Path.Combine(ProjectNitroDirectory, "stray.txt"), "stray");
        WriteFile(Path.Combine(GitWorkspaceDirectory, "stray.txt"), "stray");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");

        // assert
        result.AssertSuccess($"✓ Restored 2 files from '{ArchivePath}'.");
        DescribeWorkspace().MatchInlineSnapshot(
            """
            . (working directory)
              .git
              .nitro
            .git
              nitro
            .nitro/research/epic-1-topic/notes.md
            .git/nitro/agents.db
            """);
    }

    [Fact]
    public async Task Execute_Should_ReadRestoredTasksAndMail_When_ArchiveIsRestored()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        await ExecuteCommandAsync("agent", "tasks", "create", "Stray task");

        // act
        var restore = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");
        var tasks = await ExecuteCommandAsync("agent", "tasks", "list");
        var inbox = await ExecuteCommandAsync("agent", "mail", "inbox");

        // assert
        restore.AssertSuccess();
        tasks.StdOut.TrimEnd().MatchInlineSnapshot(
            """
            acme-4ji  P2  task  open  Seeded task

            1 task(s)
            """);
        MessageId().Replace(inbox.StdOut.TrimEnd(), "<id>")
            .MatchInlineSnapshot(
                """
                <id>  *  maya  Seeded mail  now

                1 message(s)
                """);
    }

    [Fact]
    public async Task Execute_Should_Fail_When_ArchiveDoesNotExist()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../missing.zip", "--force");

        // assert
        AssertNormalizedError(
            result,
            """
            The archive '<temp>/missing.zip' does not exist.
            """);
        Assert.True(File.Exists(Path.Combine(ProjectNitroDirectory, "research", "epic-1-topic", "notes.md")));
    }

    [Fact]
    public async Task Execute_Should_Fail_When_ArchiveIsNotAZipFile()
    {
        // arrange
        await File.WriteAllTextAsync(ArchivePath, "not a zip", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");

        // assert
        AssertNormalizedError(
            result,
            """
            The archive '<temp>/backup.zip' is not a valid zip file.
            """);
    }

    [Fact]
    public async Task Execute_Should_Fail_When_ManifestIsMissing()
    {
        // arrange
        CreateArchive(manifest: null, ("repo/.nitro/notes.md", "notes"u8.ToArray()));

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");

        // assert
        AssertNormalizedError(
            result,
            """
            The archive '<temp>/backup.zip' has no 'manifest.json', so it is not an agent workspace backup.
            """);
    }

    [Fact]
    public async Task Execute_Should_LeaveLocalFoldersUntouched_When_FormatVersionIsUnknown()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        CreateArchive(Manifest(formatVersion: 2, "repo/.nitro"), ("repo/.nitro/notes.md", "notes"u8.ToArray()));

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");

        // assert
        AssertNormalizedError(
            result,
            """
            The archive format version 2 is not supported. This version of Nitro reads format version 1.
            """);
        Assert.True(File.Exists(Path.Combine(ProjectNitroDirectory, "research", "epic-1-topic", "notes.md")));
    }

    [Fact]
    public async Task Execute_Should_LeaveLocalFoldersUntouched_When_ManifestListsNoRoots()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        CreateArchive(Manifest(formatVersion: 1));

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");

        // assert
        AssertNormalizedError(
            result,
            """
            The 'manifest.json' in archive '<temp>/backup.zip' is not a valid agent workspace manifest.
            """);
        Assert.True(File.Exists(Path.Combine(ProjectNitroDirectory, "research", "epic-1-topic", "notes.md")));
        Assert.True(File.Exists(Path.Combine(GitWorkspaceDirectory, "agents.db")));
    }

    [Fact]
    public async Task Execute_Should_LeaveLocalFoldersUntouched_When_ProjectDatabaseSchemaIsNewer()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        CreateArchive(
            Manifest(1, "repo/.nitro"),
            ("repo/.nitro/agents/agents.db", DatabaseHeader(AgentDatabase.CurrentVersion + 1)));

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");

        // assert
        AssertNormalizedError(
            result,
            $"""
            The agent database 'repo/.nitro/agents/agents.db' in the archive has schema version {AgentDatabase.CurrentVersion + 1}, which is newer than the version {AgentDatabase.CurrentVersion} this version of Nitro supports. Update Nitro to restore it.
            """);
        Assert.True(File.Exists(Path.Combine(ProjectNitroDirectory, "research", "epic-1-topic", "notes.md")));
    }

    [Fact]
    public async Task Execute_Should_RejectTheArchive_When_OnlyTheSecondDatabaseSchemaIsNewer()
    {
        // arrange
        // The manifest records the version of the first database only, so both must be read.
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));
        CreateArchive(
            Manifest(1, "repo/.nitro", "git/nitro"),
            ("repo/.nitro/agents/agents.db", DatabaseHeader(AgentDatabase.CurrentVersion)),
            ("git/nitro/agents.db", DatabaseHeader(AgentDatabase.CurrentVersion + 1)));

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");

        // assert
        AssertNormalizedError(
            result,
            $"""
            The agent database 'git/nitro/agents.db' in the archive has schema version {AgentDatabase.CurrentVersion + 1}, which is newer than the version {AgentDatabase.CurrentVersion} this version of Nitro supports. Update Nitro to restore it.
            """);
    }

    [Fact]
    public async Task Execute_Should_RestoreOlderDatabase_When_SchemaVersionIsOlder()
    {
        // arrange
        CreateArchive(
            Manifest(1, "repo/.nitro"),
            ("repo/.nitro/agents/agents.db", DatabaseHeader(AgentDatabase.CurrentVersion - 1)));

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");

        // assert
        result.AssertSuccess($"✓ Restored 1 file from '{ArchivePath}'.");
    }

    [Theory]
    [InlineData("repo/.nitro/../../evil.txt")]
    [InlineData("repo/.nitro/a/../../../evil.txt")]
    [InlineData("repo/.nitro\\..\\evil.txt")]
    [InlineData("/tmp/evil.txt")]
    [InlineData("other/evil.txt")]
    [InlineData("repo/.nitro//evil.txt")]
    public async Task Execute_Should_LeaveLocalFoldersUntouched_When_EntryEscapesTheRoots(string entryName)
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        CreateArchive(
            Manifest(1, "repo/.nitro"),
            ("repo/.nitro/fine.md", "fine"u8.ToArray()),
            (entryName, "evil"u8.ToArray()));

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");

        // assert
        result.AssertError(string.Format(EntryNotAllowedMessage, entryName));
        Assert.Equal(
            "notes",
            await File.ReadAllTextAsync(
                Path.Combine(ProjectNitroDirectory, "research", "epic-1-topic", "notes.md"),
                TestContext.Current.CancellationToken));
        Assert.False(File.Exists(Path.Combine(Directory.GetParent(WorkingDirectory)!.FullName, "evil.txt")));
        Assert.False(File.Exists(Path.Combine(WorkingDirectory, "evil.txt")));
    }

    [Fact]
    public async Task Execute_Should_Fail_When_ArchiveIsInsideADirectoryThatRestoreDeletes()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        File.Move(ArchivePath, Path.Combine(ProjectNitroDirectory, "backup.zip"));

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", ".nitro/backup.zip", "--force");

        // assert
        AssertNormalizedError(
            result,
            """
            The archive '<temp>/acme/.nitro/backup.zip' is inside '<temp>/acme/.nitro', which restore deletes.
            """);
        Assert.True(File.Exists(Path.Combine(ProjectNitroDirectory, "backup.zip")));
    }

    [Fact]
    public async Task Execute_Should_Fail_When_ArchiveHasGitRootOutsideAGitRepository()
    {
        // arrange
        CreateArchive(
            Manifest(1, "git/nitro"),
            ("git/nitro/agents.db", DatabaseHeader(AgentDatabase.CurrentVersion)));

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");

        // assert
        AssertNormalizedError(
            result,
            """
            The archive '<temp>/backup.zip' contains a '.git/nitro' directory, but the current directory is not inside a git repository.
            """);
    }

    [Fact]
    public async Task Execute_Should_DeleteNothing_When_NonInteractiveWithoutForce()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "restore", "--archive", "../backup.zip");

        // assert
        result.AssertError("Use --force to restore without confirmation.");
        Assert.True(File.Exists(Path.Combine(ProjectNitroDirectory, "research", "epic-1-topic", "notes.md")));
    }

    [Fact]
    public async Task Execute_Should_DeleteNothing_When_ConfirmationIsDeclined()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        WriteFile(Path.Combine(ProjectNitroDirectory, "stray.txt"), "stray");
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand("agent", "restore", "--archive", "../backup.zip");

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Aborted.", result.StdOut);
        Assert.True(File.Exists(Path.Combine(ProjectNitroDirectory, "stray.txt")));
    }

    [Fact]
    public async Task Execute_Should_RestoreAfterConfirmation_When_Interactive()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        WriteFile(Path.Combine(ProjectNitroDirectory, "stray.txt"), "stray");
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand("agent", "restore", "--archive", "../backup.zip");

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.False(File.Exists(Path.Combine(ProjectNitroDirectory, "stray.txt")));
        Assert.True(File.Exists(Path.Combine(ProjectNitroDirectory, "research", "epic-1-topic", "notes.md")));
    }

    [Fact]
    public async Task Execute_Should_RefuseAndListAgents_When_OtherAgentsAreOnlineOrIdle()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        await SeedGitAgentAsync("nova");
        await SeedGitAgentAsync("mira");
        await SetAgentLastSeenAsync("mira", FakeTime.GetUtcNow().AddHours(-2));
        await SeedGitAgentAsync("zed");
        await EndAgentAsync("zed");
        WriteFile(Path.Combine(ProjectNitroDirectory, "stray.txt"), "stray");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip");

        // assert
        result.AssertError(
            "Agents are active in this workspace and hold its database open: mira, nova. "
            + "Use --force to restore anyway.");
        Assert.True(File.Exists(Path.Combine(ProjectNitroDirectory, "stray.txt")));
    }

    [Fact]
    public async Task Execute_Should_Restore_When_OtherAgentsAreActiveAndForceIsGiven()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        await SeedGitAgentAsync("nova");
        WriteFile(Path.Combine(ProjectNitroDirectory, "stray.txt"), "stray");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");

        // assert
        result.AssertSuccess();
        Assert.False(File.Exists(Path.Combine(ProjectNitroDirectory, "stray.txt")));
        Assert.Equal(
            "0",
            await ScalarAsync(GitWorkspaceDirectory, "SELECT COUNT(*) FROM agents WHERE name = 'nova';"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execute_Should_Refuse_When_AMailWakeDaemonHoldsTheLease(bool force)
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        WriteFile(Path.Combine(ProjectNitroDirectory, "stray.txt"), "stray");

        var expiresAt = FakeTime.GetUtcNow().AddSeconds(10);
        await ExecuteAsync(
            GitWorkspaceDirectory,
            "INSERT INTO mail_wake_daemons (id, owner_token, acquired_at, heartbeat_at, expires_at) "
            + $"VALUES (1, 'daemon-x', '{expiresAt:O}', '{expiresAt:O}', '{expiresAt:O}');");

        string[] arguments = force
            ? ["agent", "restore", "--archive", "../backup.zip", "--force"]
            : ["agent", "restore", "--archive", "../backup.zip"];

        // act
        var result = await ExecuteCommandAsync(arguments);

        // assert
        AssertNormalizedError(
            result,
            """
            A mail wake daemon holds the database in '<temp>/acme/.git/nitro' (lease until 2026-01-01 00:00:10Z). Close the `nitro agent` board or the process running it, then restore again.
            """);
        Assert.True(File.Exists(Path.Combine(ProjectNitroDirectory, "stray.txt")));
    }

    [Fact]
    public async Task Execute_Should_RestoreAfterConfirmation_When_TheActorIsTheOnlyActiveAgent()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        await SeedGitAgentAsync("nova");
        WriteFile(Path.Combine(ProjectNitroDirectory, "stray.txt"), "stray");
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "agent", "restore", "--archive", "../backup.zip", "--actor", "nova");

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.False(File.Exists(Path.Combine(ProjectNitroDirectory, "stray.txt")));
    }

    [Fact]
    public async Task Execute_Should_RefuseAndListOnlyOtherAgents_When_TheActorAndOthersAreActive()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        await SeedGitAgentAsync("nova");
        await SeedGitAgentAsync("mira");
        WriteFile(Path.Combine(ProjectNitroDirectory, "stray.txt"), "stray");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--actor", "nova");

        // assert
        result.AssertError(
            "Agents are active in this workspace and hold its database open: mira. "
            + "Use --force to restore anyway.");
        Assert.True(File.Exists(Path.Combine(ProjectNitroDirectory, "stray.txt")));
    }

    [Fact]
    public async Task Execute_Should_RefuseWithoutForce_When_ADatabaseCannotBeRead()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        CorruptGitDatabase();
        WriteFile(Path.Combine(ProjectNitroDirectory, "stray.txt"), "stray");
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand("agent", "restore", "--archive", "../backup.zip");

        // act
        var result = await command.RunToCompletionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, result.ExitCode);
        result.StdErr.Replace(Directory.GetParent(WorkingDirectory)!.FullName, "<temp>").MatchInlineSnapshot(
            """
            Could not check whether agents are using the workspace database: <temp>/acme/.git/nitro/agents.db. Use --force to restore anyway.
            """);
        Assert.True(File.Exists(Path.Combine(ProjectNitroDirectory, "stray.txt")));
    }

    [Fact]
    public async Task Execute_Should_RestoreTheDatabase_When_ItCannotBeReadAndForceIsGiven()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        CorruptGitDatabase();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");

        // assert
        result.AssertSuccess();
        Assert.Equal("2", await ScalarAsync(GitWorkspaceDirectory, "SELECT COUNT(*) FROM agents;"));
    }

    [Fact]
    public async Task Execute_Should_RefuseWithoutForce_When_TheRegistryTableIsMissing()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        await ExecuteAsync(GitWorkspaceDirectory, "ALTER TABLE agents RENAME TO agents_old;");

        // act
        var result = await ExecuteCommandAsync("agent", "restore", "--archive", "../backup.zip");

        // assert
        AssertNormalizedError(
            result,
            """
            Could not check whether agents are using the workspace database: <temp>/acme/.git/nitro/agents.db. Use --force to restore anyway.
            """);
    }

    [Theory]
    [InlineData("repo/.nitro/agents/AGENTS.DB")]
    [InlineData("repo/.nitro/Agents/agents.db")]
    [InlineData("git/nitro/AGENTS.DB")]
    public async Task Execute_Should_RejectTheArchive_When_ADatabaseEntryWithADifferentCaseHasANewerSchema(
        string entryName)
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        CreateArchive(
            Manifest(1, "repo/.nitro", "git/nitro"),
            (entryName, DatabaseHeader(AgentDatabase.CurrentVersion + 1)));

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");

        // assert
        result.AssertError(
            $"The agent database '{entryName}' in the archive has schema version {AgentDatabase.CurrentVersion + 1}, "
            + $"which is newer than the version {AgentDatabase.CurrentVersion} this version of Nitro supports. "
            + "Update Nitro to restore it.");
        Assert.True(File.Exists(Path.Combine(ProjectNitroDirectory, "research", "epic-1-topic", "notes.md")));
    }

    [Fact]
    public async Task Execute_Should_SucceedAndWarn_When_TheReplacedFolderCannotBeDeleted()
    {
        // arrange
        SkipUnlessPermissionsAreEnforced();

        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        LockProjectFolderAgainstDeletion();

        try
        {
            // act
            var result = await ExecuteCommandAsync(
                "agent", "restore", "--archive", "../backup.zip", "--force");

            // assert
            Assert.Equal(0, result.ExitCode);
            Assert.Empty(result.StdErr);
            ReplacedSuffix().Replace(
                result.StdOut.Replace(Directory.GetParent(WorkingDirectory)!.FullName, "<temp>"),
                "<id>")
                .MatchInlineSnapshot(
                    """
                    ✓ Restored 2 files from '<temp>/backup.zip'.
                    Could not delete the replaced folder '<temp>/acme/.nitro-replaced-<id>'. Remove it manually.
                    """);
            Assert.True(File.Exists(Path.Combine(ProjectNitroDirectory, "research", "epic-1-topic", "notes.md")));
        }
        finally
        {
            UnlockLeftoverFolders();
        }
    }

    [Fact]
    public async Task JsonOutput_Should_ListTheLeftoverFolder_When_TheReplacedFolderCannotBeDeleted()
    {
        // arrange
        SkipUnlessPermissionsAreEnforced();

        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        LockProjectFolderAgainstDeletion();
        SetupInteractionMode(InteractionMode.JsonOutput);

        try
        {
            // act
            var result = await ExecuteCommandAsync(
                "agent", "restore", "--archive", "../backup.zip", "--force");

            // assert
            result.AssertSuccess();
            ReplacedSuffix().Replace(
                result.StdOut
                    .Replace(ArchivePath, "<archive>")
                    .Replace(WorkingDirectory, "<work>"),
                "<id>")
                .MatchInlineSnapshot(
                    """
                    {
                      "archive": "<archive>",
                      "roots": [
                        "repo/.nitro",
                        "git/nitro"
                      ],
                      "fileCount": 2,
                      "leftoverDirectories": [
                        "<work>/.nitro-replaced-<id>"
                      ]
                    }
                    """);
        }
        finally
        {
            UnlockLeftoverFolders();
        }
    }

    [Fact]
    public async Task Execute_Should_PutTheOriginalFoldersBack_When_TheSwapFails()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        WriteFile(Path.Combine(ProjectNitroDirectory, "stray.txt"), "stray");
        Directory.Delete(GitWorkspaceDirectory, recursive: true);
        await File.WriteAllTextAsync(GitWorkspaceDirectory, "blocker", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");

        // assert
        Assert.Equal(1, result.ExitCode);
        DescribeWorkspace().MatchInlineSnapshot(
            """
            . (working directory)
              .git
              .nitro
            .git
              nitro
            .nitro/research/epic-1-topic/notes.md
            .nitro/stray.txt
            """);
    }

    [Fact]
    public async Task Execute_Should_PutTheGitFolderBack_When_TheProjectFolderCannotBePlaced()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        Directory.Delete(ProjectNitroDirectory, recursive: true);
        await File.WriteAllTextAsync(ProjectNitroDirectory, "blocker", TestContext.Current.CancellationToken);
        var workspaceBefore = DescribeWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Equal(workspaceBefore, DescribeWorkspace());
        Assert.Contains(".git/nitro/agents.db", workspaceBefore);
    }

    [Fact]
    public async Task Execute_Should_AskBeforeDeletingBothFolders_When_Interactive()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand("agent", "restore", "--archive", "../backup.zip");

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync(TestContext.Current.CancellationToken);

        // assert
        result.StdOut.Replace(Directory.GetParent(WorkingDirectory)!.FullName, "<temp>")
            .MatchInlineSnapshot(
                """
                ? Delete '<temp>/acme/.nitro' and '<temp>/acme/.git/nitro' and restore from '<temp>/backup.zip'? [y/n] (y): n
                Aborted.
                """);
    }

    [Fact]
    public async Task Execute_Should_DeleteTheProjectFolder_When_ArchiveHasNoProjectRoot()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        Directory.Delete(ProjectNitroDirectory, recursive: true);
        await ExecuteCommandAsync("agent", "backup", "--archive", "../git-only.zip");
        WriteFile(Path.Combine(ProjectNitroDirectory, "stray.txt"), "stray");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../git-only.zip", "--force");

        // assert
        result.AssertSuccess($"✓ Restored 1 file from '{Path.Combine(Directory.GetParent(WorkingDirectory)!.FullName, "git-only.zip")}'.");
        DescribeWorkspace().MatchInlineSnapshot(
            """
            . (working directory)
              .git
            .git
              nitro
            .git/nitro/agents.db
            """);
    }

    [Fact]
    public async Task JsonOutput_Should_PrintTheRestoreSummary_When_RestoreSucceeds()
    {
        // arrange
        await SeedWorkspaceAsync("Seeded task", "Seeded mail");
        await BackupAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "restore", "--archive", "../backup.zip", "--force");

        // assert
        result.AssertSuccess();
        result.StdOut.Replace(ArchivePath, "<archive>").MatchInlineSnapshot(
            """
            {
              "archive": "<archive>",
              "roots": [
                "repo/.nitro",
                "git/nitro"
              ],
              "fileCount": 2,
              "leftoverDirectories": []
            }
            """);
    }

    private async Task SeedWorkspaceAsync(string taskTitle, string mailSubject)
    {
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));
        await InitWorkspaceAsync();

        await ExecuteCommandAsync("agent", "tasks", "create", taskTitle);
        WriteFile(Path.Combine(ProjectNitroDirectory, "research", "epic-1-topic", "notes.md"), "notes");

        await SeedGitAgentAsync("maya");
        await SeedGitAgentAsync("test-agent");

        var mail = new MailStore(
            new TestFileSystem(WorkingDirectory),
            FakeTime,
            new AgentDatabase(),
            new AgentStore(new TestFileSystem(WorkingDirectory), FakeTime, new AgentDatabase()));

        await mail.SendMessageAsync(
            new MailMessageCreation
            {
                Sender = "maya",
                Subject = mailSubject,
                Body = "body",
                To = ["test-agent"],
                Cc = []
            },
            TestContext.Current.CancellationToken);

        await EndAgentAsync("maya");
        await EndAgentAsync("test-agent");
    }

    private async Task BackupAsync()
    {
        var result = await ExecuteCommandAsync("agent", "backup", "--archive", "../backup.zip");
        Assert.Equal(0, result.ExitCode);
    }

    private Task SeedGitAgentAsync(string name)
        => ExecuteAsync(
            GitWorkspaceDirectory,
            "INSERT INTO agents "
            + "(name, role, registered_at, started_at, last_seen_at, endpoint_kind, endpoint_addr) "
            + $"VALUES ('{name}', '', '{FakeTime.GetUtcNow():O}', '{FakeTime.GetUtcNow():O}', "
            + $"'{FakeTime.GetUtcNow():O}', 'db-watch', 'test');");

    private Task EndAgentAsync(string name)
        => ExecuteAsync(
            GitWorkspaceDirectory,
            $"UPDATE agents SET ended_at = '{FakeTime.GetUtcNow():O}' WHERE name = '{name}';");

    private Task SetAgentLastSeenAsync(string name, DateTimeOffset lastSeenAt)
        => ExecuteAsync(
            GitWorkspaceDirectory,
            $"UPDATE agents SET last_seen_at = '{lastSeenAt:O}' WHERE name = '{name}';");

    private static void SkipUnlessPermissionsAreEnforced()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip("Unix directory-permission denial has no Windows equivalent.");
        }

        if (Environment.IsPrivilegedProcess)
        {
            Assert.Skip("Running as root bypasses the directory permission check.");
        }
    }

    private void LockProjectFolderAgainstDeletion()
    {
        var lockedDirectory = Path.Combine(ProjectNitroDirectory, "locked");
        WriteFile(Path.Combine(lockedDirectory, "inner.txt"), "inner");

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(lockedDirectory, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        }
    }

    private void UnlockLeftoverFolders()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        foreach (var leftover in Directory.GetDirectories(WorkingDirectory, ".nitro-replaced-*"))
        {
            File.SetUnixFileMode(
                Path.Combine(leftover, "locked"),
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private void CorruptGitDatabase()
    {
        var databasePath = AgentWorkspace.GetDatabasePath(GitWorkspaceDirectory);

        File.Delete(databasePath + "-wal");
        File.Delete(databasePath + "-shm");
        File.WriteAllBytes(databasePath, new byte[512]);
    }

    private void AssertNormalizedError(CommandResult result, string expected)
    {
        Assert.Empty(result.StdOut);
        result.StdErr.Replace(Directory.GetParent(WorkingDirectory)!.FullName, "<temp>").MatchInlineSnapshot(expected);
        Assert.Equal(1, result.ExitCode);
    }

    /// <summary>
    /// Lists the entries next to the restored folders, so leftover staging directories show up, and
    /// every file under the two workspace folders.
    /// </summary>
    private string DescribeWorkspace()
    {
        var lines = new List<string>
        {
            ". (working directory)"
        };

        lines.AddRange(ListNames(WorkingDirectory).Select(name => "  " + name));
        lines.Add(".git");
        lines.AddRange(ListNames(Path.Combine(WorkingDirectory, ".git")).Select(name => "  " + name));

        foreach (var (label, directory) in new[] { (".nitro", ProjectNitroDirectory), (".git/nitro", GitWorkspaceDirectory) })
        {
            if (Directory.Exists(directory))
            {
                lines.AddRange(
                    Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                        .Select(path => label + "/" + Path.GetRelativePath(directory, path).Replace('\\', '/'))
                        .Order(StringComparer.Ordinal));
            }
        }

        return string.Join('\n', lines);
    }

    private static IEnumerable<string> ListNames(string directory)
        => Directory.GetFileSystemEntries(directory)
            .Select(path => Path.GetFileName(path))
            .Order(StringComparer.Ordinal);

    private void CreateArchive(string? manifest, params (string Name, byte[] Content)[] entries)
    {
        using var stream = File.Create(ArchivePath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

        if (manifest is not null)
        {
            Add(archive, "manifest.json", Encoding.UTF8.GetBytes(manifest));
        }

        foreach (var (name, content) in entries)
        {
            Add(archive, name, content);
        }

        static void Add(ZipArchive archive, string name, byte[] content)
        {
            using var entryStream = archive.CreateEntry(name).Open();
            entryStream.Write(content);
        }
    }

    private static string Manifest(int formatVersion, params string[] roots)
        => $$"""
            {
              "formatVersion": {{formatVersion}},
              "cliVersion": "1.0.0",
              "createdAt": "2026-01-01T00:00:00+00:00",
              "databaseVersion": null,
              "roots": [{{string.Join(", ", roots.Select(root => $"\"{root}\""))}}]
            }
            """;

    private static byte[] DatabaseHeader(int userVersion)
    {
        var header = new byte[100];
        "SQLite format 3\0"u8.CopyTo(header);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(60, 4), userVersion);

        return header;
    }

    private static void WriteFile(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static async Task ExecuteAsync(string workspaceDirectory, string sql)
    {
        await using var connection = new SqliteConnection(
            $"Data Source={AgentWorkspace.GetDatabasePath(workspaceDirectory)};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<string?> ScalarAsync(string workspaceDirectory, string sql)
    {
        await using var connection = new SqliteConnection(
            $"Data Source={AgentWorkspace.GetDatabasePath(workspaceDirectory)};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        return (await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))?.ToString();
    }

    [GeneratedRegex("^m-[a-z0-9]+", RegexOptions.Multiline)]
    private static partial Regex MessageId();

    [GeneratedRegex("(?<=-replaced-)[0-9a-f]{32}")]
    private static partial Regex ReplacedSuffix();
}
