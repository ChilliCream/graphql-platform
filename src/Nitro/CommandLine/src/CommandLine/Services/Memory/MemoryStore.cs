using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The project and global memory stores: directory provisioning and
/// discovery, the curated vertical (save, update, forget, show, recent),
/// and the journal vertical (log, promote), reading and writing markdown
/// files directly, with union-merged, no-shadowing reads across scopes.
/// </summary>
internal sealed class MemoryStore(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    string globalMemoryDirectory) : IMemoryStore
{
    // Matches the threshold AtomicFileSystemTests exercises: a temp file
    // this old was abandoned by a crashed or cancelled write, not one still
    // in flight.
    private static readonly TimeSpan AbandonedTempFileAge = TimeSpan.FromHours(1);

    public string GlobalMemoryDirectory => globalMemoryDirectory;

    public string? FindProjectWorkspaceDirectory()
        => AgentWorkspace.FindMemory(fileSystem, fileSystem.GetCurrentDirectory());

    public Task EnsureProjectWorkspaceAsync(string workspaceDirectory, CancellationToken cancellationToken)
    {
        var memoryDirectory = AgentWorkspace.GetMemoryDirectory(workspaceDirectory);

        CreateIfMissing(AgentWorkspace.GetMemoryCuratedDirectory(memoryDirectory));
        CreateIfMissing(AgentWorkspace.GetMemoryJournalDirectory(memoryDirectory));
        CreateIfMissing(AgentWorkspace.GetMemoryLocalDirectory(memoryDirectory));

        return Task.CompletedTask;
    }

    public async Task<MemoryRecord> SaveAsync(
        MemoryRecordCreation creation, CancellationToken cancellationToken)
    {
        var scope = ValidateWriteScope(creation.Scope);
        var type = ValidateType(creation.Type);
        var tags = NormalizeTags(creation.Tags);
        var actor = ValidateActor(creation.Actor);

        var curatedDirectory = scope == MemoryScopes.Global
            ? EnsureGlobalStore()
            : await EnsureProjectStoreAsync(cancellationToken);

        fileSystem.CleanupAbandonedTempFiles(curatedDirectory, AbandonedTempFileAge);

        var now = timeProvider.GetUtcNow();
        var id = MemoryId.New(timeProvider);
        var path = GetCuratedPath(curatedDirectory, id);

        var frontmatter = new MemoryFrontmatter(
            MemoryFrontmatterParser.SupportedSchemaVersion,
            id,
            type,
            tags,
            now,
            now,
            actor,
            PromotedFrom: null,
            creation.Text);

        await fileSystem.CreateFileAtomicAsync(
            path, MemoryFrontmatterWriter.Write(frontmatter), cancellationToken);

        return ToRecord(frontmatter, path, scope);
    }

    public async Task<MemoryRecord> UpdateAsync(
        string id, string scope, MemoryRecordUpdate update, CancellationToken cancellationToken)
    {
        var normalizedScope = ValidateWriteScope(scope);
        var record = await GetRequiredAsync(id, normalizedScope, cancellationToken);

        var text = update.TextGiven ? update.Text ?? "" : record.Body;
        var type = update.TypeGiven ? ValidateType(update.Type ?? "") : record.Type;
        var tags = ApplyTagChanges(record.Tags, update.AddTags, update.RemoveTags);

        var curatedDirectory = GetCuratedDirectoryForScope(normalizedScope);
        fileSystem.CleanupAbandonedTempFiles(curatedDirectory, AbandonedTempFileAge);

        var frontmatter = new MemoryFrontmatter(
            MemoryFrontmatterParser.SupportedSchemaVersion,
            record.Id,
            type,
            tags,
            record.CreatedAt,
            timeProvider.GetUtcNow(),
            record.CreatedBy,
            record.PromotedFrom,
            text);

        await fileSystem.ReplaceFileAtomicAsync(
            record.Path, MemoryFrontmatterWriter.Write(frontmatter), cancellationToken);

        return ToRecord(frontmatter, record.Path, normalizedScope);
    }

