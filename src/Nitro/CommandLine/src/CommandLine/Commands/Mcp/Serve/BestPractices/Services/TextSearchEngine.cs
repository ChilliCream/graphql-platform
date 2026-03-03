using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Services;

internal sealed class TextSearchEngine
{
    private const double K1 = 1.5;
    private const double B = 0.75;
    private const double Bm25Weight = 0.7;
    private const double NgramWeight = 0.3;
    private const double TitleFieldWeight = 3.0;
    private const double KeywordsFieldWeight = 4.0;
    private const double AbstractFieldWeight = 2.0;
    private const double BodyFieldWeight = 1.0;

    private readonly int _documentCount;
    private readonly FieldIndex _titleIndex;
    private readonly FieldIndex _keywordsIndex;
    private readonly FieldIndex _abstractIndex;
    private readonly FieldIndex _bodyIndex;
    private readonly HashSet<string>[] _documentTrigrams;

    public TextSearchEngine(IReadOnlyList<BestPracticeDocument> documents)
    {
        _documentCount = documents.Count;

        var titleTexts = new string[_documentCount];
        var keywordsTexts = new string[_documentCount];
        var abstractTexts = new string[_documentCount];
        var bodyTexts = new string[_documentCount];

        _documentTrigrams = new HashSet<string>[_documentCount];

        for (var i = 0; i < _documentCount; i++)
        {
            titleTexts[i] = documents[i].Title;
            keywordsTexts[i] = documents[i].Keywords;
            abstractTexts[i] = documents[i].Abstract;
            bodyTexts[i] = documents[i].Body;

            _documentTrigrams[i] = BuildDocumentTrigrams(
                titleTexts[i], keywordsTexts[i], abstractTexts[i], bodyTexts[i]);
        }

        _titleIndex = new FieldIndex(titleTexts);
        _keywordsIndex = new FieldIndex(keywordsTexts);
        _abstractIndex = new FieldIndex(abstractTexts);
        _bodyIndex = new FieldIndex(bodyTexts);
    }

    public List<TextSearchResult> Search(string? query, int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var queryTokens = Tokenize(query);
        if (queryTokens.Count == 0)
        {
            return [];
        }

        var queryTrigrams = BuildTrigrams(query);

        var bm25Scores = new double[_documentCount];
        var ngramScores = new double[_documentCount];

        // Compute BM25 per field, weighted
        for (var i = 0; i < _documentCount; i++)
        {
            bm25Scores[i] =
                TitleFieldWeight * ComputeBm25(_titleIndex, queryTokens, i)
                + KeywordsFieldWeight * ComputeBm25(_keywordsIndex, queryTokens, i)
                + AbstractFieldWeight * ComputeBm25(_abstractIndex, queryTokens, i)
                + BodyFieldWeight * ComputeBm25(_bodyIndex, queryTokens, i);
        }

        // Compute trigram Jaccard similarity
        if (queryTrigrams.Count > 0)
        {
            for (var i = 0; i < _documentCount; i++)
            {
                var docTrigrams = _documentTrigrams[i];
                if (docTrigrams.Count == 0)
                {
                    continue;
                }

                var intersection = 0;
                foreach (var trigram in queryTrigrams)
                {
                    if (docTrigrams.Contains(trigram))
                    {
                        intersection++;
                    }
                }

                var union = queryTrigrams.Count + docTrigrams.Count - intersection;
                ngramScores[i] = union > 0 ? (double)intersection / union : 0.0;
            }
        }

        // Min-max normalize BM25
        var minBm25 = double.MaxValue;
        var maxBm25 = double.MinValue;

        for (var i = 0; i < _documentCount; i++)
        {
            if (bm25Scores[i] < minBm25)
            {
                minBm25 = bm25Scores[i];
            }

            if (bm25Scores[i] > maxBm25)
            {
                maxBm25 = bm25Scores[i];
            }
        }

        var bm25Range = maxBm25 - minBm25;

        // Compute hybrid scores and collect candidates
        var results = new List<TextSearchResult>();

        for (var i = 0; i < _documentCount; i++)
        {
            var normalizedBm25 = bm25Range > 0 ? (bm25Scores[i] - minBm25) / bm25Range : 0.0;

            var finalScore = Bm25Weight * normalizedBm25 + NgramWeight * ngramScores[i];

            if (finalScore > 0)
            {
                results.Add(new TextSearchResult { DocumentIndex = i, Score = finalScore });
            }
        }

        results.Sort((a, b) => b.Score.CompareTo(a.Score));

        if (results.Count > maxResults)
        {
            results.RemoveRange(maxResults, results.Count - maxResults);
        }

        return results;
    }

