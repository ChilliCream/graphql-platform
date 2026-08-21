using ChilliCream.Nitro.CommandLine.Commands.Memory.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Commands.Memory;

internal sealed class DoctorMemoryCommand : Command
{
    public DoctorMemoryCommand() : base("doctor")
    {
        Description = "Check the memory store for integrity problems.";

        Options.Add(Opt<MemoryReadScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent memory doctor", "agent memory doctor --scope global");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMemoryStore>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var scope = parseResult.GetRequiredValue(Opt<MemoryReadScopeOption>.Instance);
        var inspections = new List<ScopeInspection>();

        if (scope is MemoryScopes.Project or MemoryScopes.All)
        {
            var workspaceDirectory = store.FindProjectWorkspaceDirectory();

            if (workspaceDirectory is null)
            {
                if (scope == MemoryScopes.Project)
                {
                    throw new ExitException("No agent workspace found. Run `nitro agent init` first.");
                }
            }
            else
            {
                var memoryDirectory = AgentWorkspace.GetMemoryDirectory(workspaceDirectory);

                inspections.Add(await InspectScopeAsync(
                    fileSystem, MemoryScopes.Project, memoryDirectory, cancellationToken));
            }
        }

        if (scope is MemoryScopes.Global or MemoryScopes.All)
        {
            inspections.Add(await InspectScopeAsync(
                fileSystem, MemoryScopes.Global, store.GlobalMemoryDirectory, cancellationToken));
        }

        var crossScopeDuplicates = FindCrossScopeDuplicates(inspections);
        var sameScopeCollectionDuplicates = FindSameScopeCollectionDuplicates(inspections);

        var healthy = crossScopeDuplicates.Count == 0
            && sameScopeCollectionDuplicates.Count == 0
            && inspections.All(inspection =>
                inspection.MalformedFrontmatter.Count == 0
                && inspection.IndexOnlyIds.Count == 0
                && inspection.FilesMissingFromIndexIds.Count == 0);

        var scopeResults = inspections.Select(ToResult).ToArray();

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(
                new MemoryDoctorResult(
                    scopeResults, crossScopeDuplicates, sameScopeCollectionDuplicates, healthy)));

            return healthy ? ExitCodes.Success : ExitCodes.Error;
        }

        foreach (var result in scopeResults)
        {
            console.WriteLine($"Scope: {result.Scope}");
            console.WriteLine($"  Curated: {result.CuratedCount} at {result.CuratedDirectory}");
            console.WriteLine($"  Journal: {result.JournalCount} at {result.JournalDirectory}");
            console.WriteLine($"  Index: {result.IndexStatus} ({result.IndexPath})");

            WriteCheck(
                console,
                "Frontmatter",
                result.MalformedFrontmatter.Count == 0,
                result.MalformedFrontmatter.Select(f => $"{f.Kind} {f.Path}: {f.Reason}"));

            WriteCheck(
                console,
                "Index agreement",
                result.IndexOnlyIds.Count == 0 && result.FilesMissingFromIndexIds.Count == 0,
                result.IndexOnlyIds.Select(id => $"'{id}' is in the index but has no curated file on disk")
                    .Concat(result.FilesMissingFromIndexIds.Select(
                        id => $"'{id}' has a curated file on disk but is missing from the index")));

            console.WriteLine();
        }

        WriteCheck(
            console,
            "Cross-scope duplicate ids",
            crossScopeDuplicates.Count == 0,
            crossScopeDuplicates.Select(conflict =>
                $"'{conflict.Id}' exists in {string.Join(" and ", conflict.Scopes)}: "
                + string.Join(", ", conflict.Paths)));

        WriteCheck(
            console,
            "Same-scope collection duplicate ids",
            sameScopeCollectionDuplicates.Count == 0,
            sameScopeCollectionDuplicates.Select(conflict =>
                $"'{conflict.Id}' exists as both curated and journal in {conflict.Scope}: "
                + string.Join(", ", conflict.Paths)));