    public async Task<MemoryRecord> ForgetAsync(string id, string scope, CancellationToken cancellationToken)
    {
        var normalizedScope = ValidateWriteScope(scope);
        var record = await GetRequiredAsync(id, normalizedScope, cancellationToken);

        fileSystem.DeleteFile(record.Path);

        return record;
    }

    public async Task<MemoryRecord?> FindAsync(string id, string scope, CancellationToken cancellationToken)
    {
        var normalizedScope = ValidateReadScope(scope);

        switch (normalizedScope)
        {
            case MemoryScopes.Project:
                return await FindInDirectoryAsync(
                    GetCuratedDirectory(RequireProjectWorkspaceDirectory()),
                    id,
                    MemoryScopes.Project,
                    cancellationToken);

            case MemoryScopes.Global:
                return await FindInDirectoryAsync(GetGlobalCuratedDirectory(), id, MemoryScopes.Global, cancellationToken);

            default:
                var projectWorkspaceDirectory = FindProjectWorkspaceDirectory();

                var projectRecord = projectWorkspaceDirectory is null
                    ? null
                    : await FindInDirectoryAsync(
                        GetCuratedDirectory(projectWorkspaceDirectory), id, MemoryScopes.Project, cancellationToken);

                var globalRecord = await FindInDirectoryAsync(
                    GetGlobalCuratedDirectory(), id, MemoryScopes.Global, cancellationToken);

                if (projectRecord is not null && globalRecord is not null)
                {
                    throw new MemoryScopeConflictException([
                        new MemoryScopeConflict(
                            id,
                            [MemoryScopes.Project, MemoryScopes.Global],
                            [projectRecord.Path, globalRecord.Path])
                    ]);
                }

                return projectRecord ?? globalRecord;
        }
    }

    public async Task<MemoryRecord> GetRequiredAsync(string id, string scope, CancellationToken cancellationToken)
        => await FindAsync(id, scope, cancellationToken)
            ?? throw new ExitException($"Memory '{id}' does not exist.");

    public async Task<IReadOnlyList<MemoryRecord>> GetRecentCuratedAsync(
        string scope, int? limit, CancellationToken cancellationToken)
    {
        var normalizedScope = ValidateReadScope(scope);
        var records = new List<MemoryRecord>();

        if (normalizedScope is MemoryScopes.Project or MemoryScopes.All)
        {
            var projectWorkspaceDirectory = FindProjectWorkspaceDirectory();

            if (projectWorkspaceDirectory is not null)
            {
                records.AddRange(await ListCuratedAsync(
                    GetCuratedDirectory(projectWorkspaceDirectory), MemoryScopes.Project, cancellationToken));
            }
        }

        if (normalizedScope is MemoryScopes.Global or MemoryScopes.All)
        {
            records.AddRange(
                await ListCuratedAsync(GetGlobalCuratedDirectory(), MemoryScopes.Global, cancellationToken));
        }

        if (normalizedScope == MemoryScopes.All)
        {
            ThrowIfCrossScopeDuplicates(records, r => r.Id, r => r.Scope, r => r.Path);
        }

        return limit is { } value ? records.Take(value).ToList() : records;
    }

