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
    private const int MaxQueryLength = 1024;
    private const int MaxPaths = 5;

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
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(first);

        if (query.Length > MaxQueryLength)
        {
            throw new SearchQueryTooLargeException();
        }

        if (after is { Length: 0 })
        {
            throw new ArgumentException("The cursor must not be empty.", nameof(after));
        }

        var data = EnsureIndex();
        var queryTokens = BM25Tokenizer.Tokenize(query);
        var rawResults = data.Index.Search(queryTokens, cancellationToken);

        if (rawResults.Count == 0)
        {
            return ValueTask.FromResult<IReadOnlyList<SchemaSearchResult>>(Array.Empty<SchemaSearchResult>());
        }

        // Determine the maximum raw score for normalization.
        var maxRawScore = rawResults[0].Score; // Results are sorted descending.

        // Decode the cursor to determine the starting offset.
        var offset = after is not null
            ? DecodeCursor(after, rawResults.Count)
            : 0;

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

        return ValueTask.FromResult<IReadOnlyList<SchemaSearchResult>>(results);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<SchemaCoordinatePath>> GetPathsToRootAsync(
        SchemaCoordinate coordinate,
        CancellationToken cancellationToken = default)
    {
        var data = EnsureIndex();
        var result = FindPathsToRoot(data, coordinate, MaxPaths, cancellationToken);

        // Sort by path length (shortest first).
        result.Sort(static (a, b) => a.Count.CompareTo(b.Count));

        return ValueTask.FromResult<IReadOnlyList<SchemaCoordinatePath>>(result);
    }

    private static List<SchemaCoordinatePath> FindPathsToRoot(
        SearchData data,
        SchemaCoordinate coordinate,
        int maxPaths,
        CancellationToken cancellationToken)
    {
        var rootTypeNames = data.RootTypeNames;
        var reverseMap = data.ReverseMap;
        var startTypeName = coordinate.Name;
        var paths = new List<SchemaCoordinatePath>();

        // If the start type is already a root type, the path is just the coordinate itself
        // for field coordinates, or empty for type coordinates.
        if (rootTypeNames.Contains(startTypeName))
        {
            if (coordinate.MemberName is not null)
            {
                paths.Add(new SchemaCoordinatePath([coordinate]));
            }

            return paths;
        }

        // BFS: each queue entry is (currentTypeName, pathSoFar).
        // The path accumulates field hops only; the target coordinate is appended
        // when finalizing a completed path.
        var queue = new Queue<(string TypeName, List<SchemaCoordinate> Path)>();
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
                if (!visited.Add(reference.Name))
                {
                    continue;
                }

                var newPath = new List<SchemaCoordinate>(currentPath.Count + 1) { reference };
                newPath.AddRange(currentPath);

                if (rootTypeNames.Contains(reference.Name))
                {
                    if (coordinate.MemberName is not null)
                    {
                        newPath.Add(coordinate);
                    }

                    paths.Add(new SchemaCoordinatePath(CollectionsMarshal.AsSpan(newPath)));

                    if (paths.Count >= maxPaths)
                    {
                        break;
                    }
                }
                else
                {
                    queue.Enqueue((reference.Name, newPath));
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

    private static int DecodeCursor(string cursor, int resultCount)
    {
        int offset;

        try
        {
            var bytes = Convert.FromBase64String(cursor);

            if (bytes.Length < 4)
            {
                throw new InvalidSearchCursorException();
            }

            offset = BitConverter.ToInt32(bytes, 0);
        }
        catch (FormatException)
        {
            throw new InvalidSearchCursorException();
        }

        if (offset < 0 || offset > resultCount)
        {
            throw new InvalidSearchCursorException();
        }

        return offset;
    }

    /// <summary>
    /// Holds the lazily-built search data structures.
    /// </summary>
    private sealed class SearchData(
        BM25Index index,
        FrozenDictionary<string, SchemaCoordinate[]> reverseMap,
        FrozenSet<string> rootTypeNames)
    {
        public BM25Index Index { get; } = index;

        public FrozenDictionary<string, SchemaCoordinate[]> ReverseMap { get; } = reverseMap;

        public FrozenSet<string> RootTypeNames { get; } = rootTypeNames;
    }
}
