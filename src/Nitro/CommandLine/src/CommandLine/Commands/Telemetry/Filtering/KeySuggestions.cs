using System.Collections.Frozen;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;

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

    public static string? CreateHint(
        FilterNode? filter,
        IReadOnlyList<AttributeKeyRow> attributeKeys,
        OpenTelemetrySignalKind signal)
    {
        ArgumentNullException.ThrowIfNull(attributeKeys);

        var suggestions = filter.GetFields()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(FilterCompiler.SplitScope)
            .Where(static field => !s_virtualFields.Contains(field.Key))
            .Select(field => (field.Key, KnownKeys: attributeKeys.GetPaths(field.Kind)))
            .Where(static field => !field.KnownKeys.Contains(field.Key, StringComparer.OrdinalIgnoreCase))
            .Select(static field => (field.Key, Candidates: field.Key.FindCandidates(field.KnownKeys)))
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
}

file static class Extensions
{
    extension(FilterNode? node)
    {
        public IEnumerable<string> GetFields()
        {
            switch (node)
            {
                case FilterAndNode and:
                    foreach (var child in and.Children)
                    {
                        foreach (var field in child.GetFields())
                        {
                            yield return field;
                        }
                    }

                    break;

                case FilterOrNode or:
                    foreach (var child in or.Children)
                    {
                        foreach (var field in child.GetFields())
                        {
                            yield return field;
                        }
                    }

                    break;

                case FilterNotNode not:
                    foreach (var field in not.Child.GetFields())
                    {
                        yield return field;
                    }

                    break;

                case FilterPredicateNode predicate:
                    yield return predicate.Field;
                    break;
            }
        }
    }

    extension(IReadOnlyList<AttributeKeyRow> attributeKeys)
    {
        public string[] GetPaths(OpenTelemetryAttributeKind? kind)
            => attributeKeys
                .Where(key => kind is null
                    || string.Equals(key.Kind, kind.Value.ToString(), StringComparison.OrdinalIgnoreCase))
                .Select(static key => key.Path)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }

    extension(string value)
    {
        public string[] FindCandidates(IReadOnlyList<string> knownKeys)
        {
            var tiers = new List<KeyCandidate>[]
            {
                [],
                [],
                [],
                []
            };
            var unknownParts = PathParts.Parse(value);

            foreach (var candidate in knownKeys)
            {
                var candidateParts = PathParts.Parse(candidate);

                if (candidate.StartsWith(value, StringComparison.OrdinalIgnoreCase))
                {
                    tiers[0].Add(new KeyCandidate(
                        candidate,
                        candidate.Length - value.Length,
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
                    var distance = value.LevenshteinDistance(candidate);
                    if (distance <= 3)
                    {
                        tiers[2].Add(new KeyCandidate(candidate, 0, candidateParts.SegmentCount, distance));
                    }
                    else if (unknownParts.IsSameRoot(candidateParts)
                        && (distance = unknownParts.Leaf.LevenshteinDistance(candidateParts.Leaf)) <= 3)
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

        public int LevenshteinDistance(string right)
        {
            var left = value;
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
    }
}
