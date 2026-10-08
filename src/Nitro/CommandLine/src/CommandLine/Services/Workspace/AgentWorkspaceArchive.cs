using System.IO.Compression;
using System.IO.Enumeration;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Results;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Defines the layout of an agent workspace archive and writes one: a zip with a root
/// <c>manifest.json</c>, the project's <c>.nitro</c> directory under <c>repo/.nitro</c>, and
/// the git workspace directory under <c>git/nitro</c>.
/// </summary>
internal static partial class AgentWorkspaceArchive
{
    /// <summary>
    /// The archive format version written to the manifest.
    /// </summary>
    public const int FormatVersion = 1;

    public const string ManifestEntryName = "manifest.json";
    public const string ProjectRootEntryName = "repo/" + AgentWorkspace.RootDirectoryName;
    public const string GitRootEntryName = "git/" + AgentWorkspace.GitWorkspaceDirectoryName;

    private const string DatabaseWalFileName = AgentWorkspace.DatabaseFileName + "-wal";
    private const string DatabaseSharedMemoryFileName = AgentWorkspace.DatabaseFileName + "-shm";

    private static readonly DateTime s_minEntryTime = new(1980, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime s_maxEntryTime = new(2107, 12, 31, 23, 59, 58, DateTimeKind.Unspecified);

    /// <summary>
    /// Resolves the project <c>.nitro</c> directory and the git workspace directory an archive
    /// covers, whether or not they exist. Outside a git repository the git workspace directory
    /// is null and the project directory belongs to the nearest workspace.
    /// </summary>
    public static (string ProjectDirectory, string? GitWorkspaceDirectory) ResolveDirectories(
        IFileSystem fileSystem,
        string currentDirectory)
    {
        var gitWorkspace = AgentWorkspace.FindGitWorkspace(fileSystem, currentDirectory);
        var location = gitWorkspace
            ?? AgentWorkspace.ResolveForInit(fileSystem, currentDirectory);

        return (
            Path.Combine(location.ProjectDirectory, AgentWorkspace.RootDirectoryName),
            gitWorkspace?.WorkspaceDirectory);
    }

    /// <summary>
    /// Writes the archive for the given directories, where a null directory is not backed up,
    /// and moves it into place when complete. An existing archive is replaced only when
    /// <paramref name="overwrite"/> is true.
    /// </summary>
    public static async Task<AgentWorkspaceArchiveSummary> WriteAsync(
        string archivePath,
        string? projectDirectory,
        string? gitWorkspaceDirectory,
        bool overwrite,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        archivePath = Path.GetFullPath(archivePath);

        var roots = new List<ArchiveRoot>();

        if (projectDirectory is not null)
        {
            var directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(projectDirectory));

            roots.Add(new ArchiveRoot(
                ProjectRootEntryName,
                directory,
                Path.Combine(directory, AgentWorkspace.AgentsDirectoryName)));
        }

        if (gitWorkspaceDirectory is not null)
        {
            var directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(gitWorkspaceDirectory));

            roots.Add(new ArchiveRoot(GitRootEntryName, directory, directory));
        }

