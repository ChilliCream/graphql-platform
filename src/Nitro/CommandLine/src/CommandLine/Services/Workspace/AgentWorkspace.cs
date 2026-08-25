using System.Globalization;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Defines the on-disk layout of the unified agent workspace, shared by the
/// task tracker and the mail feature, and shared helpers for it. In a git
/// repository the workspace lives inside the git common directory
/// (<c>.git/nitro</c>); outside git it lives at <c>.nitro/agents</c>, and an
/// existing <c>.nitro/agents</c> always takes precedence over git.
/// </summary>
internal static class AgentWorkspace
{
    public const string RootDirectoryName = ".nitro";
    public const string AgentsDirectoryName = "agents";
    public const string DatabaseFileName = "agents.db";
    public const string LegacyJsonlFileName = "tasks.jsonl";
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
    /// Ignores the SQLite database files and the disposable memory index,
    /// which are local state; the memory markdown and this file itself are
    /// the committed, durable state and must not be ignored.
    /// </summary>
    public const string GitIgnoreContent =
        """
        # The agent database is the source of truth for tasks and mail. It is
        # local, machine-specific state and is never committed.
        agents.db
        agents.db-wal
        agents.db-shm

        # The memory index is a disposable, rebuildable cache; the curated and
        # journal markdown under memory/ is the source of truth in git.
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
    /// The tasks.jsonl path from the retired JSONL sync model. The file is
    /// imported and removed during init; the database is the source of truth.
    /// </summary>
    public static string GetLegacyJsonlPath(string workspaceDirectory)
        => Path.Combine(workspaceDirectory, LegacyJsonlFileName);

    /// <summary>
    /// The project memory root, nested under the shared agent workspace
    /// directory returned by <see cref="GetDirectory"/>.
    /// </summary>
    public static string GetMemoryDirectory(string workspaceDirectory)
        => Path.Combine(workspaceDirectory, MemoryDirectoryName);

    /// <summary>
    /// The machine-local Nitro root under the platform's application data
    /// directory, shared by every global (non-project) feature: the memory
    /// store, the instance id fallback file, and future global config.
    /// </summary>
    public static string GetGlobalConfigDirectory(string applicationDataDirectory)
        => Path.Combine(applicationDataDirectory, GlobalConfigDirectoryName);

    /// <summary>
    /// The machine-local global memory root, under the platform's
    /// application data directory. Independent of any project workspace.
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
    /// Finds the nearest initialized workspace at or above the given
    /// directory, together with the project directory that owns it. At each
    /// level a <c>.nitro/agents</c> workspace takes precedence over the
    /// repository's <c>.git/nitro</c>. Returns null when no workspace
    /// exists.
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
                return new WorkspaceLocation(directory, fallbackDirectory);
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
    /// Finds the nearest existing <c>.nitro/agents</c> directory at or above
    /// the given directory, initialized or not, together with the project
    /// directory that owns it. Returns null when none exists.
    /// </summary>
    public static WorkspaceLocation? FindFallbackDirectory(IFileSystem fileSystem, string startDirectory)
    {
        for (var directory = startDirectory;
            !string.IsNullOrEmpty(directory);
            directory = Path.GetDirectoryName(directory))
        {
            var fallbackDirectory = GetDirectory(directory);

            if (fileSystem.DirectoryExists(fallbackDirectory))
            {
                return new WorkspaceLocation(directory, fallbackDirectory);
            }
        }

        return null;
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
    /// The <c>.git/nitro</c> workspace location for a repository rooted at
    /// exactly the given directory, or null when it is not a repository
    /// root. The project directory is the main checkout root, so every
    /// linked worktree of a repository maps to the same location.
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

        return new WorkspaceLocation(projectDirectory, GetGitWorkspaceDirectory(gitCommonDirectory));
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

        return Path.GetFullPath(
            Path.IsPathRooted(commonDirectory)
                ? commonDirectory
                : Path.Combine(gitDirectory, commonDirectory));
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
            var fallbackDirectory = GetDirectory(directory);

            if (HasDatabaseOrMemory(fileSystem, fallbackDirectory))
            {
                return fallbackDirectory;
            }

            var gitWorkspace = FindGitWorkspaceAt(fileSystem, directory);

            if (gitWorkspace is not null
                && HasDatabaseOrMemory(fileSystem, gitWorkspace.Value.WorkspaceDirectory))
            {
                return gitWorkspace.Value.WorkspaceDirectory;
            }
        }

        return null;
    }

    private static bool HasDatabaseOrMemory(IFileSystem fileSystem, string workspaceDirectory)
    {
        if (fileSystem.FileExists(GetDatabasePath(workspaceDirectory)))
        {
            return true;
        }

        var memoryDirectory = GetMemoryDirectory(workspaceDirectory);

        return fileSystem.DirectoryExists(GetMemoryCuratedDirectory(memoryDirectory))
            || fileSystem.DirectoryExists(GetMemoryJournalDirectory(memoryDirectory));
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

/// <summary>
/// A resolved workspace location: the workspace directory and the project
/// directory that owns it (the directory containing <c>.nitro</c>, or the
/// repository's main checkout root).
/// </summary>
internal readonly record struct WorkspaceLocation(string ProjectDirectory, string WorkspaceDirectory);
