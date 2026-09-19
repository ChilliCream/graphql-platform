using System.Globalization;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves the shared agent workspace under a Git common directory or
/// <c>.nitro/agents</c>. At each searched directory, an initialized fallback workspace
/// takes precedence over a Git workspace.
/// </summary>
internal static class AgentWorkspace
{
    public const string RootDirectoryName = ".nitro";
    public const string AgentsDirectoryName = "agents";
    public const string DatabaseFileName = "agents.db";
    public const string GitIgnoreFileName = ".gitignore";
    public const string FallbackPrefix = "task";
    public const string DisplayPath = RootDirectoryName + "/" + AgentsDirectoryName;

    public const string GitDirectoryName = ".git";
    public const string GitWorkspaceDirectoryName = "nitro";
    public const string GitDisplayPath = GitDirectoryName + "/" + GitWorkspaceDirectoryName;

    public const string MemoryDirectoryName = "memory";
    public const string MemoryCuratedDirectoryName = "curated";
    public const string MemoryJournalDirectoryName = "journal";
    public const string MemoryLocalDirectoryName = ".local";
    public const string MemoryIndexDatabaseFileName = "index.db";
    private const string GlobalConfigDirectoryName = "nitro";

    private const int MaxPrefixLength = 64;

    /// <summary>
    /// Git ignore rules for workspace database files and the legacy memory index directory.
    /// </summary>
    public const string GitIgnoreContent =
        """
        # Local agent workspace database files.
        agents.db
        agents.db-wal
        agents.db-shm

        # Legacy memory index files.
        memory/.local/
        """;

    /// <summary>
    /// The fallback workspace directory (<c>.nitro/agents</c>) under the
    /// given project directory, used outside a git repository.
    /// </summary>
    public static string GetDirectory(string baseDirectory)
        => Path.Combine(baseDirectory, RootDirectoryName, AgentsDirectoryName);

    /// <summary>
    /// The workspace directory inside a repository's git common directory.
    /// </summary>
    public static string GetGitWorkspaceDirectory(string gitCommonDirectory)
        => Path.Combine(gitCommonDirectory, GitWorkspaceDirectoryName);

    /// <summary>
    /// True when the workspace uses the <c>.nitro/agents</c> fallback layout
    /// rather than living inside a git common directory.
    /// </summary>
    public static bool IsFallbackLayout(string workspaceDirectory)
    {
        var trimmed = Path.TrimEndingDirectorySeparator(workspaceDirectory);

        return Path.GetFileName(trimmed) == AgentsDirectoryName
            && Path.GetFileName(Path.GetDirectoryName(trimmed) ?? "") == RootDirectoryName;
    }

    /// <summary>
    /// The display form of a workspace path: <c>.nitro/agents</c> or
    /// <c>.git/nitro</c> for the two standard layouts, the full path
    /// otherwise.
    /// </summary>
    public static string GetDisplayPath(string workspaceDirectory)
    {
        var normalized = workspaceDirectory.Replace('\\', '/').TrimEnd('/');

        if (normalized == DisplayPath || normalized.EndsWith("/" + DisplayPath, StringComparison.Ordinal))
        {
            return DisplayPath;
        }

        if (normalized.EndsWith("/" + GitDisplayPath, StringComparison.Ordinal))
        {
            return GitDisplayPath;
        }

        return workspaceDirectory;
    }

    public static string GetDatabasePath(string workspaceDirectory)
        => Path.Combine(workspaceDirectory, DatabaseFileName);

    /// <summary>
    /// Returns the legacy memory directory under the supplied workspace directory.
    /// </summary>
    public static string GetMemoryDirectory(string workspaceDirectory)
        => Path.Combine(workspaceDirectory, MemoryDirectoryName);

    /// <summary>
    /// Returns the Nitro configuration directory under the supplied application data directory.
    /// </summary>
    public static string GetGlobalConfigDirectory(string applicationDataDirectory)
        => Path.Combine(applicationDataDirectory, GlobalConfigDirectoryName);

    /// <summary>
    /// Returns the legacy global memory directory under the supplied application data directory.
    /// </summary>
    public static string GetGlobalMemoryDirectory(string applicationDataDirectory)
        => Path.Combine(GetGlobalConfigDirectory(applicationDataDirectory), MemoryDirectoryName);

    public static string GetMemoryCuratedDirectory(string memoryDirectory)
        => Path.Combine(memoryDirectory, MemoryCuratedDirectoryName);

