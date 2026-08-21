using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The project memory store: directory provisioning and discovery, and the
/// curated vertical (save, update, forget, show, recent) reading and
/// writing markdown files directly. The disposable FTS index and the
/// journal land in later slices.
/// </summary>
internal sealed class MemoryStore(IFileSystem fileSystem, TimeProvider timeProvider) : IMemoryStore
{
    // Matches the threshold AtomicFileSystemTests exercises: a temp file
    // this old was abandoned by a crashed or cancelled write, not one still
    // in flight.
    private static readonly TimeSpan AbandonedTempFileAge = TimeSpan.FromHours(1);

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
        var type = ValidateType(creation.Type);
        var tags = NormalizeTags(creation.Tags);
        var actor = ValidateActor(creation.Actor);

        var workspaceDirectory = RequireProjectWorkspaceDirectory();

        // Provisioning happens on `agent init`; this is the lazy-creation
        // fallback for a workspace that has an agent database but has never
        // written a curated memory before.
        await EnsureProjectWorkspaceAsync(workspaceDirectory, cancellationToken);

        var curatedDirectory = GetCuratedDirectory(workspaceDirectory);
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

        return ToRecord(frontmatter, path);
    }

    public async Task<MemoryRecord> UpdateAsync(
        string id, MemoryRecordUpdate update, CancellationToken cancellationToken)
    {
        var record = await GetRequiredAsync(id, cancellationToken);

        var text = update.TextGiven ? update.Text ?? "" : record.Body;
        var type = update.TypeGiven ? ValidateType(update.Type ?? "") : record.Type;
        var tags = ApplyTagChanges(record.Tags, update.AddTags, update.RemoveTags);

        var curatedDirectory = GetCuratedDirectory(RequireProjectWorkspaceDirectory());
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

        return ToRecord(frontmatter, record.Path);
    }

    public async Task<MemoryRecord> ForgetAsync(string id, CancellationToken cancellationToken)
    {
        var record = await GetRequiredAsync(id, cancellationToken);

        fileSystem.DeleteFile(record.Path);

        return record;
    }

    public async Task<MemoryRecord?> FindAsync(string id, CancellationToken cancellationToken)
    {
        var workspaceDirectory = FindProjectWorkspaceDirectory();

        if (workspaceDirectory is null)
        {
            return null;
        }

        var path = GetCuratedPath(GetCuratedDirectory(workspaceDirectory), id);

        if (!fileSystem.FileExists(path))
        {
            return null;
        }

        var content = await fileSystem.ReadAllTextAsync(path, cancellationToken);

        if (!MemoryFrontmatterParser.TryParse(content, id, out var frontmatter, out var failure))
        {
            throw new ExitException($"Memory '{id}' has malformed frontmatter: {failure.Message}");
        }

        return ToRecord(frontmatter, path);
    }

    public async Task<MemoryRecord> GetRequiredAsync(string id, CancellationToken cancellationToken)
        => await FindAsync(id, cancellationToken)
            ?? throw new ExitException($"Memory '{id}' does not exist.");

    public async Task<IReadOnlyList<MemoryRecord>> GetRecentCuratedAsync(
        int? limit, CancellationToken cancellationToken)
    {
        var workspaceDirectory = FindProjectWorkspaceDirectory();

        if (workspaceDirectory is null)
        {
            return [];
        }

        var curatedDirectory = GetCuratedDirectory(workspaceDirectory);

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

            records.Add(ToRecord(frontmatter, path));
        }

        var ordered = records
            .OrderByDescending(record => record.UpdatedAt)
            .ThenBy(record => record.Id, StringComparer.Ordinal)
            .AsEnumerable();

        if (limit is { } value)
        {
            ordered = ordered.Take(value);
        }

        return ordered.ToList();
    }

    private string RequireProjectWorkspaceDirectory()
        => FindProjectWorkspaceDirectory()
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

    private static string GetCuratedDirectory(string workspaceDirectory)
        => AgentWorkspace.GetMemoryCuratedDirectory(AgentWorkspace.GetMemoryDirectory(workspaceDirectory));

    private static string GetCuratedPath(string curatedDirectory, string id)
        => Path.Combine(curatedDirectory, id + ".md");

    private static MemoryRecord ToRecord(MemoryFrontmatter frontmatter, string path) => new()
    {
        Id = frontmatter.Id,
        Scope = MemoryScopes.Project,
        Type = frontmatter.Type,
        Tags = frontmatter.Tags,
        Path = path,
        Body = frontmatter.Body,
        CreatedAt = frontmatter.CreatedAt,
        UpdatedAt = frontmatter.UpdatedAt,
        CreatedBy = frontmatter.CreatedBy,
        PromotedFrom = frontmatter.PromotedFrom
    };

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
        if (actor.Length == 0 || actor.Contains('\n') || actor.Contains('\r'))
        {
            throw new ExitException("The actor must not be empty or contain line breaks.");
        }

        return actor;
    }

    private void CreateIfMissing(string directory)
    {
        if (!fileSystem.DirectoryExists(directory))
        {
            fileSystem.CreateDirectory(directory);
        }
    }
}
