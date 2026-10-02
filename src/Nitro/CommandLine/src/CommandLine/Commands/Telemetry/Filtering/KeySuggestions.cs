using System.Collections.Frozen;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal static class KeySuggestions
{
    private static readonly FrozenSet<string> s_virtualFields = new[]
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

        var suggestions = GetFields(filter)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(FilterCompiler.SplitScope)
            .Where(static field => !s_virtualFields.Contains(field.Key))
            .Select(field => (field.Key, KnownKeys: GetPaths(attributeKeys, field.Kind)))
            .Where(static field => !field.KnownKeys.Contains(field.Key, StringComparer.OrdinalIgnoreCase))
            .Select(static field => (field.Key, Candidates: FindCandidates(field.Key, field.KnownKeys)))
            .Where(static suggestion => suggestion.Candidates.Length > 0)
            .Select(static suggestion =>
                $"unknown key '{suggestion.Key}', did you mean {string.Join(", ", suggestion.Candidates)}?"
            )
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

    private static string[] GetPaths(IReadOnlyList<AttributeKeyRow> attributeKeys, OpenTelemetryAttributeKind? kind)
        => attributeKeys
            .Where(key =>
                kind is null || string.Equals(key.Kind, kind.Value.ToString(), StringComparison.OrdinalIgnoreCase)
            )
            .Select(static key => key.Path)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string[] FindCandidates(string unknown, IReadOnlyList<string> knownKeys)
    {
        var tiers = new List<KeyCandidate>[] { [], [], [], [] };
        var unknownParts = PathParts.Parse(unknown);

        foreach (var candidate in knownKeys)
        {
            var candidateParts = PathParts.Parse(candidate);

            if (candidate.StartsWith(unknown, StringComparison.OrdinalIgnoreCase))
            {
                tiers[0]
                    .Add(
                        new KeyCandidate(
                            candidate,
                            candidate.Length - unknown.Length,
                            candidateParts.SegmentCount,
                            Distance: 0));
            }
            else if (unknownParts.IsSameRoot(candidateParts)
                && candidateParts.Leaf.StartsWith(unknownParts.Leaf, StringComparison.OrdinalIgnoreCase))
            {
                tiers[1]
                    .Add(
                        new KeyCandidate(
                            candidate,
                            candidateParts.Leaf.Length - unknownParts.Leaf.Length,
                            candidateParts.SegmentCount,
                            Distance: 0));
            }
            else
            {
                var distance = LevenshteinDistance.Calculate(unknown, candidate);
                if (distance <= 3)
                {
                    tiers[2].Add(new KeyCandidate(candidate, 0, candidateParts.SegmentCount, distance));
                }
                else if (unknownParts.IsSameRoot(candidateParts)
                    && (distance = LevenshteinDistance.Calculate(unknownParts.Leaf, candidateParts.Leaf)) <= 3)
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
            .Concat(
                tiers[1]
                    .OrderBy(static candidate => candidate.CompletionLength)
                    .ThenBy(static candidate => candidate.SegmentCount)
                    .ThenBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static candidate => candidate.Path, StringComparer.Ordinal))
            .Concat(
                tiers[2]
                    .OrderBy(static candidate => candidate.Distance)
                    .ThenBy(static candidate => candidate.SegmentCount)
                    .ThenBy(static candidate => candidate.Path.Length)
                    .ThenBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static candidate => candidate.Path, StringComparer.Ordinal))
            .Concat(
                tiers[3]
                    .OrderBy(static candidate => candidate.Distance)
                    .ThenBy(static candidate => candidate.SegmentCount)
                    .ThenBy(static candidate => candidate.Path.Length)
                    .ThenBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static candidate => candidate.Path, StringComparer.Ordinal))
            .Select(static candidate => candidate.Path)
            .ToArray();
    }
}