        ValidateDestination(archivePath, roots, overwrite);

        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);

        var temporaryArchivePath = archivePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var snapshotDirectory = Directory.CreateTempSubdirectory("nitro-agent-backup-");

        try
        {
            var snapshots = await SnapshotDatabasesAsync(
                roots, snapshotDirectory.FullName, cancellationToken);

            var manifest = new AgentWorkspaceArchiveManifest(
                FormatVersion,
                NitroCliVersion.Current,
                timeProvider.GetUtcNow(),
                snapshots.Count > 0 ? snapshots[0].Version : null,
                roots.Select(root => root.EntryName).ToArray());

            var fileCount = 0;

            await using (var stream = new FileStream(
                temporaryArchivePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                // ZipArchive implements IAsyncDisposable only on .NET 10 and later.
#pragma warning disable RCS1261
                using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
#pragma warning restore RCS1261

                var manifestEntry = archive.CreateEntry(ManifestEntryName, CompressionLevel.Optimal);

                await using (var manifestStream = manifestEntry.Open())
                {
                    await JsonSerializer.SerializeAsync(
                        manifestStream,
                        manifest,
                        JsonSourceGenerationContext.Default.AgentWorkspaceArchiveManifest,
                        cancellationToken);
                }

                foreach (var root in roots)
                {
                    fileCount += await AddRootAsync(
                        archive, root, snapshots, cancellationToken);
                }
            }

            File.Move(temporaryArchivePath, archivePath, overwrite);

            return new AgentWorkspaceArchiveSummary(
                archivePath,
                manifest.Roots,
                fileCount,
                new FileInfo(archivePath).Length);
        }
        finally
        {
            DeleteIfExists(temporaryArchivePath);
            snapshotDirectory.Delete(recursive: true);
        }
    }

    private static void ValidateDestination(
        string archivePath,
        List<ArchiveRoot> roots,
        bool overwrite)
    {
        foreach (var root in roots)
        {
            if (IsWithin(root.Directory, archivePath))
            {
                throw ThrowHelper.ArchiveInsideBackedUpDirectory(archivePath, root.Directory);
            }
        }

        if (Directory.Exists(archivePath))
        {
            throw ThrowHelper.ArchivePathIsDirectory(archivePath);
        }

        if (File.Exists(archivePath) && !overwrite)
        {
            throw ThrowHelper.ArchiveAlreadyExists(archivePath);
        }
    }

    /// <summary>
    /// Writes a consistent copy of every existing workspace database into the snapshot
    /// directory, so the live file and its write-ahead log are never copied directly.
    /// </summary>
    private static async Task<List<DatabaseSnapshot>> SnapshotDatabasesAsync(
        List<ArchiveRoot> roots,
        string snapshotDirectory,
        CancellationToken cancellationToken)
    {
        var snapshots = new List<DatabaseSnapshot>();

        foreach (var root in roots)
        {
            var databasePath = AgentWorkspace.GetDatabasePath(root.WorkspaceDirectory);

            if (!File.Exists(databasePath))
            {
                continue;
            }

            var snapshotPath = Path.Combine(snapshotDirectory, $"{snapshots.Count}.db");

            await using var connection = new SqliteConnection(
                new SqliteConnectionStringBuilder
                {
                    DataSource = databasePath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ToString());

            await connection.OpenAsync(cancellationToken);

            await using (var snapshotCommand = connection.CreateCommand())
            {
                snapshotCommand.CommandText = "VACUUM INTO $path;";
                snapshotCommand.Parameters.AddWithValue("$path", snapshotPath);
                await snapshotCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            long version;

            await using (var versionCommand = connection.CreateCommand())
            {
                versionCommand.CommandText = "PRAGMA user_version;";
                version = (long)(await versionCommand.ExecuteScalarAsync(cancellationToken))!;
            }

            snapshots.Add(new DatabaseSnapshot(databasePath, snapshotPath, version));
        }

        return snapshots;
    }

    private static async Task<int> AddRootAsync(
        ZipArchive archive,
        ArchiveRoot root,
        List<DatabaseSnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        var excludedFiles = new HashSet<string>(PathComparer)
        {
            Path.Combine(root.WorkspaceDirectory, DatabaseWalFileName),
            Path.Combine(root.WorkspaceDirectory, DatabaseSharedMemoryFileName)
        };

        var excludedDirectory = AgentWorkspace.GetMemoryLocalDirectory(
            AgentWorkspace.GetMemoryDirectory(root.WorkspaceDirectory));

        var files = new FileSystemEnumerable<string>(
            root.Directory,
            (ref FileSystemEntry entry) => entry.ToFullPath(),
            new EnumerationOptions
            {
                RecurseSubdirectories = true,
                AttributesToSkip = FileAttributes.None,
                IgnoreInaccessible = false
            })
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry)
                => !entry.IsDirectory
                    && !entry.Attributes.HasFlag(FileAttributes.ReparsePoint)
                    && !excludedFiles.Contains(entry.ToFullPath()),
            ShouldRecursePredicate = (ref FileSystemEntry entry)
                => !entry.Attributes.HasFlag(FileAttributes.ReparsePoint)
                    && !PathComparer.Equals(entry.ToFullPath(), excludedDirectory)
        };

        var count = 0;

        foreach (var path in files.Order(PathComparer))
        {
            var sourcePath = snapshots.Find(snapshot => PathComparer.Equals(snapshot.DatabasePath, path))
                ?.SnapshotPath ?? path;

            FileStream source;

            try
            {
                source = new FileStream(
                    sourcePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 81920,
                    useAsync: true);
            }
            catch (FileNotFoundException)
            {
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }

            await using (source)
            {
                var relativePath = Path.GetRelativePath(root.Directory, path).Replace('\\', '/');
                var entry = archive.CreateEntry(
                    root.EntryName + "/" + relativePath, CompressionLevel.Optimal);
                entry.LastWriteTime = ToEntryTime(File.GetLastWriteTime(path));

                await using var entryStream = entry.Open();
                await source.CopyToAsync(entryStream, cancellationToken);
            }

            count++;
        }

        return count;
    }

    /// <summary>
    /// Converts a local file write time into a zip entry time, which stores a wall-clock time
    /// without a zone and only covers the years 1980 through 2107. Times outside that range
    /// become the nearest valid time.
    /// </summary>
    private static DateTimeOffset ToEntryTime(DateTime localWriteTime)
    {
        var wallClock = DateTime.SpecifyKind(localWriteTime, DateTimeKind.Unspecified);

        if (wallClock < s_minEntryTime)
        {
            wallClock = s_minEntryTime;
        }
        else if (wallClock > s_maxEntryTime)
        {
            wallClock = s_maxEntryTime;
        }

        return new DateTimeOffset(wallClock);
    }

    private static bool IsWithin(string directory, string path)
        => path.StartsWith(
            directory + Path.DirectorySeparatorChar,
            OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

    private static StringComparer PathComparer
        => OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

    private static void DeleteIfExists(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed record ArchiveRoot(string EntryName, string Directory, string WorkspaceDirectory);

    private sealed record DatabaseSnapshot(string DatabasePath, string SnapshotPath, long Version);
}