    public async Task<IReadOnlyList<MemoryRecord>> SearchCuratedAsync(
        string query,
        string scope,
        IReadOnlyList<string> tags,
        string? type,
        DateTimeOffset? since,
        int? limit,
        CancellationToken cancellationToken)
    {
        var normalizedScope = ValidateReadScope(scope);
        var matchQuery = MemoryFtsQuery.BuildLiteralMatch(query);
        var normalizedTags = tags.Select(MemoryTags.Normalize).ToList();
        var normalizedType = type is null ? null : MemoryTypes.Normalize(type);

        var records = new List<MemoryRecord>();

        if (normalizedScope is MemoryScopes.Project or MemoryScopes.All)
        {
            var projectWorkspaceDirectory = FindProjectWorkspaceDirectory();

            if (projectWorkspaceDirectory is not null)
            {
                records.AddRange(await SearchScopeAsync(
                    GetCuratedDirectory(projectWorkspaceDirectory),
                    GetLocalDirectory(projectWorkspaceDirectory),
                    MemoryScopes.Project,
                    matchQuery,
                    normalizedType,
                    normalizedTags,
                    since,
                    cancellationToken));
            }
        }

        if (normalizedScope is MemoryScopes.Global or MemoryScopes.All)
        {
            records.AddRange(await SearchScopeAsync(
                GetGlobalCuratedDirectory(),
                GetGlobalLocalDirectory(),
                MemoryScopes.Global,
                matchQuery,
                normalizedType,
                normalizedTags,
                since,
                cancellationToken));
        }

        if (normalizedScope == MemoryScopes.All)
        {
            ThrowIfCrossScopeDuplicates(records, r => r.Id, r => r.Scope, r => r.Path);
        }

        return limit is { } value ? records.Take(value).ToList() : records;
    }

    public async Task<MemoryJournalEntry> LogAsync(
        MemoryJournalEntryCreation creation, CancellationToken cancellationToken)
    {
        var scope = ValidateWriteScope(creation.Scope);
        var actor = ValidateActor(creation.Actor);

        string journalDirectory;

        if (scope == MemoryScopes.Global)
        {
            EnsureGlobalStore();
            journalDirectory = GetGlobalJournalDirectory();
        }
        else
        {
            var workspaceDirectory = RequireProjectWorkspaceDirectory();
            await EnsureProjectWorkspaceAsync(workspaceDirectory, cancellationToken);
            journalDirectory = GetJournalDirectory(workspaceDirectory);
        }

        var now = timeProvider.GetUtcNow();
        var dateDirectory = AgentWorkspace.GetMemoryJournalDateDirectory(
            journalDirectory, DateOnly.FromDateTime(now.UtcDateTime));

        CreateIfMissing(dateDirectory);
        fileSystem.CleanupAbandonedTempFiles(dateDirectory, AbandonedTempFileAge);

        var id = MemoryId.New(timeProvider);
        var path = Path.Combine(dateDirectory, id + ".md");

        var frontmatter = new MemoryJournalFrontmatter(
            MemoryJournalFrontmatterParser.SupportedSchemaVersion, id, now, actor, creation.Text);

        await fileSystem.CreateFileAtomicAsync(
            path, MemoryJournalFrontmatterWriter.Write(frontmatter), cancellationToken);

        return ToJournalRecord(frontmatter, path, scope);
    }

    public async Task<MemoryJournalEntry?> FindJournalEntryAsync(
        string id, string scope, CancellationToken cancellationToken)
    {
        var normalizedScope = ValidateReadScope(scope);

        switch (normalizedScope)
        {
            case MemoryScopes.Project:
                return await FindJournalEntryInDirectoryAsync(
                    GetJournalDirectory(RequireProjectWorkspaceDirectory()),
                    id,
                    MemoryScopes.Project,
                    cancellationToken);

            case MemoryScopes.Global:
                return await FindJournalEntryInDirectoryAsync(
                    GetGlobalJournalDirectory(), id, MemoryScopes.Global, cancellationToken);

            default:
                var projectWorkspaceDirectory = FindProjectWorkspaceDirectory();

                var projectEntry = projectWorkspaceDirectory is null
                    ? null
                    : await FindJournalEntryInDirectoryAsync(
                        GetJournalDirectory(projectWorkspaceDirectory), id, MemoryScopes.Project, cancellationToken);

                var globalEntry = await FindJournalEntryInDirectoryAsync(
                    GetGlobalJournalDirectory(), id, MemoryScopes.Global, cancellationToken);

                if (projectEntry is not null && globalEntry is not null)
                {
                    throw new MemoryScopeConflictException([
                        new MemoryScopeConflict(
                            id,
                            [MemoryScopes.Project, MemoryScopes.Global],
                            [projectEntry.Path, globalEntry.Path])
                    ]);
                }

                return projectEntry ?? globalEntry;
        }
    }

