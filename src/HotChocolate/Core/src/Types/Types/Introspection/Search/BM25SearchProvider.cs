using System.Collections.Frozen;
using System.Runtime.InteropServices;
using static HotChocolate.Types.Introspection.SchemaIndexer;

namespace HotChocolate.Types.Introspection;

/// <summary>
/// The default <see cref="ISchemaSearchProvider"/> implementation that uses BM25 scoring
/// to search schema elements by natural language queries.
/// </summary>
internal sealed class BM25SearchProvider : ISchemaSearchProvider
{
    private readonly ISchemaDefinition _schema;
    private volatile SearchData? _searchData;
    private readonly object _syncRoot = new();

    /// <summary>
    /// Initializes a new instance of <see cref="BM25SearchProvider"/>.
    /// </summary>
    /// <param name="schema">
    /// The schema definition to search.
    /// </param>
    public BM25SearchProvider(ISchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        _schema = schema;
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<SchemaSearchResult>> SearchAsync(
        string query,
        int first,
        string? after,
        float? minScore,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (first <= 0)
        {
            return new ValueTask<IReadOnlyList<SchemaSearchResult>>(
                Array.Empty<SchemaSearchResult>());
        }

        var data = EnsureIndex();
        var queryTokens = BM25Tokenizer.Tokenize(query);
        var rawResults = data.Index.Search(queryTokens, cancellationToken);

        if (rawResults.Count == 0)
        {
            return new ValueTask<IReadOnlyList<SchemaSearchResult>>(
                Array.Empty<SchemaSearchResult>());
        }

        // Determine the maximum raw score for normalization.
        var maxRawScore = rawResults[0].Score; // Results are sorted descending.

        // Decode the cursor to determine the starting offset.
        var offset = 0;

        if (after is not null)
        {
            offset = DecodeCursor(after);
        }

        var results = new List<SchemaSearchResult>(Math.Min(first, rawResults.Count));

        for (var i = offset; i < rawResults.Count && results.Count < first; i++)
        {
            var scored = rawResults[i];
            var normalizedScore = maxRawScore > 0f ? scored.Score / maxRawScore : 0f;

            if (minScore.HasValue && normalizedScore < minScore.Value)
            {
                // Results are sorted by score descending, so all subsequent
                // results will also be below the threshold.
                break;
            }

            results.Add(new SchemaSearchResult(
                data.Index.GetCoordinate(scored.DocumentId),
                normalizedScore,
                EncodeCursor(i + 1)));
        }

        return new ValueTask<IReadOnlyList<SchemaSearchResult>>(results);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<SchemaCoordinatePath>> GetPathsToRootAsync(
        SchemaCoordinate coordinate,
        int maxPaths,
        CancellationToken cancellationToken = default)
    {
        if (maxPaths <= 0)
        {
            return new ValueTask<IReadOnlyList<SchemaCoordinatePath>>(
                Array.Empty<SchemaCoordinatePath>());
        }

        var data = EnsureIndex();

        // Determine the type name to start BFS from.
        // If the coordinate has a member name, it's a field/value on a type;
        // the starting type is the coordinate's Name.
        // If it's a type coordinate, we start from that type directly.
        var startTypeName = coordinate.Name;

        var paths = FindPathsToRoot(data, startTypeName, maxPaths, cancellationToken);

        // Build SchemaCoordinatePath instances.
        // Each path is from the target coordinate back to a root type field.
        var result = new List<SchemaCoordinatePath>(paths.Count);

        foreach (var path in paths)
        {
            var segments = new List<SchemaCoordinate>();

            // If the original coordinate has a member (it's a field/value),
            // include it as the first segment.
            if (coordinate.MemberName is not null)
            {
                segments.Add(coordinate);
            }

            // Add the type-level coordinate for the starting type.
            segments.Add(new SchemaCoordinate(startTypeName));

            // Add intermediate hops (type.field coordinates leading to root).
            foreach (var (typeName, fieldName) in path)
            {
                segments.Add(new SchemaCoordinate(typeName, fieldName));
                segments.Add(new SchemaCoordinate(typeName));
            }

            result.Add(new SchemaCoordinatePath(CollectionsMarshal.AsSpan(segments)));
        }

        // Sort by path length (shortest first).
        result.Sort(static (a, b) => a.Count.CompareTo(b.Count));

        // Limit to maxPaths.
        if (result.Count > maxPaths)
        {
            result.RemoveRange(maxPaths, result.Count - maxPaths);
        }

        return new ValueTask<IReadOnlyList<SchemaCoordinatePath>>(result);
    }

    private static List<List<TypeFieldReference>> FindPathsToRoot(
        SearchData data,
        string startTypeName,
        int maxPaths,
        CancellationToken cancellationToken)
    {
        var rootTypeNames = data.RootTypeNames;
        var reverseMap = data.ReverseMap;
        var paths = new List<List<TypeFieldReference>>();

        // If the start type is already a root type, return a single empty path.
        if (rootTypeNames.Contains(startTypeName))
        {
            paths.Add([]);
            return paths;
        }

        // BFS: each queue entry is (currentTypeName, pathSoFar).
        var queue = new Queue<(string TypeName, List<TypeFieldReference> Path)>();
        queue.Enqueue((startTypeName, []));

        var visited = new HashSet<string>(StringComparer.Ordinal) { startTypeName };

        while (queue.Count > 0 && paths.Count < maxPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (currentType, currentPath) = queue.Dequeue();

            if (!reverseMap.TryGetValue(currentType, out var references))
            {
                continue;
            }

            foreach (var reference in references)
            {
                if (!visited.Add(reference.TypeName))
                {
                    continue;
                }

                var newPath = new List<TypeFieldReference>(currentPath) { reference };

                if (rootTypeNames.Contains(reference.TypeName))
                {
                    paths.Add(newPath);

                    if (paths.Count >= maxPaths)
                    {
                        break;
                    }
                }
                else
                {
                    queue.Enqueue((reference.TypeName, newPath));
                }
            }
        }

        return paths;
    }

    private SearchData EnsureIndex()
    {
        if (_searchData is not null)
        {
            return _searchData;
        }

        lock (_syncRoot)
        {
            if (_searchData is not null)
            {
                return _searchData;
            }

            var (documents, reverseMap) = Index(_schema);
            var index = BM25Index.Build(documents);

            var rootTypeNames = new HashSet<string>(StringComparer.Ordinal)
            {
                _schema.QueryType.Name
            };

            if (_schema.MutationType is not null)
            {
                rootTypeNames.Add(_schema.MutationType.Name);
            }

            if (_schema.SubscriptionType is not null)
            {
                rootTypeNames.Add(_schema.SubscriptionType.Name);
            }

            _searchData = new SearchData(
                index,
                reverseMap.ToFrozenDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToArray(),
                    StringComparer.Ordinal),
                rootTypeNames.ToFrozenSet(StringComparer.Ordinal));

            return _searchData;
        }
    }

    private static string EncodeCursor(int offset)
        => Convert.ToBase64String(BitConverter.GetBytes(offset));

    private static int DecodeCursor(string cursor)
    {
        try
        {
            var bytes = Convert.FromBase64String(cursor);

            if (bytes.Length >= 4)
            {
                return BitConverter.ToInt32(bytes, 0);
            }
        }
        catch (FormatException)
        {
            // Invalid cursor format; start from the beginning.
        }

        return 0;
    }

    /// <summary>
    /// Holds the lazily-built search data structures.
    /// </summary>
    private sealed class SearchData(
        BM25Index index,
        FrozenDictionary<string, TypeFieldReference[]> reverseMap,
        FrozenSet<string> rootTypeNames)
    {
        public BM25Index Index { get; } = index;

        public FrozenDictionary<string, TypeFieldReference[]> ReverseMap { get; } = reverseMap;

        public FrozenSet<string> RootTypeNames { get; } = rootTypeNames;
    }
}
