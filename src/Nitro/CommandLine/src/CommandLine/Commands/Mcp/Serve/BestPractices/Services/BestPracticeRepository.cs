using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Extensions;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Services;

internal sealed class BestPracticeRepository
{
    private readonly IReadOnlyList<BestPracticeDocument> _documents;
    private readonly TextSearchEngine _searchEngine;

    public BestPracticeRepository()
    {
        _documents = BestPracticeData.GetAll();
        _searchEngine = new TextSearchEngine(_documents);
    }

    public IReadOnlyList<BestPracticeSearchResult> Search(
        BestPracticeSearchRequest request,
        IReadOnlyList<string>? projectStyles)
    {
        var query = _documents.AsEnumerable();

        // 1. Style pre-filter (skip if explicit tags provided)
        if (request.Tags is null || request.Tags.Count == 0)
        {
            query = ApplyStyleFilter(query, projectStyles);
        }

        // 2. Category filter
        if (request.Category.HasValue)
        {
            query = query.Where(d => d.Category == request.Category.Value);
        }

        // 3. Tag filter (ALL tags must match)
        if (request.Tags is { Count: > 0 })
        {
            query = query.Where(d => request.Tags.All(t => d.Tags.Contains(t)));
        }

        // 4. Text search + scoring
        if (!string.IsNullOrEmpty(request.Query))
        {
            var candidateSet = query.ToHashSet();
            var candidateIndexMap = new Dictionary<int, BestPracticeDocument>();
            for (var i = 0; i < _documents.Count; i++)
            {
                if (candidateSet.Contains(_documents[i]))
                {
                    candidateIndexMap[i] = _documents[i];
                }
            }

            var searchResults = _searchEngine.Search(request.Query);
            query = searchResults
                .Where(r => candidateIndexMap.ContainsKey(r.DocumentIndex))
                .Select(r => candidateIndexMap[r.DocumentIndex]);
        }

        // 5. Take top 20
        return query
            .Take(20)
            .Select(d => new BestPracticeSearchResult
            {
                Id = d.Id,
                Title = d.Title,
                Abstract = d.Abstract,
                Category = d.Category.ToDisplayString(),
                Tags = d.Tags
            })
            .ToList();
    }

    public BestPracticeDocument? GetById(string id)
        => _documents.FirstOrDefault(d => string.Equals(d.Id, id, StringComparison.Ordinal));

    private static IEnumerable<BestPracticeDocument> ApplyStyleFilter(
        IEnumerable<BestPracticeDocument> docs,
        IReadOnlyList<string>? projectStyles)
    {
        if (projectStyles is null || projectStyles.Count == 0)
        {
            return docs;
        }

        return docs.Where(d =>
            d.Styles.Count == 0 || d.Styles.Contains("all") || d.Styles.Any(s => projectStyles.Contains(s))
        );
    }
}