    public async Task<MemoryJournalEntry> GetRequiredJournalEntryAsync(
        string id, string scope, CancellationToken cancellationToken)
        => await FindJournalEntryAsync(id, scope, cancellationToken)
            ?? throw new ExitException($"Journal entry '{id}' does not exist.");

    public async Task<IReadOnlyList<MemoryJournalEntry>> GetRecentJournalAsync(
        string scope, int? limit, CancellationToken cancellationToken)
    {
        var normalizedScope = ValidateReadScope(scope);
        var entries = new List<MemoryJournalEntry>();

        if (normalizedScope is MemoryScopes.Project or MemoryScopes.All)
        {
            var projectWorkspaceDirectory = FindProjectWorkspaceDirectory();

            if (projectWorkspaceDirectory is not null)
            {
                entries.AddRange(await ListJournalAsync(
                    GetJournalDirectory(projectWorkspaceDirectory), MemoryScopes.Project, cancellationToken));
            }
        }

        if (normalizedScope is MemoryScopes.Global or MemoryScopes.All)
        {
            entries.AddRange(
                await ListJournalAsync(GetGlobalJournalDirectory(), MemoryScopes.Global, cancellationToken));
        }

        if (normalizedScope == MemoryScopes.All)
        {
            ThrowIfCrossScopeDuplicates(entries, e => e.Id, e => e.Scope, e => e.Path);
        }

        return limit is { } value ? entries.Take(value).ToList() : entries;
    }

    public async Task<IReadOnlyList<MemoryJournalEntry>> SearchJournalAsync(
        string query, string scope, DateTimeOffset? since, int? limit, CancellationToken cancellationToken)
    {
        var normalizedScope = ValidateReadScope(scope);
        var words = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var entries = new List<MemoryJournalEntry>();

        if (normalizedScope is MemoryScopes.Project or MemoryScopes.All)
        {
            var projectWorkspaceDirectory = FindProjectWorkspaceDirectory();

            if (projectWorkspaceDirectory is not null)
            {
                entries.AddRange(await SearchJournalScopeAsync(
                    GetJournalDirectory(projectWorkspaceDirectory),
                    MemoryScopes.Project,
                    words,
                    since,
                    cancellationToken));
            }
        }

        if (normalizedScope is MemoryScopes.Global or MemoryScopes.All)
        {
            entries.AddRange(await SearchJournalScopeAsync(
                GetGlobalJournalDirectory(), MemoryScopes.Global, words, since, cancellationToken));
        }

        if (normalizedScope == MemoryScopes.All)
        {
            ThrowIfCrossScopeDuplicates(entries, e => e.Id, e => e.Scope, e => e.Path);
        }

        return limit is { } value ? entries.Take(value).ToList() : entries;
    }

    public async Task<IReadOnlyList<MemoryJournalEntry>> GetUnpromotedJournalEntriesAsync(
        string scope, CancellationToken cancellationToken)
    {
        var normalizedScope = ValidateReadScope(scope);
        var entries = new List<MemoryJournalEntry>();

        if (normalizedScope is MemoryScopes.Project or MemoryScopes.All)
        {
            var projectWorkspaceDirectory = FindProjectWorkspaceDirectory();

            if (projectWorkspaceDirectory is not null)
            {
                entries.AddRange(await GetUnpromotedInScopeAsync(
                    GetJournalDirectory(projectWorkspaceDirectory),
                    GetCuratedDirectory(projectWorkspaceDirectory),
                    MemoryScopes.Project,
                    cancellationToken));
            }
        }

        if (normalizedScope is MemoryScopes.Global or MemoryScopes.All)
        {
            entries.AddRange(await GetUnpromotedInScopeAsync(
                GetGlobalJournalDirectory(), GetGlobalCuratedDirectory(), MemoryScopes.Global, cancellationToken));
        }

        if (normalizedScope == MemoryScopes.All)
        {
            ThrowIfCrossScopeDuplicates(entries, e => e.Id, e => e.Scope, e => e.Path);
        }

        return entries;
    }

