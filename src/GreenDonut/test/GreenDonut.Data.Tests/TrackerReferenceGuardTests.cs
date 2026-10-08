namespace GreenDonut.Data;

public class TrackerReferenceGuardTests
{
    [Fact]
    public void SourceTree_Should_NotContainTrackerIdsOrAiAttribution()
    {
        // arrange
        var root = FindGreenDonutRoot();
        var patterns = BuildForbiddenPatterns();

        // act
        var violations = FindViolations(root, patterns);

        // assert
        var message = "Found forbidden tracker or attribution references:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations);
        Assert.True(violations.Count == 0, message);
    }

    private static List<string> FindViolations(string root, string[] patterns)
    {
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (IsExcluded(file))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                foreach (var pattern in patterns)
                {
                    if (lines[i].Contains(pattern, StringComparison.Ordinal))
                    {
                        violations.Add($"{Path.GetRelativePath(root, file)}:{i + 1}: contains \"{pattern}\"");
                    }
                }
            }
        }

        return violations;
    }

    private static bool IsExcluded(string file)
    {
        var normalized = file.Replace(Path.DirectorySeparatorChar, '/');
        if (normalized.Contains("/bin/", StringComparison.Ordinal)
            || normalized.Contains("/obj/", StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(Path.GetFileName(file), "TrackerReferenceGuardTests.cs", StringComparison.Ordinal);
    }

    private static string FindGreenDonutRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GreenDonut.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the GreenDonut source tree from the test assembly location.");
    }

    private static string[] BuildForbiddenPatterns()
    {
        // Returns the forbidden tracker-id prefix and attribution strings.
        var trackerPrefix = string.Concat("hc", "-", "fork", "-", "1", "-");
        var generatedWith = string.Concat("Generated", " ", "with", " ", "Claude");
        var coAuthoredBy = string.Concat("Co", "-", "Authored", "-", "By", ":", " ", "Claude");
        var sessionMarker = string.Concat("Claude", "-", "Session");

        return [trackerPrefix, generatedWith, coAuthoredBy, sessionMarker];
    }
}
