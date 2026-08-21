namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Defines the on-disk layout of the unified agent workspace, shared by the
/// task tracker and the mail feature, and shared helpers for it.
/// </summary>
internal static class AgentWorkspace
{
    public const string RootDirectoryName = ".nitro";
    public const string AgentsDirectoryName = "agents";
    public const string DatabaseFileName = "agents.db";
    public const string JsonlFileName = "tasks.jsonl";
    public const string GitIgnoreFileName = ".gitignore";
    public const string FallbackPrefix = "task";
    public const string DisplayPath = RootDirectoryName + "/" + AgentsDirectoryName;

    public const string MemoryDirectoryName = "memory";
    public const string MemoryCuratedDirectoryName = "curated";
    public const string MemoryJournalDirectoryName = "journal";
    public const string MemoryLocalDirectoryName = ".local";
    public const string MemoryIndexDatabaseFileName = "index.db";
    private const string GlobalConfigDirectoryName = "nitro";

    private const int MaxPrefixLength = 64;

    /// <summary>
    /// Ignores the SQLite database files and the disposable memory index,
    /// which are local state; the JSONL export, the memory markdown, and
    /// this file itself are the committed, durable state and must not be
    /// ignored.
    /// </summary>
    public const string GitIgnoreContent =
        """
        # The agent database is local state; tasks.jsonl is the source of truth in git.
        agents.db
        agents.db-wal
        agents.db-shm

        # The memory index is a disposable, rebuildable cache; the curated and
        # journal markdown under memory/ is the source of truth in git.
        memory/.local/
        """;

    public static string GetDirectory(string baseDirectory)
        => Path.Combine(baseDirectory, RootDirectoryName, AgentsDirectoryName);

    public static string GetDatabasePath(string workspaceDirectory)
        => Path.Combine(workspaceDirectory, DatabaseFileName);

    public static string GetJsonlPath(string workspaceDirectory)
        => Path.Combine(workspaceDirectory, JsonlFileName);

    /// <summary>
    /// The project memory root, nested under the shared agent workspace
    /// directory returned by <see cref="GetDirectory"/>.
    /// </summary>
    public static string GetMemoryDirectory(string workspaceDirectory)
        => Path.Combine(workspaceDirectory, MemoryDirectoryName);

    /// <summary>
    /// The machine-local global memory root, under the platform's
    /// application data directory. Independent of any project workspace.
    /// </summary>
    public static string GetGlobalMemoryDirectory(string applicationDataDirectory)
        => Path.Combine(applicationDataDirectory, GlobalConfigDirectoryName, MemoryDirectoryName);

    public static string GetMemoryCuratedDirectory(string memoryDirectory)
        => Path.Combine(memoryDirectory, MemoryCuratedDirectoryName);

    public static string GetMemoryJournalDirectory(string memoryDirectory)
        => Path.Combine(memoryDirectory, MemoryJournalDirectoryName);

    public static string GetMemoryLocalDirectory(string memoryDirectory)
        => Path.Combine(memoryDirectory, MemoryLocalDirectoryName);

    public static string GetMemoryIndexDatabasePath(string memoryLocalDirectory)
        => Path.Combine(memoryLocalDirectory, MemoryIndexDatabaseFileName);

    /// <summary>
    /// Finds the nearest workspace directory at or above the given directory.
    /// Returns null when no workspace exists.
    /// </summary>
    public static string? Find(IFileSystem fileSystem, string startDirectory)
    {
        for (var directory = startDirectory;
            !string.IsNullOrEmpty(directory);
            directory = Path.GetDirectoryName(directory))
        {
            var workspaceDirectory = GetDirectory(directory);

            if (fileSystem.FileExists(GetDatabasePath(workspaceDirectory)))
            {
                return workspaceDirectory;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the nearest workspace directory at or above the given directory
    /// that has either an agent database or a committed tasks.jsonl. Returns
    /// null when neither exists.
    /// </summary>
    public static string? FindDatabaseOrJsonl(IFileSystem fileSystem, string startDirectory)
    {
        for (var directory = startDirectory;
            !string.IsNullOrEmpty(directory);
            directory = Path.GetDirectoryName(directory))
        {
            var workspaceDirectory = GetDirectory(directory);

            if (fileSystem.FileExists(GetDatabasePath(workspaceDirectory))
                || fileSystem.FileExists(GetJsonlPath(workspaceDirectory)))
            {
                return workspaceDirectory;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the nearest workspace directory at or above the given directory
    /// that has either an agent database or project memory markdown.
    /// Memory storage is markdown-first, so a freshly cloned repository with
    /// committed curated or journal entries but no database yet still
    /// counts. Returns null when neither exists.
    /// </summary>
    public static string? FindMemory(IFileSystem fileSystem, string startDirectory)
    {
        for (var directory = startDirectory;
            !string.IsNullOrEmpty(directory);
            directory = Path.GetDirectoryName(directory))
        {
            var workspaceDirectory = GetDirectory(directory);

            if (fileSystem.FileExists(GetDatabasePath(workspaceDirectory)))
            {
                return workspaceDirectory;
            }

            var memoryDirectory = GetMemoryDirectory(workspaceDirectory);

            if (fileSystem.DirectoryExists(GetMemoryCuratedDirectory(memoryDirectory))
                || fileSystem.DirectoryExists(GetMemoryJournalDirectory(memoryDirectory)))
            {
                return workspaceDirectory;
            }
        }

        return null;
    }

    /// <summary>
    /// Normalizes a task ID prefix to lowercase letters, digits, hyphens, and
    /// underscores. Returns <see cref="FallbackPrefix"/> when nothing remains.
    /// </summary>
    public static string NormalizePrefix(string value)
    {
        Span<char> buffer = stackalloc char[MaxPrefixLength];
        var length = 0;

        foreach (var c in value)
        {
            if (length == MaxPrefixLength)
            {
                break;
            }

            var lower = char.ToLowerInvariant(c);

            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_')
            {
                buffer[length++] = lower;
            }
        }

        var trimmed = buffer[..length].Trim("-_");

        return trimmed.IsEmpty ? FallbackPrefix : trimmed.ToString();
    }
}