    public async Task<MemoryPromotionOutcome> PromoteAsync(
        string journalId,
        string scope,
        string type,
        IReadOnlyList<string> tags,
        CancellationToken cancellationToken)
    {
        var normalizedType = ValidateType(type);
        var normalizedTags = NormalizeTags(tags);

        var entry = await GetRequiredJournalEntryAsync(journalId, scope, cancellationToken);
        var resolvedScope = entry.Scope;

        var curatedDirectory = GetCuratedDirectoryForScope(resolvedScope);
        var curatedId = MemoryPromotedId.Derive(resolvedScope, entry.Id);
        var curatedPath = GetCuratedPath(curatedDirectory, curatedId);

        if (fileSystem.FileExists(curatedPath))
        {
            return new MemoryPromotionOutcome(
                await ReadExistingPromotionAsync(curatedPath, curatedId, resolvedScope, cancellationToken),
                AlreadyPromoted: true);
        }

        CreateIfMissing(curatedDirectory);
        fileSystem.CleanupAbandonedTempFiles(curatedDirectory, AbandonedTempFileAge);

        var now = timeProvider.GetUtcNow();

        var frontmatter = new MemoryFrontmatter(
            MemoryFrontmatterParser.SupportedSchemaVersion,
            curatedId,
            normalizedType,
            normalizedTags,
            now,
            now,
            entry.CreatedBy,
            entry.Id,
            entry.Body);

        try
        {
            await fileSystem.CreateFileAtomicAsync(
                curatedPath, MemoryFrontmatterWriter.Write(frontmatter), cancellationToken);
        }
        catch (IOException) when (fileSystem.FileExists(curatedPath))
        {
            // Lost a create race to a concurrent or retried promote of the
            // same journal entry: the winner's file is authoritative.
            return new MemoryPromotionOutcome(
                await ReadExistingPromotionAsync(curatedPath, curatedId, resolvedScope, cancellationToken),
                AlreadyPromoted: true);
        }

        return new MemoryPromotionOutcome(ToRecord(frontmatter, curatedPath, resolvedScope), AlreadyPromoted: false);
    }

    private async Task<MemoryRecord> ReadExistingPromotionAsync(
        string curatedPath, string curatedId, string scope, CancellationToken cancellationToken)
    {
        var content = await fileSystem.ReadAllTextAsync(curatedPath, cancellationToken);

        if (!MemoryFrontmatterParser.TryParse(content, curatedId, out var frontmatter, out var failure))
        {
            throw new ExitException($"Memory '{curatedId}' has malformed frontmatter: {failure.Message}");
        }

        return ToRecord(frontmatter, curatedPath, scope);
    }

    private async Task<IReadOnlyList<MemoryJournalEntry>> GetUnpromotedInScopeAsync(
        string journalDirectory, string curatedDirectory, string scope, CancellationToken cancellationToken)
    {
        var entries = await ListJournalAsync(journalDirectory, scope, cancellationToken);
        var unpromoted = new List<MemoryJournalEntry>();

        foreach (var entry in entries)
        {
            var curatedId = MemoryPromotedId.Derive(scope, entry.Id);
            var curatedPath = GetCuratedPath(curatedDirectory, curatedId);

            if (!fileSystem.FileExists(curatedPath))
            {
                unpromoted.Add(entry);
            }
        }

        return unpromoted;
    }

    private async Task<IReadOnlyList<MemoryJournalEntry>> SearchJournalScopeAsync(
        string journalDirectory,
        string scope,
        IReadOnlyList<string> words,
        DateTimeOffset? since,
        CancellationToken cancellationToken)
    {
        var entries = await ListJournalAsync(journalDirectory, scope, cancellationToken);

        return entries
            .Where(entry => since is null || entry.CreatedAt >= since)
            .Where(entry => MatchesAllWords(entry.Body, words))
            .ToList();
    }

