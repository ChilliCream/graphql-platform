using System.Buffers.Binary;
using System.IO.Compression;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal static partial class AgentWorkspaceArchive
{
    private const int MaxManifestLength = 1024 * 1024;

    private const int DatabaseHeaderLength = 100;
    private const int DatabaseUserVersionOffset = 60;
    private static readonly byte[] s_databaseMagic = "SQLite format 3\0"u8.ToArray();

    private static readonly string s_projectDatabaseEntryName =
        $"{ProjectRootEntryName}/{AgentWorkspace.AgentsDirectoryName}/{AgentWorkspace.DatabaseFileName}";

    private static readonly string s_gitDatabaseEntryName =
        $"{GitRootEntryName}/{AgentWorkspace.DatabaseFileName}";

    /// <summary>
    /// Checks that the archive can be restored into the given directories without modifying
    /// anything: a known format version, no archive inside a directory that restore deletes, only
    /// entries under the manifest roots without absolute or <c>..</c> paths, and no
    /// <c>agents.db</c> (matched case-insensitively) with a schema newer than
    /// <see cref="AgentDatabase.CurrentVersion"/>.
    /// </summary>
    public static AgentWorkspaceArchiveManifest Validate(
        string archivePath,
        string projectDirectory,
        string? gitWorkspaceDirectory)
    {
        archivePath = Path.GetFullPath(archivePath);

        using var archive = OpenArchive(archivePath);

        return Validate(archive, archivePath, projectDirectory, gitWorkspaceDirectory);
    }

    /// <summary>
    /// Replaces the project <c>.nitro</c> directory and, when <paramref name="gitWorkspaceDirectory"/>
    /// is not null, the git workspace directory with the archive contents, deleting a directory the
    /// archive has no root for. Any failure before the swap leaves the existing directories untouched.
    /// </summary>
    public static async Task<AgentWorkspaceRestoreSummary> RestoreAsync(
        string archivePath,
        string projectDirectory,
        string? gitWorkspaceDirectory,
        CancellationToken cancellationToken)
    {
        archivePath = Path.GetFullPath(archivePath);

        // ZipArchive implements IAsyncDisposable only on .NET 10 and later.
#pragma warning disable RCS1261
        using var archive = OpenArchive(archivePath);
#pragma warning restore RCS1261

        var manifest = Validate(archive, archivePath, projectDirectory, gitWorkspaceDirectory);

        var suffix = Guid.NewGuid().ToString("N");
        var targets = new List<RestoreTarget>
        {
            CreateTarget(ProjectRootEntryName, projectDirectory, suffix)
        };

        if (gitWorkspaceDirectory is not null)
        {
            targets.Add(CreateTarget(GitRootEntryName, gitWorkspaceDirectory, suffix));
        }

        try
        {
            var fileCount = 0;

            foreach (var target in targets.Where(target => manifest.Roots.Contains(target.EntryName)))
            {
                fileCount += await ExtractRootAsync(archive, target, cancellationToken);
            }

            var leftoverDirectories = SwapIntoPlace(targets, manifest.Roots);

            return new AgentWorkspaceRestoreSummary(
                archivePath, manifest.Roots, fileCount, leftoverDirectories);
        }
        finally
        {
            foreach (var target in targets)
            {
                DeleteDirectoryIfExists(target.StagingDirectory);
            }
        }
    }

    private static ZipArchive OpenArchive(string archivePath)
    {
        if (!File.Exists(archivePath))
        {
            throw ThrowHelper.ArchiveNotFound(archivePath);
        }

        try
        {
            return ZipFile.OpenRead(archivePath);
        }
        catch (InvalidDataException)
        {
            throw ThrowHelper.ArchiveNotReadable(archivePath);
        }
    }

    private static AgentWorkspaceArchiveManifest Validate(
        ZipArchive archive,
        string archivePath,
        string projectDirectory,
        string? gitWorkspaceDirectory)
    {
        var manifest = ReadManifest(archive, archivePath);

        foreach (var directory in new[] { projectDirectory, gitWorkspaceDirectory }.OfType<string>())
        {
            var fullDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));

            if (IsWithin(fullDirectory, archivePath))
            {
                throw ThrowHelper.ArchiveInsideRestoredDirectory(archivePath, fullDirectory);
            }
        }

        foreach (var root in manifest.Roots)
        {
            if (root is not (ProjectRootEntryName or GitRootEntryName))
            {
                throw ThrowHelper.ArchiveManifestInvalid(archivePath);
            }
        }

        if (manifest.Roots.Count == 0 || manifest.Roots.Distinct().Count() != manifest.Roots.Count)
        {
            throw ThrowHelper.ArchiveManifestInvalid(archivePath);
        }

        if (manifest.Roots.Contains(GitRootEntryName) && gitWorkspaceDirectory is null)
        {
            throw ThrowHelper.ArchiveGitRootWithoutRepository(archivePath);
        }

        var names = new HashSet<string>(PathComparer);

        foreach (var entry in archive.Entries)
        {
            if (entry.FullName == ManifestEntryName)
            {
                continue;
            }

            if (!IsEntryNameAllowed(entry.FullName, manifest.Roots) || !names.Add(entry.FullName))
            {
                throw ThrowHelper.ArchiveEntryInvalid(entry.FullName);
            }

            if (string.Equals(entry.FullName, s_projectDatabaseEntryName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(entry.FullName, s_gitDatabaseEntryName, StringComparison.OrdinalIgnoreCase))
            {
                ValidateDatabase(entry);
            }
        }

        return manifest;
    }

    private static AgentWorkspaceArchiveManifest ReadManifest(ZipArchive archive, string archivePath)
    {
        var entry = archive.GetEntry(ManifestEntryName)
            ?? throw ThrowHelper.ArchiveManifestMissing(archivePath);

        if (entry.Length > MaxManifestLength)
        {
            throw ThrowHelper.ArchiveManifestInvalid(archivePath);
        }

        try
        {
            using var stream = entry.Open();
            using var document = JsonDocument.Parse(stream);

            // Reads the format version alone first, so a newer format is reported as unsupported.
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("formatVersion", out var formatVersion)
                || !formatVersion.TryGetInt64(out var version))
            {
                throw ThrowHelper.ArchiveManifestInvalid(archivePath);
            }

            if (version != FormatVersion)
            {
                throw ThrowHelper.ArchiveFormatUnsupported(version, FormatVersion);
            }

            var manifest = document.RootElement.Deserialize(
                JsonSourceGenerationContext.Default.AgentWorkspaceArchiveManifest);

            return manifest is { Roots: not null }
                ? manifest
                : throw ThrowHelper.ArchiveManifestInvalid(archivePath);
        }
        catch (JsonException)
        {
            throw ThrowHelper.ArchiveManifestInvalid(archivePath);
        }
        catch (InvalidDataException)
        {
            throw ThrowHelper.ArchiveNotReadable(archivePath);
        }
    }

    /// <summary>
    /// True for a relative, forward-slash path with no empty, <c>.</c> or <c>..</c> segment that
    /// lies under one of the listed roots.
    /// </summary>
    private static bool IsEntryNameAllowed(string name, IReadOnlyList<string> roots)
    {
        if (name.Length == 0
            || name.Contains('\\')
            || name.Contains('\0')
            || Path.IsPathRooted(name))
        {
            return false;
        }

        var isDirectory = name.EndsWith('/');
        var segments = (isDirectory ? name[..^1] : name).Split('/');

        if (segments.Any(segment => segment.Length == 0 || segment is "." or ".."))
        {
            return false;
        }

        return roots.Any(root => name.StartsWith(root + "/", StringComparison.Ordinal));
    }

    /// <summary>
    /// Throws when the archived <c>agents.db</c> is not a SQLite database or its
    /// <c>user_version</c> is newer than <see cref="AgentDatabase.CurrentVersion"/>.
    /// </summary>
    private static void ValidateDatabase(ZipArchiveEntry entry)
    {
        Span<byte> header = stackalloc byte[DatabaseHeaderLength];

        using (var stream = entry.Open())
        {
            if (stream.ReadAtLeast(header, DatabaseHeaderLength, throwOnEndOfStream: false) < DatabaseHeaderLength
                || !header[..s_databaseMagic.Length].SequenceEqual(s_databaseMagic))
            {
                throw ThrowHelper.ArchiveDatabaseInvalid(entry.FullName);
            }
        }

        var version = BinaryPrimitives.ReadInt32BigEndian(
            header.Slice(DatabaseUserVersionOffset, sizeof(int)));

        if (version > AgentDatabase.CurrentVersion)
        {
            throw ThrowHelper.ArchiveDatabaseNewer(entry.FullName, version, AgentDatabase.CurrentVersion);
        }
    }

    private static RestoreTarget CreateTarget(string entryName, string directory, string suffix)
    {
        directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));

        var parent = Path.GetDirectoryName(directory)!;
        var name = Path.GetFileName(directory);

        return new RestoreTarget(
            entryName,
            directory,
            Path.Combine(parent, $"{name}-restore-{suffix}"),
            Path.Combine(parent, $"{name}-replaced-{suffix}"));
    }

    private static async Task<int> ExtractRootAsync(
        ZipArchive archive,
        RestoreTarget target,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(target.StagingDirectory);

        var prefix = target.EntryName + "/";
        var count = 0;

        foreach (var entry in archive.Entries.Where(
            entry => entry.FullName.StartsWith(prefix, StringComparison.Ordinal)))
        {
            var relativePath = entry.FullName[prefix.Length..];

            if (relativePath.Length == 0)
            {
                continue;
            }

            var destination = Path.GetFullPath(Path.Combine(target.StagingDirectory, relativePath));

            if (!IsWithin(target.StagingDirectory, destination))
            {
                throw ThrowHelper.ArchiveEntryInvalid(entry.FullName);
            }

            if (entry.FullName.EndsWith('/'))
            {
                Directory.CreateDirectory(destination);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            await using var source = entry.Open();
            await using var file = new FileStream(
                destination,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await source.CopyToAsync(file, cancellationToken);
            count++;
        }

        return count;
    }

    /// <summary>
    /// Replaces the target directories with the staged directories of the restored roots and puts the
    /// originals back if the swap fails. Returns the replaced directories that could not be deleted.
    /// </summary>
    private static List<string> SwapIntoPlace(List<RestoreTarget> targets, IReadOnlyList<string> restoredRoots)
    {
        var replaced = new List<RestoreTarget>();
        var placed = new List<RestoreTarget>();

        try
        {
            foreach (var target in targets.Where(target => Directory.Exists(target.Directory)))
            {
                Directory.Move(target.Directory, target.ReplacedDirectory);
                replaced.Add(target);
            }

            foreach (var target in targets.Where(target => restoredRoots.Contains(target.EntryName)))
            {
                Directory.Move(target.StagingDirectory, target.Directory);
                placed.Add(target);
            }
        }
        catch
        {
            foreach (var target in placed)
            {
                Directory.Move(target.Directory, target.StagingDirectory);
            }

            foreach (var target in replaced)
            {
                Directory.Move(target.ReplacedDirectory, target.Directory);
            }

            throw;
        }

        var leftover = new List<string>();

        foreach (var target in replaced)
        {
            try
            {
                Directory.Delete(target.ReplacedDirectory, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                leftover.Add(target.ReplacedDirectory);
            }
        }

        return leftover;
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed record RestoreTarget(
        string EntryName,
        string Directory,
        string StagingDirectory,
        string ReplacedDirectory);
}