    public static string GetMemoryJournalDirectory(string memoryDirectory)
        => Path.Combine(memoryDirectory, MemoryJournalDirectoryName);

    /// <summary>
    /// A journal entry's date directory, named by its UTC capture date.
    /// </summary>
    public static string GetMemoryJournalDateDirectory(string memoryJournalDirectory, DateOnly utcDate)
        => Path.Combine(memoryJournalDirectory, utcDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

    public static string GetMemoryLocalDirectory(string memoryDirectory)
        => Path.Combine(memoryDirectory, MemoryLocalDirectoryName);

    public static string GetMemoryIndexDatabasePath(string memoryLocalDirectory)
        => Path.Combine(memoryLocalDirectory, MemoryIndexDatabaseFileName);

    /// <summary>
    /// Finds the nearest initialized workspace at or above the given
    /// directory. Returns null when no workspace exists.
    /// </summary>
    public static string? Find(IFileSystem fileSystem, string startDirectory)
        => FindLocation(fileSystem, startDirectory)?.WorkspaceDirectory;

    /// <summary>
    /// Finds the nearest workspace containing an agent database and returns its project,
    /// checkout, and workspace directories. At each level, the fallback layout takes
    /// precedence over Git; null means no initialized workspace was found.
    /// </summary>
    public static WorkspaceLocation? FindLocation(IFileSystem fileSystem, string startDirectory)
    {
        for (var directory = startDirectory;
            !string.IsNullOrEmpty(directory);
            directory = Path.GetDirectoryName(directory))
        {
            var fallbackDirectory = GetDirectory(directory);

            if (fileSystem.FileExists(GetDatabasePath(fallbackDirectory)))
            {
                return new WorkspaceLocation(directory, directory, fallbackDirectory);
            }

            var gitWorkspace = FindGitWorkspaceAt(fileSystem, directory);

            if (gitWorkspace is not null
                && fileSystem.FileExists(GetDatabasePath(gitWorkspace.Value.WorkspaceDirectory)))
            {
                return gitWorkspace;
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves where init operates for the given directory: the nearest
    /// initialized workspace, else, per level walking up, an existing bare
    /// <c>.nitro/agents</c> directory or a repository's <c>.git/nitro</c>,
    /// else a fresh <c>.nitro/agents</c> under the start directory.
    /// </summary>
    public static WorkspaceLocation ResolveForInit(IFileSystem fileSystem, string startDirectory)
    {
        var initialized = FindLocation(fileSystem, startDirectory);

        if (initialized is not null)
        {
            return initialized.Value;
        }

        for (var directory = startDirectory;
            !string.IsNullOrEmpty(directory);
            directory = Path.GetDirectoryName(directory))
        {
            var fallbackDirectory = GetDirectory(directory);

            if (fileSystem.DirectoryExists(fallbackDirectory))
            {
                return new WorkspaceLocation(directory, directory, fallbackDirectory);
            }

            var gitWorkspace = FindGitWorkspaceAt(fileSystem, directory);

            if (gitWorkspace is not null)
            {
                return gitWorkspace.Value;
            }
        }

        return new WorkspaceLocation(startDirectory, startDirectory, GetDirectory(startDirectory));
    }

    /// <summary>
    /// Finds the nearest git repository root at or above the given directory
    /// and returns its <c>.git/nitro</c> workspace location, whether or not
    /// it exists yet. Returns null when no repository is found.
    /// </summary>
    public static WorkspaceLocation? FindGitWorkspace(IFileSystem fileSystem, string startDirectory)
    {
        for (var directory = startDirectory;
            !string.IsNullOrEmpty(directory);
            directory = Path.GetDirectoryName(directory))
        {
            var gitWorkspace = FindGitWorkspaceAt(fileSystem, directory);

            if (gitWorkspace is not null)
            {
                return gitWorkspace;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the Git workspace location for this checkout root, or null when its Git
    /// directory cannot be resolved. Linked worktrees share the common workspace
    /// directory and retain their own checkout directory.
    /// </summary>
    private static WorkspaceLocation? FindGitWorkspaceAt(IFileSystem fileSystem, string directory)
    {
        var gitCommonDirectory = ResolveGitCommonDirectory(fileSystem, directory);

        if (gitCommonDirectory is null)
        {
            return null;
        }

        var projectDirectory =
            Path.GetFileName(gitCommonDirectory) == GitDirectoryName
                ? Path.GetDirectoryName(gitCommonDirectory) ?? directory
                : directory;

        return new WorkspaceLocation(
            projectDirectory, directory, GetGitWorkspaceDirectory(gitCommonDirectory));
    }

    /// <summary>
    /// The git common directory for a repository rooted at exactly the given
    /// directory: the <c>.git</c> directory itself, or the directory a
    /// linked worktree's or submodule's <c>.git</c> file points to,
    /// following its <c>commondir</c> redirect. Returns null when the
    /// directory is not a repository root or the pointer cannot be resolved.
    /// </summary>
    public static string? ResolveGitCommonDirectory(IFileSystem fileSystem, string baseDirectory)
    {
        var gitPath = Path.Combine(baseDirectory, GitDirectoryName);

        if (fileSystem.DirectoryExists(gitPath))
        {
            return gitPath;
        }

        if (!fileSystem.FileExists(gitPath))
        {
            return null;
        }

        var gitDirectory = ResolveGitFileTarget(fileSystem, gitPath, baseDirectory);

        if (gitDirectory is null)
        {
            return null;
        }

        var commonDirPointerPath = Path.Combine(gitDirectory, "commondir");

        if (!fileSystem.FileExists(commonDirPointerPath))
        {
            return gitDirectory;
        }

        var commonDirectory = ReadAllText(fileSystem, commonDirPointerPath).Trim();

        if (commonDirectory.Length == 0)
        {
            return gitDirectory;
        }

        var resolved = Path.GetFullPath(
            Path.IsPathRooted(commonDirectory)
                ? commonDirectory
                : Path.Combine(gitDirectory, commonDirectory));

        return fileSystem.DirectoryExists(resolved) ? resolved : null;
    }

    /// <summary>
    /// Resolves a <c>.git</c> file's <c>gitdir:</c> pointer to an absolute,
    /// existing directory. Returns null for a malformed pointer or a target
    /// that does not exist.
    /// </summary>
    private static string? ResolveGitFileTarget(
        IFileSystem fileSystem,
        string gitFilePath,
        string baseDirectory)
    {
        const string prefix = "gitdir:";
        var firstLine = ReadAllText(fileSystem, gitFilePath).ReplaceLineEndings("\n").Split('\n')[0].Trim();

        if (!firstLine.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var target = firstLine[prefix.Length..].Trim();

        if (target.Length == 0)
        {
            return null;
        }

        var resolved = Path.GetFullPath(
            Path.IsPathRooted(target) ? target : Path.Combine(baseDirectory, target));

        return fileSystem.DirectoryExists(resolved) ? resolved : null;
    }

    private static string ReadAllText(IFileSystem fileSystem, string path)
    {
        using var stream = fileSystem.OpenReadStream(path);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Finds the nearest workspace with an agent database or a legacy curated or journal
    /// directory, preferring a database at each level. Returns null when none is found.
    /// </summary>
    public static string? FindMemory(IFileSystem fileSystem, string startDirectory)
    {
        for (var directory = startDirectory;
            !string.IsNullOrEmpty(directory);
            directory = Path.GetDirectoryName(directory))
        {
            var fallbackDirectory = GetDirectory(directory);

            if (fileSystem.FileExists(GetDatabasePath(fallbackDirectory)))
            {
                return fallbackDirectory;
            }

            var gitWorkspaceDirectory =
                FindGitWorkspaceAt(fileSystem, directory)?.WorkspaceDirectory;

            if (gitWorkspaceDirectory is not null
                && fileSystem.FileExists(GetDatabasePath(gitWorkspaceDirectory)))
            {
                return gitWorkspaceDirectory;
            }

            if (HasMemoryMarkdown(fileSystem, fallbackDirectory))
            {
                return fallbackDirectory;
            }

            if (gitWorkspaceDirectory is not null
                && HasMemoryMarkdown(fileSystem, gitWorkspaceDirectory))
            {
                return gitWorkspaceDirectory;
            }
        }

        return null;
    }

    private static bool HasMemoryMarkdown(IFileSystem fileSystem, string workspaceDirectory)
    {
        var memoryDirectory = GetMemoryDirectory(workspaceDirectory);

        return fileSystem.DirectoryExists(GetMemoryCuratedDirectory(memoryDirectory))
            || fileSystem.DirectoryExists(GetMemoryJournalDirectory(memoryDirectory));
    }

    /// <summary>
    /// Keeps at most 64 lowercase ASCII letters, digits, hyphens, or underscores and
    /// trims leading and trailing hyphens and underscores. Returns <see cref="FallbackPrefix"/>
    /// when nothing remains.
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
