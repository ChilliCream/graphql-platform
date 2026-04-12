using System.Buffers;
using System.Collections.Frozen;

namespace HotChocolate.Types.Introspection;

/// <summary>
/// An inverted index that supports BM25 scoring for schema search.
/// This class is immutable after construction via <see cref="Build"/>.
/// </summary>
internal sealed class BM25Index
{
    private const float K1 = 1.2f;
    private const float B = 0.75f;

    private readonly FrozenDictionary<string, TermPosting[]> _invertedIndex;
    private readonly float[] _documentLengths;
    private readonly float _averageDocumentLength;
    private readonly int _documentCount;
    private readonly SchemaCoordinate[] _coordinates;

    private BM25Index(
        FrozenDictionary<string, TermPosting[]> invertedIndex,
        float[] documentLengths,
        float averageDocumentLength,
        int documentCount,
        SchemaCoordinate[] coordinates)
    {
        _invertedIndex = invertedIndex;
        _documentLengths = documentLengths;
        _averageDocumentLength = averageDocumentLength;
        _documentCount = documentCount;
        _coordinates = coordinates;
    }

    /// <summary>
    /// Gets the total number of documents in the index.
    /// </summary>
    public int DocumentCount => _documentCount;

    /// <summary>
    /// Gets the schema coordinate for the specified document ID.
    /// </summary>
    /// <param name="documentId">
    /// The document ID.
    /// </param>
    /// <returns>
    /// The schema coordinate associated with the document.
    /// </returns>
    public SchemaCoordinate GetCoordinate(int documentId) => _coordinates[documentId];

    /// <summary>
    /// Builds a BM25 index from the specified documents.
    /// </summary>
    /// <param name="documents">
    /// The documents to index.
    /// </param>
    /// <returns>
    /// A new <see cref="BM25Index"/> instance.
    /// </returns>
    public static BM25Index Build(IReadOnlyList<BM25Document> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        var count = documents.Count;
        var coordinates = new SchemaCoordinate[count];
        var documentLengths = new float[count];
        var builder = new Dictionary<string, List<TermPosting>>(StringComparer.Ordinal);
        var totalLength = 0f;

        for (var documentId = 0; documentId < count; documentId++)
        {
            var doc = documents[documentId];
            coordinates[documentId] = doc.Coordinate;

            var tokens = BM25Tokenizer.Tokenize(doc.Text);
            documentLengths[documentId] = tokens.Length;
            totalLength += tokens.Length;

            // Count term frequencies for this document.
            var termFrequencies = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var token in tokens)
            {
                if (!termFrequencies.TryGetValue(token, out var frequency))
                {
                    frequency = 0;
                }

                termFrequencies[token] = frequency + 1;
            }

            // Add to the inverted index.
            foreach (var (term, frequency) in termFrequencies)
            {
                if (!builder.TryGetValue(term, out var postings))
                {
                    postings = [];
                    builder[term] = postings;
                }

                postings.Add(new TermPosting(documentId, frequency));
            }
        }

        var averageDocumentLength = count > 0 ? totalLength / count : 0f;

        return new BM25Index(
            builder.ToFrozenDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToArray(),
                StringComparer.Ordinal),
            documentLengths,
            averageDocumentLength,
            count,
            coordinates);
    }

    /// <summary>
    /// Searches the index with the specified query tokens and returns
    /// scored document IDs sorted by score descending.
    /// </summary>
    /// <param name="queryTokens">
    /// The tokenized query terms.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// A list of document ID and raw BM25 score pairs, sorted by score descending.
    /// </returns>
    public IReadOnlyList<ScoredDocument> Search(
        string[] queryTokens,
        CancellationToken cancellationToken = default)
    {
        if (queryTokens.Length == 0 || _documentCount == 0)
        {
            return [];
        }

        // Use rented array for accumulating scores to avoid allocations on large schemas.
        var scores = ArrayPool<float>.Shared.Rent(_documentCount);

        try
        {
            Array.Clear(scores, 0, _documentCount);

            foreach (var token in queryTokens)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_invertedIndex.TryGetValue(token, out var postings))
                {
                    continue;
                }

                // IDF = ln((N - df + 0.5) / (df + 0.5) + 1)
                var df = postings.Length;
                var idf = MathF.Log(((_documentCount - df + 0.5f) / (df + 0.5f)) + 1f);

                foreach (var posting in postings)
                {
                    var tf = posting.TermFrequency;
                    var documentLength = _documentLengths[posting.DocumentId];
                    var numerator = tf * (K1 + 1f);
                    var denominator = tf + K1 * (1f - B + B * (documentLength / _averageDocumentLength));
                    scores[posting.DocumentId] += idf * (numerator / denominator);
                }
            }

            // Collect non-zero scores.
            var results = new List<ScoredDocument>();

            for (var i = 0; i < _documentCount; i++)
            {
                if (scores[i] > 0f)
                {
                    results.Add(new ScoredDocument(i, scores[i]));
                }
            }

            // Sort by score descending.
            results.Sort(static (a, b) => b.Score.CompareTo(a.Score));

            return results;
        }
        finally
        {
            ArrayPool<float>.Shared.Return(scores);
        }
    }

    /// <summary>
    /// Represents a posting in the inverted index: a document ID and its term frequency.
    /// </summary>
    internal readonly record struct TermPosting(int DocumentId, int TermFrequency);

    /// <summary>
    /// Represents a scored document from a search operation.
    /// </summary>
    internal readonly record struct ScoredDocument(int DocumentId, float Score);
}
