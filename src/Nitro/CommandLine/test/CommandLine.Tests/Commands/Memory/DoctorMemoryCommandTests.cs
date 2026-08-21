using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

public sealed class DoctorMemoryCommandTests(NitroCommandFixture fixture)
    : MemoryCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "memory", "doctor", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Check the memory store for integrity problems.

            Usage:
              nitro agent memory doctor [options]

            Options:
              --scope <all|global|project>  The memory scope to read from (project, global, or all) [default: all]
              --output <json>               The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information

            Example:
              nitro agent memory doctor
              nitro agent memory doctor --scope global
            """);
    }

    [Fact]
    public async Task HealthyWorkspace_ReturnsSuccess()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Project note.");
        await SeedJournalEntryAsync("Project journal entry.");
        await SeedMemoryAsync("Global note.", scope: "global");

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Scope: project", result.StdOut);
        Assert.Contains("Scope: global", result.StdOut);
        Assert.Contains("✓ Cross-scope duplicate ids", result.StdOut);
        Assert.DoesNotContain("FAIL", result.StdOut);
    }

    [Fact]
    public async Task JsonOutput_HealthyWorkspace_ReturnsStructuredReport()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Project note.");
        await SeedMemoryAsync("Global note.", scope: "global");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.True(root.GetProperty("healthy").GetBoolean());
        Assert.Empty(root.GetProperty("crossScopeDuplicates").EnumerateArray());

        var scopes = root.GetProperty("scopes").EnumerateArray().ToArray();
        Assert.Equal(2, scopes.Length);
        Assert.Equal("project", scopes[0].GetProperty("scope").GetString());
        Assert.Equal(1, scopes[0].GetProperty("curatedCount").GetInt32());
        Assert.Equal("global", scopes[1].GetProperty("scope").GetString());
        Assert.Equal(1, scopes[1].GetProperty("curatedCount").GetInt32());
    }

    [Fact]
    public async Task MalformedCuratedFrontmatter_ReportedAndReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var record = await SeedMemoryAsync("Good note.");
        await File.WriteAllTextAsync(
            record.Path, "not frontmatter at all", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "doctor", "--scope", "project");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Frontmatter:", result.StdOut);
        Assert.Contains($"curated {record.Path}:", result.StdOut);
    }

    [Fact]
    public async Task MalformedJournalFrontmatter_ReportedAndReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var entry = await SeedJournalEntryAsync("Good entry.");
        await File.WriteAllTextAsync(
            entry.Path, "not frontmatter at all", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "doctor", "--scope", "project");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Frontmatter:", result.StdOut);
        Assert.Contains($"journal {entry.Path}:", result.StdOut);
    }

    [Fact]
    public async Task CrossScopeDuplicateCuratedId_ReportedAndReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var projectRecord = await SeedMemoryAsync("Project text.");
        Directory.CreateDirectory(GlobalCuratedDirectory);
        File.Copy(projectRecord.Path, Path.Combine(GlobalCuratedDirectory, projectRecord.Id + ".md"));

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Cross-scope duplicate ids:", result.StdOut);
        Assert.Contains($"'{projectRecord.Id}' exists in project and global", result.StdOut);
    }

    [Fact]
    public async Task CrossScopeDuplicateId_AlsoDuplicatedWithinProjectCollections_ScopesAreDistinct()
    {
        // An id present in project curated, project journal, AND global
        // (three entries, two of them both scope "project") must still
        // report Scopes as the distinct set ["project", "global"], not a
        // duplicated ["project", "project", "global"].

        // arrange
        await InitWorkspaceAsync();
        var entry = await SeedJournalEntryAsync("Project journal entry.");
        Directory.CreateDirectory(CuratedDirectory);
        var curatedFrontmatter =
            $"""
            ---
            schema: 1
            id: {entry.Id}
            type: fact
            tags: []
            created_at: 2026-01-01T00:00:00Z
            updated_at: 2026-01-01T00:00:00Z
            created_by: test-agent
            ---
            Duplicated as curated too.
            """;
        var curatedPath = Path.Combine(CuratedDirectory, entry.Id + ".md");
        await File.WriteAllTextAsync(
            curatedPath, curatedFrontmatter, TestContext.Current.CancellationToken);
        Directory.CreateDirectory(GlobalCuratedDirectory);
        File.Copy(curatedPath, Path.Combine(GlobalCuratedDirectory, entry.Id + ".md"));
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        var conflicts = root.GetProperty("crossScopeDuplicates").EnumerateArray().ToArray();
        var conflict = Assert.Single(conflicts);
        Assert.Equal(entry.Id, conflict.GetProperty("id").GetString());
        var scopes = conflict.GetProperty("scopes").EnumerateArray()
            .Select(s => s.GetString())
            .ToArray();
        Assert.Equal(new[] { "project", "global" }, scopes);
        Assert.Equal(3, conflict.GetProperty("paths").GetArrayLength());
    }

    [Fact]
    public async Task SameScopeCollectionDuplicateId_ReportedAndReturnsError()
    {
        // An id duplicated between curated/ and journal/ of the SAME scope
        // is invalid data too, distinct from a cross-scope duplicate, and
        // must be reported even when only one scope is inspected.

        // arrange
        await InitWorkspaceAsync();
        var entry = await SeedJournalEntryAsync("Project journal entry.");
        Directory.CreateDirectory(CuratedDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(CuratedDirectory, entry.Id + ".md"),
            $"""
            ---
            schema: 1
            id: {entry.Id}
            type: fact
            tags: []
            created_at: 2026-01-01T00:00:00Z
            updated_at: 2026-01-01T00:00:00Z
            created_by: test-agent
            ---
            Duplicated as curated too.
            """,
            TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "doctor", "--scope", "project");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Same-scope collection duplicate ids:", result.StdOut);
        Assert.Contains($"'{entry.Id}' exists as both curated and journal in project", result.StdOut);
    }

    [Fact]
    public async Task JsonOutput_SameScopeCollectionDuplicateId_ReportedInStructuredOutput()
    {
        // arrange
        await InitWorkspaceAsync();
        var entry = await SeedJournalEntryAsync("Project journal entry.");
        Directory.CreateDirectory(CuratedDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(CuratedDirectory, entry.Id + ".md"),
            $"""
            ---
            schema: 1
            id: {entry.Id}
            type: fact
            tags: []
            created_at: 2026-01-01T00:00:00Z
            updated_at: 2026-01-01T00:00:00Z
            created_by: test-agent
            ---
            Duplicated as curated too.
            """,
            TestContext.Current.CancellationToken);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "doctor", "--scope", "project");

        // assert
        Assert.Equal(1, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.False(root.GetProperty("healthy").GetBoolean());
        var duplicates = root.GetProperty("sameScopeCollectionDuplicates").EnumerateArray().ToArray();
        var conflict = Assert.Single(duplicates);
        Assert.Equal(entry.Id, conflict.GetProperty("id").GetString());
        Assert.Equal("project", conflict.GetProperty("scope").GetString());
        Assert.Equal(2, conflict.GetProperty("paths").GetArrayLength());
    }

    [Fact]
    public async Task IndexDrift_FreshIndexDisagreesWithFiles_ReportedAndReturnsError()
    {
        // A "fresh" index (its stored fingerprint matches the curated
        // directory's current fingerprint) that disagrees with the files on
        // disk is the one drift self-healing reads can never catch, since
        // they trust a matching fingerprint without re-checking row content.

        // arrange
        await InitWorkspaceAsync();
        var record = await SeedMemoryAsync("Deploy checklist.");
        await ExecuteCommandAsync("agent", "memory", "search", "deploy");
        Assert.True(File.Exists(IndexPath));

        await using (var connection = new SqliteConnection($"Data Source={IndexPath};Pooling=False"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);

            await using (var delete = connection.CreateCommand())
            {
                delete.CommandText = "DELETE FROM curated WHERE id = @id";
                delete.Parameters.AddWithValue("@id", record.Id);
                await delete.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            }

            await using var insert = connection.CreateCommand();
            insert.CommandText = """
                INSERT INTO curated (id, type, created_at, updated_at, created_by, promoted_from, path)
                VALUES ('ghost-id', 'fact', '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z', 'test-agent', NULL, 'ghost');
                """;
            await insert.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "doctor", "--scope", "project");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Index: fresh", result.StdOut);
        Assert.Contains("FAIL Index agreement:", result.StdOut);
        Assert.Contains($"'{record.Id}' has a curated file on disk but is missing from the index", result.StdOut);
        Assert.Contains("'ghost-id' is in the index but has no curated file on disk", result.StdOut);
    }

    [Fact]
    public async Task MalformedCuratedFileInFreshIndex_NotReportedAsIndexOnly()
    {
        // A curated file whose frontmatter is corrupted after the index was
        // built, without changing its filesystem fingerprint (same mtime and
        // byte length), fools the index into staying "fresh" while still
        // holding a row for that id. The Frontmatter check already reports
        // the file; Index agreement must not report the same id a second
        // time as "in the index but has no curated file on disk".

        // arrange
        await InitWorkspaceAsync();
        var record = await SeedMemoryAsync("Deploy checklist.");
        await ExecuteCommandAsync("agent", "memory", "search", "deploy");
        Assert.True(File.Exists(IndexPath));

        var originalBytes = await File.ReadAllBytesAsync(record.Path, TestContext.Current.CancellationToken);
        var originalWriteTimeUtc = File.GetLastWriteTimeUtc(record.Path);
        var malformedBytes = System.Text.Encoding.UTF8.GetBytes(
            "not frontmatter at all".PadRight(originalBytes.Length));

        await File.WriteAllBytesAsync(record.Path, malformedBytes, TestContext.Current.CancellationToken);
        File.SetLastWriteTimeUtc(record.Path, originalWriteTimeUtc);

        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "doctor", "--scope", "project");

        // assert
        Assert.Equal(1, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var scope = document.RootElement.GetProperty("scopes").EnumerateArray().Single();

        Assert.Equal("fresh", scope.GetProperty("indexStatus").GetString());
        Assert.Empty(scope.GetProperty("indexOnlyIds").EnumerateArray());
        Assert.Contains(
            scope.GetProperty("malformedFrontmatter").EnumerateArray(),
            failure => failure.GetProperty("path").GetString() == record.Path);
    }

    [Fact]
    public async Task StaleIndex_NotReportedAsUnhealthy()
    {
        // A stale index self-heals on the next real read (every search
        // rebuilds when the fingerprint no longer matches), so doctor
        // reports it informationally rather than failing.

        // arrange
        await InitWorkspaceAsync();
        var record = await SeedMemoryAsync("Deploy checklist.");
        await ExecuteCommandAsync("agent", "memory", "search", "deploy");
        Assert.True(File.Exists(IndexPath));

        await ExecuteCommandAsync(
            "agent", "memory", "update", record.Id, "--text", "A much longer replacement checklist entry.");

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "doctor", "--scope", "project");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Index: stale", result.StdOut);
        Assert.DoesNotContain("FAIL", result.StdOut);
    }

    [Fact]
    public async Task MissingIndex_NotReportedAsUnhealthy()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Never searched.");
        Assert.False(File.Exists(IndexPath));

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "doctor", "--scope", "project");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Index: missing", result.StdOut);
        Assert.DoesNotContain("FAIL", result.StdOut);
    }

    [Fact]
    public async Task NoWorkspace_DefaultScope_OnlyChecksGlobal()
    {
        // arrange
        await SeedMemoryAsync("Global note.", scope: "global");

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Scope: global", result.StdOut);
        Assert.DoesNotContain("Scope: project", result.StdOut);
    }

    [Fact]
    public async Task ExplicitProjectScope_Throws_WhenNoWorkspaceExists()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "memory", "doctor", "--scope", "project");

        // assert
        result.AssertError("No agent workspace found. Run `nitro agent init` first.");
    }
}
