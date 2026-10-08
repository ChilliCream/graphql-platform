using System.IO.Compression;
using System.Text.RegularExpressions;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers <c>agent backup</c>: the archive layout and manifest for each workspace shape,
/// the refusal rules, and the consistency of the archived <c>agents.db</c> snapshot.
/// </summary>
public sealed partial class BackupAgentCommandTests(NitroCommandFixture fixture)
    : AgentCommandTestBase(fixture)
{
    private string ArchivePath => Path.Combine(Directory.GetParent(WorkingDirectory)!.FullName, "backup.zip");

    private string ProjectNitroDirectory => Path.Combine(WorkingDirectory, ".nitro");

    private string GitWorkspaceDirectory => Path.Combine(WorkingDirectory, ".git", "nitro");

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "backup", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Back up the agent workspace to a zip archive.

            Usage:
              nitro agent backup [options]

            Options:
              --archive <archive> (REQUIRED)  The path of the workspace archive (.zip), relative to the current directory
              --force                         Overwrite the archive if it already exists
              --output <json>                 The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                  Show help and usage information

            Example:
              nitro agent backup --archive "./nitro-backup.zip"
              nitro agent backup --archive "./nitro-backup.zip" --force
            """);
    }

    [Fact]
    public async Task Execute_Should_ArchiveBothRoots_When_ProjectAndGitDirectoriesExist()
    {
        // arrange
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));
        await InitWorkspaceAsync();
        WriteFile(Path.Combine(ProjectNitroDirectory, "research", "epic-1-topic", "notes.md"), "notes");
        WriteFile(Path.Combine(GitWorkspaceDirectory, "memory", "curated", "fact.md"), "fact");
        WriteFile(Path.Combine(GitWorkspaceDirectory, "memory", ".local", "index.db"), "index");
        WriteFile(Path.Combine(GitWorkspaceDirectory, "agents.db-wal"), "wal");
        WriteFile(Path.Combine(GitWorkspaceDirectory, "agents.db-shm"), "shm");

        // act
        var result = await ExecuteCommandAsync("agent", "backup", "--archive", "../backup.zip");

        // assert
        result.AssertSuccess(
            "✓ Backed up 3 files from 'repo/.nitro' and 'git/nitro' to '" + ArchivePath + "'.");
        DescribeArchive(ArchivePath).MatchInlineSnapshot(
            """
            manifest.json
            repo/.nitro/research/epic-1-topic/notes.md
            git/nitro/agents.db
            git/nitro/memory/curated/fact.md

            {
              "formatVersion": 1,
              "cliVersion": "<version>",
              "createdAt": "2026-01-01T00:00:00+00:00",
              "databaseVersion": 18,
              "roots": [
                "repo/.nitro",
                "git/nitro"
              ]
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_ArchiveOnlyGitDirectory_When_ProjectDirectoryIsMissing()
    {
        // arrange
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "backup", "--archive", "../backup.zip");

        // assert
        result.AssertSuccess();
        DescribeArchive(ArchivePath).MatchInlineSnapshot(
            """
            manifest.json
            git/nitro/agents.db

            {
              "formatVersion": 1,
              "cliVersion": "<version>",
              "createdAt": "2026-01-01T00:00:00+00:00",
              "databaseVersion": 18,
              "roots": [
                "git/nitro"
              ]
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_SkipSymbolicLinks_When_TheWorkspaceLinksToOutsideFiles()
    {
        // arrange
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));
        await InitWorkspaceAsync();
        var outsideFile = Path.Combine(Directory.GetParent(WorkingDirectory)!.FullName, "secret.txt");
        WriteFile(outsideFile, "secret");
        WriteFile(Path.Combine(ProjectNitroDirectory, "research", "epic-1-topic", "notes.md"), "notes");
        File.CreateSymbolicLink(Path.Combine(ProjectNitroDirectory, "research", "secret.txt"), outsideFile);

        // act
        var result = await ExecuteCommandAsync("agent", "backup", "--archive", "../backup.zip");

        // assert
        result.AssertSuccess(
            "✓ Backed up 2 files from 'repo/.nitro' and 'git/nitro' to '" + ArchivePath + "'.");
        DescribeArchive(ArchivePath).MatchInlineSnapshot(
            """
            manifest.json
            repo/.nitro/research/epic-1-topic/notes.md
            git/nitro/agents.db

            {
              "formatVersion": 1,
              "cliVersion": "<version>",
              "createdAt": "2026-01-01T00:00:00+00:00",
              "databaseVersion": 18,
              "roots": [
                "repo/.nitro",
                "git/nitro"
              ]
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_PrintTheArchivePathVerbatim_When_ItContainsMarkupCharacters()
    {
        // arrange
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));
        await InitWorkspaceAsync();
        var archivePath = Path.Combine(Directory.GetParent(WorkingDirectory)!.FullName, "backup[1].zip");

        // act
        var result = await ExecuteCommandAsync("agent", "backup", "--archive", "../backup[1].zip");

        // assert
        result.AssertSuccess("✓ Backed up 1 file from 'git/nitro' to '" + archivePath + "'.");
    }

    [Fact]
    public async Task Execute_Should_ArchiveOnlyProjectDirectory_When_NoGitRepositoryExists()
    {
        // arrange
        await InitWorkspaceAsync();
        WriteFile(Path.Combine(ProjectNitroDirectory, "research", "epic-1-topic", "notes.md"), "notes");
        WriteFile(Path.Combine(WorkspaceDirectory, "memory", ".local", "index.db"), "index");
        WriteFile(Path.Combine(WorkspaceDirectory, "agents.db-wal"), "wal");

        // act
        var result = await ExecuteCommandAsync("agent", "backup", "--archive", "../backup.zip");

        // assert
        result.AssertSuccess();
        DescribeArchive(ArchivePath).MatchInlineSnapshot(
            """
            manifest.json
            repo/.nitro/agents/.gitignore
            repo/.nitro/agents/agents.db
            repo/.nitro/research/epic-1-topic/notes.md

            {
              "formatVersion": 1,
              "cliVersion": "<version>",
              "createdAt": "2026-01-01T00:00:00+00:00",
              "databaseVersion": 18,
              "roots": [
                "repo/.nitro"
              ]
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_Fail_When_NeitherDirectoryExists()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "backup", "--archive", "../backup.zip");

        // assert
        result.AssertError(
            """
            No agent workspace found to back up. Neither a '.nitro' directory nor a '.git/nitro' directory exists.
            """);
        Assert.False(File.Exists(ArchivePath));
    }

    [Fact]
    public async Task Execute_Should_Fail_When_ArchiveAlreadyExists()
    {
        // arrange
        await InitWorkspaceAsync();
        await File.WriteAllTextAsync(ArchivePath, "existing", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "backup", "--archive", "../backup.zip");

        // assert
        AssertNormalizedError(
            result,
            """
            The archive '<temp>/backup.zip' already exists. Use --force to overwrite it.
            """);
        Assert.Equal(
            "existing",
            await File.ReadAllTextAsync(ArchivePath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Execute_Should_ReplaceArchive_When_ForceIsGiven()
    {
        // arrange
        await InitWorkspaceAsync();
        await File.WriteAllTextAsync(ArchivePath, "existing", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "backup", "--archive", "../backup.zip", "--force");

        // assert
        result.AssertSuccess();
        DescribeArchive(ArchivePath).MatchInlineSnapshot(
            """
            manifest.json
            repo/.nitro/agents/.gitignore
            repo/.nitro/agents/agents.db

            {
              "formatVersion": 1,
              "cliVersion": "<version>",
              "createdAt": "2026-01-01T00:00:00+00:00",
              "databaseVersion": 18,
              "roots": [
                "repo/.nitro"
              ]
            }
            """);
        Assert.Equal([ArchivePath], Directory.GetFiles(Path.GetDirectoryName(ArchivePath)!));
    }

    [Fact]
    public async Task Execute_Should_Fail_When_ArchiveIsInsideBackedUpDirectory()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "backup", "--archive", ".nitro/backup.zip");

        // assert
        AssertNormalizedError(
            result,
            """
            The archive '<temp>/acme/.nitro/backup.zip' is inside '<temp>/acme/.nitro', which is part of the backup.
            """);
        Assert.False(File.Exists(Path.Combine(ProjectNitroDirectory, "backup.zip")));
    }

    [Fact]
    public async Task Execute_Should_ArchiveCommittedRows_When_SourceConnectionHoldsUncheckpointedWal()
    {
        // arrange
        // The open connection keeps the write-ahead log from being checkpointed or removed.
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));
        await InitWorkspaceAsync();

        var databasePath = AgentWorkspace.GetDatabasePath(GitWorkspaceDirectory);
        await using var holder = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        await holder.OpenAsync(TestContext.Current.CancellationToken);
        await ExecuteScalarAsync(holder, "SELECT COUNT(*) FROM agents;");

        await using (var writer = new SqliteConnection($"Data Source={databasePath};Pooling=False"))
        {
            await writer.OpenAsync(TestContext.Current.CancellationToken);
            await ExecuteScalarAsync(
                writer,
                "INSERT INTO agents (name, role, registered_at, started_at, last_seen_at) "
                + "VALUES ('maya', 'planner', 't', 't', 't') RETURNING name;");
        }

        var walLength = new FileInfo(databasePath + "-wal").Length;

        // act
        var result = await ExecuteCommandAsync("agent", "backup", "--archive", "../backup.zip");

        // assert
        result.AssertSuccess();
        Assert.True(walLength > 0);

        var snapshotPath = Path.Combine(Directory.GetParent(WorkingDirectory)!.FullName, "extracted.db");
        ExtractEntry(ArchivePath, "git/nitro/agents.db", snapshotPath);

        await using var snapshot = new SqliteConnection($"Data Source={snapshotPath};Pooling=False");
        await snapshot.OpenAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            "maya",
            await ExecuteScalarAsync(snapshot, "SELECT name FROM agents;"));
        Assert.Equal(
            AgentDatabase.CurrentVersion.ToString(),
            await ExecuteScalarAsync(snapshot, "PRAGMA user_version;"));
    }

    [Fact]
    public async Task JsonOutput_Should_PrintTheArchiveSummary_When_BackupSucceeds()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "backup", "--archive", "../backup.zip");

        // assert
        result.AssertSuccess();
        VolatileJsonValue().Replace(result.StdOut, "$1<volatile>").MatchInlineSnapshot(
            """
            {
              "archive": <volatile>,
              "roots": [
                "repo/.nitro"
              ],
              "fileCount": 2,
              "size": <volatile>
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_ClampEntryTimesToZipRange_When_FileTimesAreOutOfRange()
    {
        // arrange
        await InitWorkspaceAsync();
        WriteFile(Path.Combine(ProjectNitroDirectory, "times", "early.md"), "early");
        WriteFile(Path.Combine(ProjectNitroDirectory, "times", "late.md"), "late");
        File.SetLastWriteTime(
            Path.Combine(ProjectNitroDirectory, "times", "early.md"),
            new DateTime(1970, 1, 1, 12, 0, 0, DateTimeKind.Local));
        File.SetLastWriteTime(
            Path.Combine(ProjectNitroDirectory, "times", "late.md"),
            new DateTime(2200, 1, 1, 12, 0, 0, DateTimeKind.Local));

        // act
        var result = await ExecuteCommandAsync("agent", "backup", "--archive", "../backup.zip");

        // assert
        result.AssertSuccess();
        DescribeEntryTimes(ArchivePath, "repo/.nitro/times/").MatchInlineSnapshot(
            """
            repo/.nitro/times/early.md 1980-01-01 00:00:00
            repo/.nitro/times/late.md 2107-12-31 23:59:58
            """);
    }

    [Fact]
    public async Task Execute_Should_StoreLocalWriteTime_When_FileHasAValidTime()
    {
        // arrange
        // The wall-clock time is stored as is, so a UTC conversion would shift it by the local offset.
        await InitWorkspaceAsync();
        WriteFile(Path.Combine(ProjectNitroDirectory, "times", "normal.md"), "normal");
        File.SetLastWriteTime(
            Path.Combine(ProjectNitroDirectory, "times", "normal.md"),
            new DateTime(2024, 6, 15, 13, 45, 30, DateTimeKind.Local));

        // act
        var result = await ExecuteCommandAsync("agent", "backup", "--archive", "../backup.zip");

        // assert
        result.AssertSuccess();
        DescribeEntryTimes(ArchivePath, "repo/.nitro/times/").MatchInlineSnapshot(
            """
            repo/.nitro/times/normal.md 2024-06-15 13:45:30
            """);
    }

    private void AssertNormalizedError(CommandResult result, string expected)
    {
        Assert.Empty(result.StdOut);
        result.StdErr.Replace(Directory.GetParent(WorkingDirectory)!.FullName, "<temp>").MatchInlineSnapshot(expected);
        Assert.Equal(1, result.ExitCode);
    }

    private static void WriteFile(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static async Task<string?> ExecuteScalarAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var value = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        return value?.ToString();
    }

    private static void ExtractEntry(string archivePath, string entryName, string destinationPath)
    {
        using var archive = ZipFile.OpenRead(archivePath);

        archive.GetEntry(entryName)!.ExtractToFile(destinationPath);
    }

    /// <summary>
    /// Lists the stored wall-clock time of every archive entry under the given prefix.
    /// </summary>
    private static string DescribeEntryTimes(string archivePath, string prefix)
    {
        using var archive = ZipFile.OpenRead(archivePath);

        return string.Join(
            '\n',
            archive.Entries
                .Where(entry => entry.FullName.StartsWith(prefix, StringComparison.Ordinal))
                .Select(entry => $"{entry.FullName} {entry.LastWriteTime.DateTime:yyyy-MM-dd HH:mm:ss}"));
    }

    /// <summary>
    /// Lists the archive entries in order, followed by the manifest with the CLI version replaced.
    /// </summary>
    private static string DescribeArchive(string archivePath)
    {
        using var archive = ZipFile.OpenRead(archivePath);

        var manifestEntry = archive.GetEntry("manifest.json")!;
        using var reader = new StreamReader(manifestEntry.Open());
        var manifest = CliVersionLine().Replace(reader.ReadToEnd(), "$1<version>\"");

        return string.Join('\n', archive.Entries.Select(entry => entry.FullName))
            + "\n\n"
            + manifest;
    }

    [GeneratedRegex("""("cliVersion": ")[^"]*"?""")]
    private static partial Regex CliVersionLine();

    [GeneratedRegex("""("(?:archive|size)": )("[^"]*"|\d+)""")]
    private static partial Regex VolatileJsonValue();
}
