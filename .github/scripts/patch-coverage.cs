#!/usr/bin/env dotnet
// patch-coverage.cs — compute patch (changed-line) coverage from a Cobertura report + a unified git diff.
// usage: dotnet patch-coverage.cs -- <cobertura.xml> <diff.txt> [threshold]

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();
var jsonOnly = args.Contains("--json");
if (positional.Length < 2)
{
    Console.Error.WriteLine("usage: dotnet patch-coverage.cs -- <cobertura.xml> <diff.txt> [threshold] [--json]");
    Environment.Exit(1);
}

var coberturaPath = positional[0];
var diffPath = positional[1];
var threshold = 80.0;
if (positional.Length > 2
    && !double.TryParse(positional[2], NumberStyles.Float, CultureInfo.InvariantCulture, out threshold))
{
    Console.Error.WriteLine($"invalid threshold '{positional[2]}' (expected a number)");
    Environment.Exit(1);
}

// Link base (CI provides these; falls back to plain text when unset).
var server = (Environment.GetEnvironmentVariable("GITHUB_SERVER_URL") ?? "https://github.com").TrimEnd('/');
var repo = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
var sha = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "HEAD";
var prNumber = Environment.GetEnvironmentVariable("PR_NUMBER");
// Prefer the PR "Files changed" diff section (anchor = "diff-" + sha256(path)); fall back to the blob at the head sha.
var link = (string repoRel) =>
{
    if (string.IsNullOrEmpty(repo))
    {
        return null;
    }

    return string.IsNullOrEmpty(prNumber)
        ? $"{server}/{repo}/blob/{sha}/{repoRel}"
        : $"{server}/{repo}/pull/{prNumber}/files#diff-{Sha256Hex(repoRel)}";
};

// 1. Parse the diff: repo-relative path -> set of added/modified line numbers (new-file side).
var changed = ParseDiff(File.ReadAllLines(diffPath));

// 2. Parse cobertura: absolute path -> (line -> hits). Merge partial-class duplicates by max hits.
var coverage = ParseCobertura(coberturaPath);

// Whole-project line coverage (every instrumented file in the report).
var projValid = coverage.Sum(f => f.Value.Count);
var projCovered = coverage.Sum(f => f.Value.Values.Count(h => h > 0));
var projPct = projValid == 0 ? 100.0 : 100.0 * projCovered / projValid;

// 3. Intersect per file. A changed line counts only if it appears in coverage (executable).
var rows = new List<FileRow>();
foreach (var (path, lines) in changed.OrderBy(x => x.Key, StringComparer.Ordinal))
{
    if (IsTestPath(path))
    {
        continue; // patch coverage measures production code, not test/benchmark sources
    }

    var hits = Match(coverage, path);
    if (hits is null)
    {
        rows.Add(new FileRow(path, 0, 0, [], NotInstrumented: true));
        continue;
    }

    var covered = 0;
    var missed = new List<int>();
    foreach (var line in lines)
    {
        if (!hits.TryGetValue(line, out var h))
        {
            continue; // non-executable changed line (brace, comment, blank) -> excluded
        }

        if (h > 0)
        {
            covered++;
        }
        else
        {
            missed.Add(line);
        }
    }

    var coverable = covered + missed.Count;
    if (coverable == 0 && hits.Keys.Count == 0)
    {
        rows.Add(new FileRow(path, 0, 0, [], NotInstrumented: true));
        continue;
    }

    rows.Add(new FileRow(path, covered, coverable, missed, NotInstrumented: false));
}

// 4. Render markdown — worst patch coverage first.
rows = rows
    .OrderBy(r => r.NotInstrumented ? 1 : 0)
    .ThenBy(r => r.Coverable == 0 ? 1 : 0)
    .ThenBy(r => r.Coverable == 0 ? 100.0 : 100.0 * r.Covered / r.Coverable)
    .ThenBy(r => r.Path, StringComparer.Ordinal)
    .ToList();

// Uncovered changed lines as JSON (the AI feed). Repo-relative paths + line ranges + sha → grep locally.
var withMisses = rows.Where(r => r.Missed.Count > 0).ToList();
var json = BuildJson(sha, withMisses);
if (jsonOnly)
{
    Console.WriteLine(json);
    return;
}

