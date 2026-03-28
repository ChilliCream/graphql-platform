using System.Text;
using System.Text.RegularExpressions;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Tests;

// TODO: Remove
internal sealed class TestFileSystem : IFileSystem
{
    private readonly Dictionary<string, byte[]> _files;
    private readonly HashSet<string> _directories;
    private readonly StringComparer _pathComparer;
    private readonly StringComparison _pathComparison;
    private readonly RegexOptions _regexOptions;
    private readonly string _currentDirectory;

    public TestFileSystem(params KeyValuePair<string, string>[] seededFiles)
    {
        _pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        _pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        _regexOptions = OperatingSystem.IsWindows()
            ? RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
            : RegexOptions.CultureInvariant;

        _files = new Dictionary<string, byte[]>(_pathComparer);
        _directories = new HashSet<string>(_pathComparer);
        _currentDirectory = Path.GetFullPath(Directory.GetCurrentDirectory());

        CreateDirectory(_currentDirectory);

        foreach (var (path, content) in seededFiles)
        {
            WriteBytes(path, Encoding.UTF8.GetBytes(content));
        }
    }

    public IReadOnlyDictionary<string, byte[]> Files => _files;

    public bool FileExists(string path) => _files.ContainsKey(Normalize(path));

    public Stream OpenReadStream(string path)
    {
        var normalized = Normalize(path);
        if (_files.TryGetValue(normalized, out var content))
        {
            return new MemoryStream(content.ToArray(), writable: false);
        }

        throw new ExitException($"[red] File {path} was not found![/]");
    }

    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct)
    {
        var normalized = Normalize(path);
        if (_files.TryGetValue(normalized, out var content))
        {
            return Task.FromResult(content.ToArray());
        }

        throw new FileNotFoundException($"Could not find file '{path}'.", path);
    }

    public Task<string> ReadAllTextAsync(string path, CancellationToken ct)
    {
        var normalized = Normalize(path);
        if (_files.TryGetValue(normalized, out var content))
        {
            return Task.FromResult(Encoding.UTF8.GetString(content));
        }

        throw new FileNotFoundException($"Could not find file '{path}'.", path);
    }

    public Stream CreateFile(string path) => CreateWriteStream(path);

    public Task WriteAllTextAsync(string path, string content, CancellationToken ct)
    {
        WriteBytes(path, Encoding.UTF8.GetBytes(content));
        return Task.CompletedTask;
    }

    public void DeleteFile(string path)
        => _files.Remove(Normalize(path));

    public bool DirectoryExists(string path)
    {
        var normalized = Normalize(path);
        if (_directories.Contains(normalized))
        {
            return true;
        }

        var prefix = EnsureTrailingSeparator(normalized);
        return _files.Keys.Any(p => p.StartsWith(prefix, _pathComparison));
    }

    public void CreateDirectory(string path)
    {
        var directory = Normalize(path);
        while (true)
        {
            _directories.Add(directory);
            var parent = Path.GetDirectoryName(directory);
            if (string.IsNullOrEmpty(parent) || _pathComparer.Equals(parent, directory))
            {
                break;
            }

            directory = parent;
        }
    }

    public string GetCurrentDirectory() => _currentDirectory;

    public IEnumerable<string> GetFiles(
        string directory,
        string pattern,
        SearchOption searchOption)
    {
        var normalizedDirectory = Normalize(directory);
        var regex = new Regex(
            "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
            _regexOptions);

        return _files.Keys
            .Where(path => IsInDirectory(path, normalizedDirectory, searchOption))
            .Where(path => regex.IsMatch(Path.GetFileName(path)))
            .OrderBy(path => path, _pathComparer);
    }

    public IEnumerable<string> GlobMatch(
        IEnumerable<string> patterns,
        IEnumerable<string>? excludes = null)
    {
        var includePatterns = patterns.Select(CreateGlobRegex).ToArray();
        var excludePatterns = (excludes ?? []).Select(CreateGlobRegex).ToArray();

        return _files.Keys
            .Where(path =>
                includePatterns.Any(pattern => pattern.IsMatch(path))
                && !excludePatterns.Any(pattern => pattern.IsMatch(path)))
            .Distinct(_pathComparer)
            .OrderBy(path => path, _pathComparer);
    }

    private Stream CreateWriteStream(string path)
    {
        var normalized = Normalize(path);
        EnsureParentDirectory(normalized);

        return new CaptureWriteStream(bytes => _files[normalized] = bytes);
    }

    private void WriteBytes(string path, byte[] content)
    {
        var normalized = Normalize(path);
        EnsureParentDirectory(normalized);
        _files[normalized] = content;
    }

    private void EnsureParentDirectory(string path)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent))
        {
            CreateDirectory(parent);
        }
    }

    private string Normalize(string path)
        => Path.GetFullPath(path, _currentDirectory);

    private bool IsInDirectory(
        string filePath,
        string directory,
        SearchOption searchOption)
    {
        var fileDirectory = Path.GetDirectoryName(filePath);
        if (_pathComparer.Equals(fileDirectory, directory))
        {
            return true;
        }

        if (searchOption != SearchOption.AllDirectories)
        {
            return false;
        }

        return filePath.StartsWith(EnsureTrailingSeparator(directory), _pathComparison);
    }

    private static string EnsureTrailingSeparator(string path)
        => path.EndsWith(Path.DirectorySeparatorChar)
            || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;

    private Regex CreateGlobRegex(string pattern)
    {
        const string doubleStarToken = "__DOUBLE_STAR__";
        var escaped = Regex.Escape(pattern.Replace('\\', '/'))
            .Replace(@"\*\*", doubleStarToken)
            .Replace(@"\*", "[^/]*")
            .Replace(@"\?", ".")
            .Replace(doubleStarToken, ".*");

        return new Regex($"^{escaped}$", _regexOptions);
    }

    private sealed class CaptureWriteStream(Action<byte[]> onDispose) : MemoryStream
    {
        private bool _committed;

        protected override void Dispose(bool disposing)
        {
            Commit();
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            Commit();
            await base.DisposeAsync();
        }

        private void Commit()
        {
            if (_committed)
            {
                return;
            }

            _committed = true;
            onDispose(ToArray());
        }
    }
}
