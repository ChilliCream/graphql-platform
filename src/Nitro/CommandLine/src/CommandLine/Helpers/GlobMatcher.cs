using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace ChilliCream.Nitro.CommandLine;

internal static class GlobMatcher
{
    public static IEnumerable<string> Match(IEnumerable<string> patterns)    {
        var results = new List<string>();
        var relativePatterns = new List<string>();

        foreach (var pattern in patterns)
        {
            if (Path.IsPathRooted(pattern))
            {
                var (basePath, globPattern) = SplitAbsolutePattern(pattern);

                if (Directory.Exists(basePath))
                {
                    var matcher = new Matcher();
                    matcher.AddInclude(globPattern);
                    var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(basePath)));

                    results.AddRange(result.Files.Select(f => Path.Combine(basePath, f.Path)));
                }
            }
            else
            {
                relativePatterns.Add(pattern);
            }
        }

        if (relativePatterns.Count > 0)
        {
            var matcher = new Matcher();
            matcher.AddIncludePatterns(relativePatterns);
            var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(Directory.GetCurrentDirectory())));

            results.AddRange(result.Files.Select(f => Path.GetFullPath(f.Path)));
        }

        return results.Distinct().OrderBy(f => f, StringComparer.Ordinal);
    }

    private static (string basePath, string pattern) SplitAbsolutePattern(string pattern)
    {
        var segments = pattern.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
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
        var globPattern = string.Join('/', globSegments); // Matcher expects forward slashes

        if (string.IsNullOrEmpty(fixedSegments[0]) && fixedSegments.Count == 1)
        {
            basePath = "/";
        }

        return (basePath, globPattern);
    }
}
