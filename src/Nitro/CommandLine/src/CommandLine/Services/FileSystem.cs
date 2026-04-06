using ChilliCream.Nitro.CommandLine.Helpers;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace ChilliCream.Nitro.CommandLine.Services;

internal sealed class FileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public Stream OpenReadStream(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Could not open file", path);
        }

        return File.OpenRead(path);
    }

    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct)
        => File.ReadAllBytesAsync(path, ct);

    public Task<string> ReadAllTextAsync(string path, CancellationToken ct)
        => File.ReadAllTextAsync(path, ct);

    public Stream CreateFile(string path) => File.Create(path);

    public Task WriteAllTextAsync(string path, string content, CancellationToken ct)
        => File.WriteAllTextAsync(path, content, ct);

    public void DeleteFile(string path) => File.Delete(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public string GetCurrentDirectory() => Directory.GetCurrentDirectory();

    public IEnumerable<string> GetFiles(
        string directory,
        string pattern,
        SearchOption searchOption)
        => new DirectoryInfo(directory)
            .GetFiles(pattern, searchOption)
            .Select(f => f.FullName);

    public IEnumerable<string> GlobMatch(
        IEnumerable<string> patterns,
        IEnumerable<string>? excludes = null)
    {
        var results = new List<string>();
        var relativePatterns = new List<string>();
        var excludeList = excludes?.ToList();

        foreach (var pattern in patterns)
        {
            if (Path.IsPathRooted(pattern))
            {
                var (basePath, globPattern) = SplitAbsolutePattern(pattern);

                if (Directory.Exists(basePath))
                {
                    var matcher = new Matcher();
                    matcher.AddInclude(globPattern);

                    if (excludeList is { Count: > 0 })
                    {
                        matcher.AddExcludePatterns(excludeList);
                    }

                    var result = matcher.Execute(
                        new DirectoryInfoWrapper(new DirectoryInfo(basePath)));

                    results.AddRange(
                        result.Files.Select(f => Path.Combine(basePath, f.Path)));
                }
            }
            else
            {
                relativePatterns.Add(pattern);
            }
        }

        if (relativePatterns.Count > 0)
        {
            var cwd = GetCurrentDirectory();
            var matcher = new Matcher();
            matcher.AddIncludePatterns(relativePatterns);

            if (excludeList is { Count: > 0 })
            {
                matcher.AddExcludePatterns(excludeList);
            }

            var result = matcher.Execute(
                new DirectoryInfoWrapper(
                    new DirectoryInfo(cwd)));

            results.AddRange(result.Files.Select(f => Path.Combine(cwd, f.Path)));
        }

        return results.Distinct().OrderBy(f => f, StringComparer.Ordinal);
    }

    private static (string basePath, string pattern) SplitAbsolutePattern(
        string pattern)
    {
        var segments = pattern.Split(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
        var fixedSegments = new List<string>();
        var globSegments = new List<string>();
        var foundWildcard = false;

        foreach (var segment in segments)
        {
            if (!foundWildcard && !segment.Contains('*') && !segment.Contains('?'))
            {
                fixedSegments.Add(segment);
            }
            else
            {
                foundWildcard = true;
                globSegments.Add(segment);
            }
        }

        var basePath = string.Join(Path.DirectorySeparatorChar, fixedSegments);
        var globPattern = string.Join('/', globSegments);

        if (string.IsNullOrEmpty(fixedSegments[0]) && fixedSegments.Count == 1)
        {
            basePath = "/";
        }

        return (basePath, globPattern);
    }
}