    private static bool MatchesAllWords(string body, IReadOnlyList<string> words)
    {
        foreach (var word in words)
        {
            if (body.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<MemoryJournalEntry?> FindJournalEntryInDirectoryAsync(
        string journalDirectory, string id, string scope, CancellationToken cancellationToken)
    {
        if (!fileSystem.DirectoryExists(journalDirectory))
        {
            return null;
        }

        var path = fileSystem.GetFiles(journalDirectory, id + ".md", SearchOption.AllDirectories).FirstOrDefault();

        if (path is null)
        {
            return null;
        }

        var content = await fileSystem.ReadAllTextAsync(path, cancellationToken);

        if (!MemoryJournalFrontmatterParser.TryParse(content, id, out var frontmatter, out var failure))
        {
            throw new ExitException($"Journal entry '{id}' has malformed frontmatter: {failure.Message}");
        }

        return ToJournalRecord(frontmatter, path, scope);
    }

    private async Task<IReadOnlyList<MemoryJournalEntry>> ListJournalAsync(
        string journalDirectory, string scope, CancellationToken cancellationToken)
    {
        if (!fileSystem.DirectoryExists(journalDirectory))
        {
            return [];
        }

        var entries = new List<MemoryJournalEntry>();

        foreach (var path in fileSystem.GetFiles(journalDirectory, "*.md", SearchOption.AllDirectories))
        {
            var id = Path.GetFileNameWithoutExtension(path);
            var content = await fileSystem.ReadAllTextAsync(path, cancellationToken);

            if (!MemoryJournalFrontmatterParser.TryParse(content, id, out var frontmatter, out var failure))
            {
                throw new ExitException($"Journal entry '{id}' has malformed frontmatter: {failure.Message}");
            }

            entries.Add(ToJournalRecord(frontmatter, path, scope));
        }

        return entries
            .OrderByDescending(entry => entry.CreatedAt)
            .ThenBy(entry => entry.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static MemoryJournalEntry ToJournalRecord(
        MemoryJournalFrontmatter frontmatter, string path, string scope) => new()
    {
        Id = frontmatter.Id,
        Scope = scope,
        Path = path,
        Body = frontmatter.Body,
        CreatedAt = frontmatter.CreatedAt,
        CreatedBy = frontmatter.CreatedBy
    };

    private string GetGlobalJournalDirectory()
        => AgentWorkspace.GetMemoryJournalDirectory(globalMemoryDirectory);

    private static string GetJournalDirectory(string workspaceDirectory)
        => AgentWorkspace.GetMemoryJournalDirectory(AgentWorkspace.GetMemoryDirectory(workspaceDirectory));

    public async Task<MemoryIndexRebuildResult> RebuildIndexAsync(string scope, CancellationToken cancellationToken)
    {
        var normalizedScope = ValidateWriteScope(scope);

        string curatedDirectory;
        string localDirectory;

        if (normalizedScope == MemoryScopes.Global)
        {
            curatedDirectory = EnsureGlobalStore();
            localDirectory = GetGlobalLocalDirectory();
        }
        else
        {
            curatedDirectory = await EnsureProjectStoreAsync(cancellationToken);
            localDirectory = GetLocalDirectory(RequireProjectWorkspaceDirectory());
        }

        var indexedCount = await MemoryFtsIndex.RebuildAsync(
            fileSystem, curatedDirectory, localDirectory, cancellationToken);
        var indexPath = AgentWorkspace.GetMemoryIndexDatabasePath(localDirectory);

        return new MemoryIndexRebuildResult(normalizedScope, indexedCount, indexPath);
    }

    private async Task<IReadOnlyList<MemoryRecord>> SearchScopeAsync(
        string curatedDirectory,
        string localDirectory,
        string scope,
        string matchQuery,
        string? type,
        IReadOnlyList<string> tags,
        DateTimeOffset? since,
        CancellationToken cancellationToken)
    {
        if (!fileSystem.DirectoryExists(curatedDirectory))
        {
            return [];
        }

        var ids = await MemoryFtsIndex.SearchAsync(
            fileSystem, curatedDirectory, localDirectory, matchQuery, type, tags, since, cancellationToken);

        var records = new List<MemoryRecord>();

        foreach (var id in ids)
        {
            var record = await FindInDirectoryAsync(curatedDirectory, id, scope, cancellationToken);

            if (record is not null)
            {
                records.Add(record);
            }
        }

        return records;
    }

    private async Task<MemoryRecord?> FindInDirectoryAsync(
        string curatedDirectory, string id, string scope, CancellationToken cancellationToken)
    {
        var path = GetCuratedPath(curatedDirectory, id);

        if (!fileSystem.FileExists(path))
        {
            return null;
        }

        var content = await fileSystem.ReadAllTextAsync(path, cancellationToken);

        if (!MemoryFrontmatterParser.TryParse(content, id, out var frontmatter, out var failure))
        {
            throw new ExitException($"Memory '{id}' has malformed frontmatter: {failure.Message}");
        }

        return ToRecord(frontmatter, path, scope);
    }

    private async Task<IReadOnlyList<MemoryRecord>> ListCuratedAsync(
        string curatedDirectory, string scope, CancellationToken cancellationToken)
    {
        if (!fileSystem.DirectoryExists(curatedDirectory))
        {
            return [];
        }

        var records = new List<MemoryRecord>();

        foreach (var path in fileSystem.GetFiles(curatedDirectory, "*.md", SearchOption.TopDirectoryOnly))
        {
            var id = Path.GetFileNameWithoutExtension(path);
            var content = await fileSystem.ReadAllTextAsync(path, cancellationToken);

            if (!MemoryFrontmatterParser.TryParse(content, id, out var frontmatter, out var failure))
            {
                throw new ExitException($"Memory '{id}' has malformed frontmatter: {failure.Message}");
            }

            records.Add(ToRecord(frontmatter, path, scope));
        }

        return records
            .OrderByDescending(record => record.UpdatedAt)
            .ThenBy(record => record.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static void ThrowIfCrossScopeDuplicates<T>(
        IReadOnlyList<T> records,
        Func<T, string> idSelector,
        Func<T, string> scopeSelector,
        Func<T, string> pathSelector)
    {
        var conflicts = records
            .GroupBy(idSelector, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => new MemoryScopeConflict(
                group.Key,
                group.Select(scopeSelector).ToArray(),
                group.Select(pathSelector).ToArray()))
            .ToList();

        if (conflicts.Count > 0)
        {
            throw new MemoryScopeConflictException(conflicts);
        }
    }

    private async Task<string> EnsureProjectStoreAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = RequireProjectWorkspaceDirectory();

        // Provisioning happens on `agent init`; this is the lazy-creation
        // fallback for a workspace that has an agent database but has never
        // written a curated memory before.
        await EnsureProjectWorkspaceAsync(workspaceDirectory, cancellationToken);

        return GetCuratedDirectory(workspaceDirectory);
    }

    private string EnsureGlobalStore()
    {
        CreateIfMissing(AgentWorkspace.GetMemoryCuratedDirectory(globalMemoryDirectory));
        CreateIfMissing(AgentWorkspace.GetMemoryJournalDirectory(globalMemoryDirectory));
        CreateIfMissing(AgentWorkspace.GetMemoryLocalDirectory(globalMemoryDirectory));

        return GetGlobalCuratedDirectory();
    }

    private string GetCuratedDirectoryForScope(string scope)
        => scope == MemoryScopes.Global
            ? GetGlobalCuratedDirectory()
            : GetCuratedDirectory(RequireProjectWorkspaceDirectory());

    private string GetGlobalCuratedDirectory()
        => AgentWorkspace.GetMemoryCuratedDirectory(globalMemoryDirectory);

    private static string GetLocalDirectory(string workspaceDirectory)
        => AgentWorkspace.GetMemoryLocalDirectory(AgentWorkspace.GetMemoryDirectory(workspaceDirectory));

    private string GetGlobalLocalDirectory()
        => AgentWorkspace.GetMemoryLocalDirectory(globalMemoryDirectory);

    private string RequireProjectWorkspaceDirectory()
        => FindProjectWorkspaceDirectory()
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

    private static string GetCuratedDirectory(string workspaceDirectory)
        => AgentWorkspace.GetMemoryCuratedDirectory(AgentWorkspace.GetMemoryDirectory(workspaceDirectory));

    private static string GetCuratedPath(string curatedDirectory, string id)
        => Path.Combine(curatedDirectory, id + ".md");

    private static MemoryRecord ToRecord(MemoryFrontmatter frontmatter, string path, string scope) => new()
    {
        Id = frontmatter.Id,
        Scope = scope,
        Type = frontmatter.Type,
        Tags = frontmatter.Tags,
        Path = path,
        Body = frontmatter.Body,
        CreatedAt = frontmatter.CreatedAt,
        UpdatedAt = frontmatter.UpdatedAt,
        CreatedBy = frontmatter.CreatedBy,
        PromotedFrom = frontmatter.PromotedFrom
    };

    private static string ValidateWriteScope(string scope)
    {
        var normalized = MemoryScopes.Normalize(scope);

        if (!MemoryScopes.IsValid(normalized))
        {
            throw new ExitException($"The scope '{scope}' is invalid. Use 'project' or 'global'.");
        }

        return normalized;
    }

    private static string ValidateReadScope(string scope)
    {
        var normalized = MemoryScopes.Normalize(scope);

        if (!MemoryScopes.IsValidReadScope(normalized))
        {
            throw new ExitException($"The scope '{scope}' is invalid. Use 'project', 'global', or 'all'.");
        }

        return normalized;
    }

    private static string ValidateType(string type)
    {
        var normalized = MemoryTypes.Normalize(type);

        if (!MemoryTypes.IsValid(normalized))
        {
            throw new ExitException(
                $"The type '{type}' is invalid. A type may contain only lowercase letters, digits, "
                + "and hyphens, up to 40 characters.");
        }

        return normalized;
    }

    private static string ValidateTag(string tag)
    {
        var normalized = MemoryTags.Normalize(tag);

        if (!MemoryTags.IsValid(normalized))
        {
            throw new ExitException(
                $"The tag '{tag}' is invalid. A tag may contain only lowercase letters, digits, "
                + "and hyphens, up to 40 characters.");
        }

        return normalized;
    }

    private static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string> tags)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var normalized = new List<string>();

        foreach (var tag in tags)
        {
            var value = ValidateTag(tag);

            if (seen.Add(value))
            {
                normalized.Add(value);
            }
        }

        return normalized;
    }

    private static IReadOnlyList<string> ApplyTagChanges(
        IReadOnlyList<string> currentTags,
        IReadOnlyList<string> addTags,
        IReadOnlyList<string> removeTags)
    {
        var tags = currentTags.ToList();

        foreach (var tag in addTags)
        {
            var value = ValidateTag(tag);

            if (!tags.Contains(value, StringComparer.Ordinal))
            {
                tags.Add(value);
            }
        }

        if (removeTags.Count > 0)
        {
            var remove = new HashSet<string>(
                removeTags.Select(ValidateTag), StringComparer.Ordinal);
            tags.RemoveAll(remove.Contains);
        }

        return tags;
    }

    private static string ValidateActor(string actor)
    {
        var trimmed = actor.Trim();

        if (trimmed.Length == 0 || trimmed.Contains('\n') || trimmed.Contains('\r'))
        {
            throw new ExitException("The actor must not be empty or contain line breaks.");
        }

        return trimmed;
    }

    private void CreateIfMissing(string directory)
    {
        if (!fileSystem.DirectoryExists(directory))
        {
            fileSystem.CreateDirectory(directory);
        }
    }
}