        return healthy ? ExitCodes.Success : ExitCodes.Error;
    }

    private static MemoryDoctorScopeResult ToResult(ScopeInspection inspection) => new(
        inspection.Scope,
        inspection.CuratedDirectory,
        inspection.JournalDirectory,
        inspection.IndexPath,
        inspection.CuratedPathsById.Count,
        inspection.JournalPathsById.Count,
        inspection.IndexStatus,
        inspection.IndexOnlyIds,
        inspection.FilesMissingFromIndexIds,
        inspection.MalformedFrontmatter);

    /// <summary>
    /// Reads every curated and journal file directly, rather than through
    /// <see cref="IMemoryStore"/>, because the store's normal listing
    /// methods throw on the first malformed file; doctor instead collects
    /// every offending path and reason.
    /// </summary>
    private static async Task<ScopeInspection> InspectScopeAsync(
        IFileSystem fileSystem, string scope, string memoryDirectory, CancellationToken cancellationToken)
    {
        var curatedDirectory = AgentWorkspace.GetMemoryCuratedDirectory(memoryDirectory);
        var journalDirectory = AgentWorkspace.GetMemoryJournalDirectory(memoryDirectory);
        var localDirectory = AgentWorkspace.GetMemoryLocalDirectory(memoryDirectory);
        var indexPath = AgentWorkspace.GetMemoryIndexDatabasePath(localDirectory);

        var malformed = new List<MemoryDoctorFrontmatterFailure>();
        var curatedPathsById = new Dictionary<string, string>(StringComparer.Ordinal);
        var journalPathsById = new Dictionary<string, string>(StringComparer.Ordinal);
        var malformedCuratedIds = new HashSet<string>(StringComparer.Ordinal);

        if (fileSystem.DirectoryExists(curatedDirectory))
        {
            foreach (var path in fileSystem.GetFiles(curatedDirectory, "*.md", SearchOption.TopDirectoryOnly))
            {
                var id = Path.GetFileNameWithoutExtension(path);
                var content = await fileSystem.ReadAllTextAsync(path, cancellationToken);

                if (MemoryFrontmatterParser.TryParse(content, id, out _, out var failure))
                {
                    curatedPathsById[id] = path;
                }
                else
                {
                    malformed.Add(new MemoryDoctorFrontmatterFailure("curated", path, failure.Message));
                    malformedCuratedIds.Add(id);
                }
            }
        }

        if (fileSystem.DirectoryExists(journalDirectory))
        {
            foreach (var path in fileSystem.GetFiles(journalDirectory, "*.md", SearchOption.AllDirectories))
            {
                var id = Path.GetFileNameWithoutExtension(path);
                var content = await fileSystem.ReadAllTextAsync(path, cancellationToken);

                if (MemoryJournalFrontmatterParser.TryParse(content, id, out _, out var failure))
                {
                    journalPathsById[id] = path;
                }
                else
                {
                    malformed.Add(new MemoryDoctorFrontmatterFailure("journal", path, failure.Message));
                }
            }
        }

        var (indexStatus, indexOnlyIds, filesMissingFromIndexIds) = await InspectIndexAsync(
            fileSystem, curatedDirectory, indexPath, curatedPathsById.Keys, malformedCuratedIds, cancellationToken);

        return new ScopeInspection(
            scope,
            curatedDirectory,
            journalDirectory,
            indexPath,
            curatedPathsById,
            journalPathsById,
            indexStatus,
            indexOnlyIds,
            filesMissingFromIndexIds,
            malformed);
    }

    /// <summary>
    /// Compares the search index's own fingerprint against the curated
    /// directory's current fingerprint the same way a normal read does. A
    /// mismatch ("stale") or a missing or corrupt index is not reported as
    /// an id-agreement problem: normal reads self-heal it automatically.
    /// Only when the index claims to be fresh, and reads would therefore
    /// trust it without rebuilding, is its row set compared against the
    /// files actually on disk. Ids from curated files with malformed
    /// frontmatter are excluded from <c>indexOnlyIds</c>: they are already
    /// reported by the Frontmatter check, and a stale index row for a file
    /// that fails to parse is not a separate index-agreement problem.
    /// </summary>
    private static async Task<(
        string Status, IReadOnlyList<string> IndexOnlyIds, IReadOnlyList<string> FilesMissingFromIndexIds)>
        InspectIndexAsync(
            IFileSystem fileSystem,
            string curatedDirectory,
            string indexPath,
            IEnumerable<string> curatedIds,
            IReadOnlySet<string> malformedCuratedIds,
            CancellationToken cancellationToken)
    {
        if (!fileSystem.FileExists(indexPath))
        {
            return ("missing", [], []);
        }

        SqliteConnection? connection = null;

        try
        {
            connection = new SqliteConnection($"Data Source={indexPath};Pooling=False");
            await connection.OpenAsync(cancellationToken);

            var quickCheck = await connection.QueryFirstOrDefaultAsync<string>("PRAGMA quick_check;");

            if (quickCheck != "ok")
            {
                return ("corrupt", [], []);
            }

            var storedFingerprint = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT value FROM meta WHERE key = @key;",
                new { key = MemoryIndexSchema.FingerprintKey });

            var currentFingerprint = MemoryIndexFingerprint.Compute(fileSystem, curatedDirectory);

            if (storedFingerprint != currentFingerprint)
            {
                return ("stale", [], []);
            }

            var indexedIds = new HashSet<string>(
                await connection.QueryAsync<string>("SELECT id FROM curated;"), StringComparer.Ordinal);
            var diskIds = new HashSet<string>(curatedIds, StringComparer.Ordinal);

            var indexOnly = indexedIds.Except(diskIds).Except(malformedCuratedIds)
                .OrderBy(id => id, StringComparer.Ordinal).ToArray();
            var filesMissing = diskIds.Except(indexedIds).OrderBy(id => id, StringComparer.Ordinal).ToArray();

            return ("fresh", indexOnly, filesMissing);
        }
        catch (SqliteException)
        {
            return ("corrupt", [], []);
        }
        finally
        {
            if (connection is not null)
            {
                await connection.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Finds ids present in both a project and a global inspection, across
    /// curated and journal ids combined: valid ids are collision-resistant
    /// by construction, so the same id in two scopes is invalid data,
    /// mirroring <c>MemoryStore</c>'s cross-scope duplicate check but
    /// reporting rather than throwing.
    /// </summary>
    private static IReadOnlyList<MemoryScopeConflict> FindCrossScopeDuplicates(
        IReadOnlyList<ScopeInspection> inspections)
    {
        if (inspections.Count < 2)
        {
            return [];
        }

        var entries = inspections
            .SelectMany(inspection => inspection.CuratedPathsById
                .Concat(inspection.JournalPathsById)
                .Select(pair => (Id: pair.Key, inspection.Scope, Path: pair.Value)));

        return entries
            .GroupBy(entry => entry.Id, StringComparer.Ordinal)
            .Where(group => group.Select(entry => entry.Scope).Distinct().Count() > 1)
            .Select(group => new MemoryScopeConflict(
                group.Key,
                group.Select(entry => entry.Scope).Distinct().ToArray(),
                group.Select(entry => entry.Path).ToArray()))
            .ToArray();
    }

    /// <summary>
    /// Finds ids present in both the curated and journal collections of the
    /// SAME scope. Ids are collision-resistant by construction, so an id
    /// duplicated between a scope's curated/ and journal/ directories is
    /// invalid data too, distinct from the cross-scope check above, which
    /// only fires when both a project and a global inspection exist.
    /// </summary>
    private static IReadOnlyList<MemoryCollectionConflict> FindSameScopeCollectionDuplicates(
        IReadOnlyList<ScopeInspection> inspections)
    {
        var conflicts = new List<MemoryCollectionConflict>();

        foreach (var inspection in inspections)
        {
            var duplicateIds = inspection.CuratedPathsById.Keys
                .Intersect(inspection.JournalPathsById.Keys, StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal);

            foreach (var id in duplicateIds)
            {
                conflicts.Add(new MemoryCollectionConflict(
                    id,
                    inspection.Scope,
                    [inspection.CuratedPathsById[id], inspection.JournalPathsById[id]]));
            }
        }

        return conflicts;
    }

    private static void WriteCheck(
        INitroConsole console,
        string name,
        bool ok,
        IEnumerable<string> problems)
    {
        if (ok)
        {
            console.OkLine(name);
            return;
        }

        console.WriteLine($"FAIL {name}:");

        foreach (var problem in problems)
        {
            console.WriteLine($"  {problem}");
        }
    }

    private sealed record ScopeInspection(
        string Scope,
        string CuratedDirectory,
        string JournalDirectory,
        string IndexPath,
        IReadOnlyDictionary<string, string> CuratedPathsById,
        IReadOnlyDictionary<string, string> JournalPathsById,
        string IndexStatus,
        IReadOnlyList<string> IndexOnlyIds,
        IReadOnlyList<string> FilesMissingFromIndexIds,
        IReadOnlyList<MemoryDoctorFrontmatterFailure> MalformedFrontmatter);

    public sealed record MemoryDoctorResult(
        IReadOnlyList<MemoryDoctorScopeResult> Scopes,
        IReadOnlyList<MemoryScopeConflict> CrossScopeDuplicates,
        IReadOnlyList<MemoryCollectionConflict> SameScopeCollectionDuplicates,
        bool Healthy);

    public sealed record MemoryDoctorScopeResult(
        string Scope,
        string CuratedDirectory,
        string JournalDirectory,
        string IndexPath,
        int CuratedCount,
        int JournalCount,
        string IndexStatus,
        IReadOnlyList<string> IndexOnlyIds,
        IReadOnlyList<string> FilesMissingFromIndexIds,
        IReadOnlyList<MemoryDoctorFrontmatterFailure> MalformedFrontmatter);

    public sealed record MemoryDoctorFrontmatterFailure(string Kind, string Path, string Reason);

    /// <summary>
    /// One id that exists in both the curated and journal collections of the
    /// same scope. Distinct from <see cref="MemoryScopeConflict"/>, which
    /// covers an id duplicated across scopes rather than across collections
    /// within one scope.
    /// </summary>
    public sealed record MemoryCollectionConflict(string Id, string Scope, IReadOnlyList<string> Paths);
}