    private double ComputeBm25(FieldIndex index, List<string> queryTokens, int docIndex)
    {
        var score = 0.0;
        var docLength = index.DocumentLengths[docIndex];
        var avgDl = index.AverageDocumentLength;

        foreach (var token in queryTokens)
        {
            if (!index.InvertedIndex.TryGetValue(token, out var postings))
            {
                continue;
            }

            var df = postings.Count;
            var idf = Math.Log((_documentCount - df + 0.5) / (df + 0.5) + 1.0);

            if (!postings.TryGetValue(docIndex, out var tf))
            {
                continue;
            }

            var numerator = tf * (K1 + 1.0);
            var denominator = tf + K1 * (1.0 - B + B * docLength / avgDl);
            score += idf * (numerator / denominator);
        }

        return score;
    }

    internal static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        var start = -1;

        for (var i = 0; i <= text.Length; i++)
        {
            var isTokenChar = i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_');

            if (isTokenChar)
            {
                if (start < 0)
                {
                    start = i;
                }
            }
            else if (start >= 0)
            {
                var length = i - start;
                if (length >= 2)
                {
                    tokens.Add(text.Substring(start, length).ToLowerInvariant());
                }

                start = -1;
            }
        }

        return tokens;
    }

    private static HashSet<string> BuildTrigrams(string text)
    {
        var trigrams = new HashSet<string>(StringComparer.Ordinal);

        // Tokenize already lowercases each token, no need to lower the full text.
        var tokens = Tokenize(text);

        foreach (var token in tokens)
        {
            if (token.Length < 3)
            {
                trigrams.Add(token);
            }
            else
            {
                for (var i = 0; i <= token.Length - 3; i++)
                {
                    trigrams.Add(token.Substring(i, 3));
                }
            }
        }

        return trigrams;
    }

    private static HashSet<string> BuildDocumentTrigrams(
        string title, string keywords, string abstractText, string body)
    {
        var trigrams = BuildTrigrams(title);
        trigrams.UnionWith(BuildTrigrams(keywords));
        trigrams.UnionWith(BuildTrigrams(abstractText));
        trigrams.UnionWith(BuildTrigrams(body));
        return trigrams;
    }

    private sealed class FieldIndex
    {
        public Dictionary<string, Dictionary<int, int>> InvertedIndex { get; }
        public int[] DocumentLengths { get; }
        public double AverageDocumentLength { get; }

        public FieldIndex(string[] texts)
        {
            var docCount = texts.Length;
            InvertedIndex = new Dictionary<string, Dictionary<int, int>>(StringComparer.Ordinal);
            DocumentLengths = new int[docCount];

            var totalLength = 0L;

            for (var docIndex = 0; docIndex < docCount; docIndex++)
            {
                var tokens = Tokenize(texts[docIndex]);
                DocumentLengths[docIndex] = tokens.Count;
                totalLength += tokens.Count;

                foreach (var token in tokens)
                {
                    if (!InvertedIndex.TryGetValue(token, out var postings))
                    {
                        postings = new Dictionary<int, int>();
                        InvertedIndex[token] = postings;
                    }

                    postings.TryGetValue(docIndex, out var count);
                    postings[docIndex] = count + 1;
                }
            }

            AverageDocumentLength = docCount > 0 ? (double)totalLength / docCount : 0.0;
        }
    }
}