var totalCovered = rows.Sum(r => r.Covered);
var totalCoverable = rows.Sum(r => r.Coverable);
if (totalCoverable == 0)
{
    // No executable lines changed (only tests, comments, generated code, or non-.cs files); emit nothing so CI skips the comment.
    return;
}

var pct = 100.0 * totalCovered / totalCoverable;

var sb = new StringBuilder();
sb.AppendLine("## Patch coverage");
sb.AppendLine();
sb.AppendLine($"**{pct.ToString("0.0", CultureInfo.InvariantCulture)}%** of changed lines covered "
    + $"({totalCovered}/{totalCoverable})");
sb.AppendLine();
// Only files with coverable changed lines; drop non-instrumented (tests, interfaces, generated) and zero-coverable noise.
const int maxRows = 20;
var tableRows = rows.Where(r => !r.NotInstrumented && r.Coverable > 0).ToList();

sb.AppendLine("| File | Covered | Changed | Patch % |");
sb.AppendLine("|:-----|--------:|--------:|--------:|");
foreach (var r in tableRows.Take(maxRows))
{
    var u = link(r.Path);
    var name = u is null ? Short(r.Path) : $"[{Short(r.Path)}]({u})";
    var filePercent = 100.0 * r.Covered / r.Coverable;
    var mark = r.Covered == r.Coverable ? "🟢" : (filePercent >= threshold ? "🟡" : "🔴");
    sb.AppendLine($"| {name} | {r.Covered} | {r.Coverable} | {filePercent.ToString("0.0", CultureInfo.InvariantCulture)}%&nbsp;{mark} |");
}

if (tableRows.Count > maxRows)
{
    sb.AppendLine();
    sb.AppendLine($"_+{tableRows.Count - maxRows} more changed files; see the JSON below for uncovered lines._");
}

if (withMisses.Count > 0)
{
    sb.AppendLine();
    sb.AppendLine("<details><summary>Uncovered changed lines (JSON)</summary>");
    sb.AppendLine();
    sb.AppendLine("```json");
    sb.AppendLine(json);
    sb.AppendLine("```");
    sb.AppendLine();
    sb.AppendLine("</details>");
}

sb.AppendLine();
sb.AppendLine("---");
sb.AppendLine();
sb.AppendLine($"Project coverage: **{projPct.ToString("0.0", CultureInfo.InvariantCulture)}%** "
    + $"({projCovered}/{projValid} lines)");

Console.WriteLine(sb.ToString());

// --- helpers ---

static Dictionary<string, SortedSet<int>> ParseDiff(string[] diffLines)
{
    var result = new Dictionary<string, SortedSet<int>>();
    string? current = null;
    var newLine = 0;
    foreach (var raw in diffLines)
    {
        if (raw.StartsWith("+++ ", StringComparison.Ordinal))
        {
            var p = raw[4..].Trim();
            if (p.StartsWith("b/", StringComparison.Ordinal))
            {
                p = p[2..];
            }

            current = p == "/dev/null" ? null : p;
            if (current is not null && !result.ContainsKey(current))
            {
                result[current] = [];
            }

            continue;
        }

        if (raw.StartsWith("@@", StringComparison.Ordinal))
        {
            // @@ -a,b +c,d @@
            var plus = raw.IndexOf('+');
            var span = raw[(plus + 1)..raw.IndexOf("@@", plus, StringComparison.Ordinal)].Trim();
            var comma = span.IndexOf(',');
            newLine = int.Parse(comma < 0 ? span : span[..comma], CultureInfo.InvariantCulture);
            continue;
        }

        if (current is null || raw.Length == 0)
        {
            continue;
        }

        switch (raw[0])
        {
            case '+':
                result[current].Add(newLine);
                newLine++;
                break;
            case ' ':
                newLine++;
                break;
            case '-':
                break; // deletion: no advance on new side
            default:
                break; // "\ No newline at end of file" etc.
        }
    }

    return result;
}

