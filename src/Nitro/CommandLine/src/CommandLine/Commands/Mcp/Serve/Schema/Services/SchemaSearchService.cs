using System.Collections.Frozen;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;

internal sealed class SchemaSearchService
{
    private const int MaxPaths = 5;
    private const int MaxDepth = 12;

    private static readonly char[] _querySeparators = [' ', '.', '_', '-', '(', ')', ':'];

    private static readonly FrozenDictionary<SchemaIndexMemberKind, string> _kindNames =
        System.Enum.GetValues<SchemaIndexMemberKind>()
            .ToFrozenDictionary(k => k, k => k.ToString().ToUpperInvariant());

    public SearchResult Search(SchemaIndex index, string query, SchemaIndexMemberKind? kindFilter, int limit)
    {
        var tokens = TokenizeQuery(query);
        var allEntries = index.GetAll();

        var scored = allEntries
            .Where(e => kindFilter is null || e.Kind == kindFilter)
            .Select(e => (entry: e, score: Score(e, tokens)))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.entry.Coordinate, StringComparer.Ordinal)
            .ToArray();

        var totalCount = scored.Length;
        var results = scored
            .Take(limit)
            .Select(x => new SearchResultItem
            {
                Coordinate = x.entry.Coordinate,
                Kind = _kindNames[x.entry.Kind],
                Name = x.entry.Name,
                TypeName = x.entry.TypeName,
                Description = x.entry.Description,
                IsDeprecated = x.entry.IsDeprecated,
                DeprecationReason = x.entry.DeprecationReason,
                PathsToRoot = ComputePathsToRoot(x.entry.Coordinate, index)
            })
            .ToArray();

        return new SearchResult { Results = results, TotalCount = totalCount };
    }

    public GetMembersResult GetMembers(SchemaIndex index, IReadOnlyList<string> coordinates)
    {
        var found = new List<MemberDetail>();
        var notFound = new List<string>();

        foreach (var coordinate in coordinates)
        {
            var entry = index.GetByCoordinate(coordinate);
            if (entry is null)
            {
                notFound.Add(coordinate);
                continue;
            }

            found.Add(
                new MemberDetail
                {
                    Coordinate = entry.Coordinate,
                    Kind = _kindNames[entry.Kind],
                    Name = entry.Name,
                    ParentType = entry.ParentTypeName,
                    TypeName = entry.TypeName,
                    Description = entry.Description,
                    IsDeprecated = entry.IsDeprecated,
                    DeprecationReason = entry.DeprecationReason,
                    Arguments = entry.Arguments,
                    Directives = entry.Directives,
                    Interfaces = entry.Interfaces,
                    PossibleTypes = entry.PossibleTypes,
                    EnumValues = entry.EnumValues
                });
        }

        return new GetMembersResult { Members = found, NotFound = notFound };
    }

    private static IReadOnlyList<IReadOnlyList<string>> ComputePathsToRoot(string coordinate, SchemaIndex index)
    {
        if (index.RootTypes.Contains(coordinate))
        {
            return new[] { new[] { coordinate } };
        }

        var ownerType = coordinate.Contains('.') ? coordinate[..coordinate.IndexOf('.')] : coordinate;

        if (index.RootTypes.Contains(ownerType))
        {
            return new[] { new[] { ownerType, coordinate } };
        }

        var results = new List<IReadOnlyList<string>>();
        var queue = new Queue<(string TypeName, List<string> Path)>();
        var visited = new HashSet<string>(StringComparer.Ordinal);

        queue.Enqueue((ownerType, new List<string> { coordinate }));

        while (queue.Count > 0 && results.Count < MaxPaths)
        {
            var (currentType, path) = queue.Dequeue();

            if (path.Count > MaxDepth)
            {
                continue;
            }

            var incoming = index.GetIncomingEdges(currentType);
            if (incoming.Count == 0)
            {
                continue;
            }

            foreach (var incomingField in incoming)
            {
                if (!visited.Add(incomingField))
                {
                    continue;
                }

                var incomingOwner = incomingField.Contains('.')
                    ? incomingField[..incomingField.IndexOf('.')]
                    : incomingField;

                var newPath = new List<string> { incomingField };
                newPath.AddRange(path);

                if (index.RootTypes.Contains(incomingOwner))
                {
                    var fullPath = new List<string> { incomingOwner };
                    fullPath.AddRange(newPath);
                    results.Add(fullPath);

                    if (results.Count >= MaxPaths)
                    {
                        break;
                    }
                }
                else if (newPath.Count < MaxDepth)
                {
                    queue.Enqueue((incomingOwner, newPath));
                }
            }
        }

        return results;
    }

    private static string[] TokenizeQuery(string query)
        => query
            .ToLowerInvariant()
            .Split(_querySeparators, StringSplitOptions.RemoveEmptyEntries);

    private static int Score(SchemaIndexEntry entry, string[] tokens)
    {
        if (tokens.Length == 0)
        {
            return 1;
        }

        var name = entry.Name.ToLowerInvariant();
        var desc = (entry.Description ?? "").ToLowerInvariant();
        var score = 0;

        foreach (var token in tokens)
        {
            var tokenScore = 0;

            if (name == token)
            {
                tokenScore = 100;
            }
            else if (name.StartsWith(token, StringComparison.Ordinal))
            {
                tokenScore = 80;
            }
            else if (name.Contains(token, StringComparison.Ordinal))
            {
                tokenScore = 60;
            }
            else if (desc.Contains(token, StringComparison.Ordinal))
            {
                tokenScore = 20;
            }

            if (tokenScore == 0)
            {
                return 0;
            }

            score += tokenScore;
        }

        return score;
    }
}
