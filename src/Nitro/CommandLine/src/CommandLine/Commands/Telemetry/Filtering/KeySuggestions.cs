using System.Collections.Frozen;
using System.Collections.Immutable;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal static class KeySuggestions
{
    private static readonly FrozenSet<string> s_virtualFields =
        new[]
        {
            "status",
            "severity",
            "duration",
            "span.name",
            "log.message",
            "trace.id",
            "span.id",
            "span.kind"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly ImmutableArray<string> s_scopePrefixes =
    [
        "@span.",
        "@event.",
        "@resource.",
        "@log.",
        "@body."
    ];

    public static string? CreateHint(
        FilterNode? filter,
        IReadOnlyList<AttributeKeyRow> attributeKeys,
        OpenTelemetrySignalKind signal)
    {
        ArgumentNullException.ThrowIfNull(attributeKeys);

        var knownKeys = attributeKeys
            .Select(static key => key.Path)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var unknownKeys = GetFields(filter)
            .Select(Normalize)
            .Where(static field => !s_virtualFields.Contains(field))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(field => !knownKeys.Contains(field, StringComparer.OrdinalIgnoreCase));
        var suggestions = unknownKeys
            .Select(key => (Key: key, Candidates: FindCandidates(key, knownKeys)))
            .Where(static suggestion => suggestion.Candidates.Length > 0)
            .Select(static suggestion =>
                $"unknown key '{suggestion.Key}', did you mean {string.Join(", ", suggestion.Candidates)}?")
            .ToArray();

        if (suggestions.Length == 0)
        {
            return null;
        }

        return $"no results; {string.Join(" ", suggestions)} "
            + $"Run nitro telemetry attributes keys --signal {signal.ToString().ToLowerInvariant()} to list keys.";
    }

    private static IEnumerable<string> GetFields(FilterNode? node)
    {
        switch (node)
        {
            case FilterAndNode and:
                foreach (var child in and.Children)
                {
                    foreach (var field in GetFields(child))
                    {
                        yield return field;
                    }
                }

                break;

            case FilterOrNode or:
                foreach (var child in or.Children)
                {
                    foreach (var field in GetFields(child))
                    {
                        yield return field;
                    }
                }

                break;

            case FilterNotNode not:
                foreach (var field in GetFields(not.Child))
                {
                    yield return field;
                }

                break;

            case FilterPredicateNode predicate:
                yield return predicate.Field;
                break;
        }
    }

    private static string Normalize(string field)
    {
        foreach (var prefix in s_scopePrefixes)
        {
            if (field.StartsWith(prefix, StringComparison.Ordinal))
            {
                return field[prefix.Length..];
            }
        }

        return field;
    }

    private static string[] FindCandidates(string unknown, IReadOnlyList<string> knownKeys)
    {
        var tiers = new List<KeyCandidate>[]
        {
            [],
            [],
            [],
            []
        };
        var unknownParts = PathParts.Parse(unknown);

        foreach (var candidate in knownKeys)
        {
            var candidateParts = PathParts.Parse(candidate);

            if (candidate.StartsWith(unknown, StringComparison.OrdinalIgnoreCase))
            {
                tiers[0].Add(new KeyCandidate(
                    candidate,
                    candidate.Length - unknown.Length,
                    candidateParts.SegmentCount,
                    Distance: 0));
            }
            else if (unknownParts.IsSameRoot(candidateParts)
                && candidateParts.Leaf.StartsWith(unknownParts.Leaf, StringComparison.OrdinalIgnoreCase))
            {
                tiers[1].Add(new KeyCandidate(
                    candidate,
                    candidateParts.Leaf.Length - unknownParts.Leaf.Length,
                    candidateParts.SegmentCount,
                    Distance: 0));
            }
            else
            {
                var distance = LevenshteinDistance(unknown, candidate);
                if (distance <= 3)
                {
                    tiers[2].Add(new KeyCandidate(candidate, 0, candidateParts.SegmentCount, distance));
                }
                else if (unknownParts.IsSameRoot(candidateParts)
                    && (distance = LevenshteinDistance(unknownParts.Leaf, candidateParts.Leaf)) <= 3)
                {
                    tiers[3].Add(new KeyCandidate(candidate, 0, candidateParts.SegmentCount, distance));
                }
            }
        }

        return tiers[0]
            .OrderBy(static candidate => candidate.CompletionLength)
            .ThenBy(static candidate => candidate.SegmentCount)
            .ThenBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static candidate => candidate.Path, StringComparer.Ordinal)
            .Concat(tiers[1]
                .OrderBy(static candidate => candidate.CompletionLength)
                .ThenBy(static candidate => candidate.SegmentCount)
                .ThenBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Path, StringComparer.Ordinal))
            .Concat(tiers[2]
                .OrderBy(static candidate => candidate.Distance)
                .ThenBy(static candidate => candidate.SegmentCount)
                .ThenBy(static candidate => candidate.Path.Length)
                .ThenBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Path, StringComparer.Ordinal))
            .Concat(tiers[3]
                .OrderBy(static candidate => candidate.Distance)
                .ThenBy(static candidate => candidate.SegmentCount)
                .ThenBy(static candidate => candidate.Path.Length)
                .ThenBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static candidate => candidate.Path, StringComparer.Ordinal))
            .Select(static candidate => candidate.Path)
            .ToArray();
    }

    private static int LevenshteinDistance(string left, string right)
    {
        if (left.Length > right.Length)
        {
            (left, right) = (right, left);
        }

        var previous = new int[left.Length + 1];
        var current = new int[left.Length + 1];
        for (var i = 0; i <= left.Length; i++)
        {
            previous[i] = i;
        }

        for (var rightIndex = 1; rightIndex <= right.Length; rightIndex++)
        {
            current[0] = rightIndex;
            var rightCharacter = char.ToUpperInvariant(right[rightIndex - 1]);

            for (var leftIndex = 1; leftIndex <= left.Length; leftIndex++)
            {
                var cost = char.ToUpperInvariant(left[leftIndex - 1]) == rightCharacter ? 0 : 1;
                current[leftIndex] = Math.Min(
                    Math.Min(current[leftIndex - 1] + 1, previous[leftIndex] + 1),
                    previous[leftIndex - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[left.Length];
    }

    private readonly record struct KeyCandidate(
        string Path,
        int CompletionLength,
        int SegmentCount,
        int Distance);

    private readonly record struct PathParts(string Root, string Leaf, int SegmentCount, bool IsDotted)
    {
        public static PathParts Parse(string path)
        {
            var firstDot = path.IndexOf('.');
            var lastDot = path.LastIndexOf('.');
            return new PathParts(
                firstDot < 0 ? string.Empty : path[..firstDot],
                lastDot < 0 ? path : path[(lastDot + 1)..],
                path.Count(static character => character == '.') + 1,
                firstDot >= 0);
        }

        public bool IsSameRoot(PathParts other)
            => IsDotted
                && other.IsDotted
                && string.Equals(Root, other.Root, StringComparison.OrdinalIgnoreCase);
    }
}