static Dictionary<string, Dictionary<int, int>> ParseCobertura(string path)
{
    var doc = XDocument.Load(path);
    var map = new Dictionary<string, Dictionary<int, int>>(StringComparer.Ordinal);
    foreach (var cls in doc.Descendants("class"))
    {
        var file = (string?)cls.Attribute("filename");
        if (file is null)
        {
            continue;
        }

        file = file.Replace('\\', '/');
        if (!map.TryGetValue(file, out var lines))
        {
            lines = [];
            map[file] = lines;
        }

        foreach (var line in cls.Descendants("line"))
        {
            var n = (int?)line.Attribute("number");
            var h = (int?)line.Attribute("hits");
            if (n is null || h is null)
            {
                continue;
            }

            lines[n.Value] = lines.TryGetValue(n.Value, out var prev) ? Math.Max(prev, h.Value) : h.Value;
        }
    }

    return map;
}

static Dictionary<int, int>? Match(Dictionary<string, Dictionary<int, int>> coverage, string repoRelPath)
{
    var needle = "/" + repoRelPath.Replace('\\', '/');
    Dictionary<int, int>? found = null;
    foreach (var (abs, lines) in coverage)
    {
        if (abs.EndsWith(needle, StringComparison.Ordinal) || abs == repoRelPath)
        {
            found ??= [];
            foreach (var (n, h) in lines)
            {
                found[n] = found.TryGetValue(n, out var prev) ? Math.Max(prev, h) : h;
            }
        }
    }

    return found;
}

static string Short(string path)
{
    const int maxWidth = 60;
    var parts = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0)
    {
        return path;
    }

    // Greedily keep as many full trailing segments as fit; always keep the file name.
    var take = 1;
    var width = parts[^1].Length;
    for (var i = parts.Length - 2; i >= 0; i--)
    {
        var add = parts[i].Length + 1;
        if (width + add > maxWidth)
        {
            break;
        }

        width += add;
        take++;
    }

    var tail = string.Join('/', parts[^take..]);
    return take < parts.Length ? "…/" + tail : tail;
}

static IEnumerable<(int Start, int End)> RangePairs(List<int> lines)
{
    lines.Sort();
    var start = lines[0];
    var prev = lines[0];
    for (var i = 1; i <= lines.Count; i++)
    {
        if (i < lines.Count && lines[i] == prev + 1)
        {
            prev = lines[i];
            continue;
        }

        yield return (start, prev);
        if (i < lines.Count)
        {
            start = prev = lines[i];
        }
    }
}

static string BuildJson(string sha, List<FileRow> withMisses)
{
    var b = new StringBuilder();
    b.AppendLine("{");
    b.AppendLine($"  \"sha\": {JsonStr(sha)},");
    b.AppendLine("  \"files\": [");
    for (var i = 0; i < withMisses.Count; i++)
    {
        var pairs = string.Join(", ", RangePairs(withMisses[i].Missed).Select(p => $"[{p.Start}, {p.End}]"));
        var comma = i == withMisses.Count - 1 ? "" : ",";
        b.AppendLine($"    {{ \"path\": {JsonStr(withMisses[i].Path)}, \"ranges\": [{pairs}] }}{comma}");
    }

    b.AppendLine("  ]");
    b.Append('}');
    return b.ToString();
}

static bool IsTestPath(string path)
{
    var p = "/" + path.Replace('\\', '/') + "/";
    return p.Contains("/test/", StringComparison.OrdinalIgnoreCase)
        || p.Contains("/tests/", StringComparison.OrdinalIgnoreCase)
        || p.Contains("/benchmark/", StringComparison.OrdinalIgnoreCase)
        || p.Contains("/benchmarks/", StringComparison.OrdinalIgnoreCase);
}

static string Sha256Hex(string value)
{
    var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(value));
    return Convert.ToHexStringLower(bytes);
}

// Quoted, escaped JSON string literal (handles quotes, backslashes, and control characters in paths).
static string JsonStr(string value) => $"\"{JsonEncodedText.Encode(value)}\"";

internal record FileRow(string Path, int Covered, int Coverable, List<int> Missed, bool NotInstrumented);
