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

    private const int MaxPrefixLength = 64;

    /// <summary>
    /// Ignores only the SQLite database files, which are local state; the
    /// JSONL export and this file itself are the tracker's committed,
    /// durable state and must not be ignored.
    /// </summary>
    public const string GitIgnoreContent =
        """
        # The agent database is local state; tasks.jsonl is the source of truth in git.
        agents.db
        agents.db-wal
        agents.db-shm
        """;

    public static string GetDirectory(string baseDirectory)
        => Path.Combine(baseDirectory, RootDirectoryName, AgentsDirectoryName);

    public static string GetDatabasePath(string workspaceDirectory)
        => Path.Combine(workspaceDirectory, DatabaseFileName);

    public static string GetJsonlPath(string workspaceDirectory)
        => Path.Combine(workspaceDirectory, JsonlFileName);

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
