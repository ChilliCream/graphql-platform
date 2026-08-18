namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// Defines the on-disk layout of a task workspace and shared helpers for it.
/// </summary>
internal static class TaskWorkspace
{
    public const string RootDirectoryName = ".nitro";
    public const string TasksDirectoryName = "tasks";
    public const string DatabaseFileName = "tasks.db";
    public const string GitIgnoreFileName = ".gitignore";
    public const string FallbackPrefix = "task";
    public const string DisplayPath = RootDirectoryName + "/" + TasksDirectoryName;

    private const int MaxPrefixLength = 64;

    public const string GitIgnoreContent =
        """
        # The task database is local state and must not be committed.
        *
        !.gitignore
        """;

    public static string GetDirectory(string baseDirectory)
        => Path.Combine(baseDirectory, RootDirectoryName, TasksDirectoryName);

    public static string GetDatabasePath(string workspaceDirectory)
        => Path.Combine(workspaceDirectory, DatabaseFileName);

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
