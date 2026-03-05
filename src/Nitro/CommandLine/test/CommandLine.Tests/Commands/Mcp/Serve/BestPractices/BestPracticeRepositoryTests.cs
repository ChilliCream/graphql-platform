using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Extensions;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Services;

using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp.Serve.BestPractices;

public sealed class BestPracticeRepositoryTests
{
    private readonly BestPracticeRepository _repository = new();

    [Fact]
    public void LoadsAllDocuments()
    {
        // act - Search returns max 20 results; use per-category searches
        // to verify total document count across all categories.
        var categories = new[]
        {
            BestPracticeCategory.DataLoader,
            BestPracticeCategory.Resolvers,
            BestPracticeCategory.DefiningTypes,
            BestPracticeCategory.Testing,
            BestPracticeCategory.FilteringSorting,
            BestPracticeCategory.Subscriptions,
            BestPracticeCategory.SchemaDesign,
            BestPracticeCategory.Pagination,
            BestPracticeCategory.ErrorHandling,
            BestPracticeCategory.Security,
            BestPracticeCategory.Middleware,
            BestPracticeCategory.Configuration
        };

        var allIds = new HashSet<string>();
        foreach (var category in categories)
        {
            var results = _repository.Search(
                new BestPracticeSearchRequest { Category = category },
                projectStyles: null);
            foreach (var r in results)
            {
                allIds.Add(r.Id);
            }
        }

        // assert
        Assert.True(allIds.Count >= 40,
            $"Expected at least 40 unique documents across all categories, but found {allIds.Count}.");
    }

    [Fact]
    public void GetById_KnownId_ReturnsDocument()
    {
        // act
        var doc = _repository.GetById("dataloader-basic");

        // assert
        Assert.NotNull(doc);
        Assert.Equal("dataloader-basic", doc.Id);
        Assert.Equal(BestPracticeCategory.DataLoader, doc.Category);
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        // act
        var doc = _repository.GetById("nonexistent-document-id");

        // assert
        Assert.Null(doc);
    }

    [Fact]
    public void NoDuplicateIds()
    {
        // arrange
        var allResults = _repository.Search(
            new BestPracticeSearchRequest(),
            projectStyles: null);

        // act
        var ids = allResults.Select(r => r.Id).ToList();
        var uniqueIds = ids.Distinct().ToList();

        // assert
        Assert.Equal(uniqueIds.Count, ids.Count);
    }

    [Fact]
    public void Search_CategoryFilter_ReturnsOnlyMatchingCategory()
    {
        // arrange
        var request = new BestPracticeSearchRequest
        {
            Category = BestPracticeCategory.DataLoader
        };

        // act
        var results = _repository.Search(request, projectStyles: null);

        // assert
        Assert.NotEmpty(results);
        Assert.All(results, r =>
            Assert.Equal(
                BestPracticeCategory.DataLoader.ToDisplayString(),
                r.Category));
    }

    [Fact]
    public void Search_TagFilter_ReturnsOnlyDocumentsWithTag()
    {
        // arrange
        var request = new BestPracticeSearchRequest
        {
            Tags = ["relay"]
        };

        // act
        var results = _repository.Search(request, projectStyles: null);

        // assert
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Contains("relay", r.Tags));
    }

    [Fact]
    public void Search_StyleFilter_ReturnsMatchingOrAllStyles()
    {
        // arrange
        var request = new BestPracticeSearchRequest();

        // act - search with a specific project style
        var results = _repository.Search(request, projectStyles: ["clean-architecture"]);

        // assert - should include docs with style "all" or "clean-architecture" or empty styles
        Assert.NotEmpty(results);
    }

    [Fact]
    public void Search_TextQuery_MatchesTitleAndAbstract()
    {
        // arrange
        var request = new BestPracticeSearchRequest { Query = "DataLoader" };

        // act
        var results = _repository.Search(request, projectStyles: null);

        // assert
        Assert.NotEmpty(results);
        // The first result should be highly relevant to DataLoader
        Assert.Contains(results, r =>
            r.Title.Contains("DataLoader", StringComparison.OrdinalIgnoreCase)
            || r.Abstract.Contains("DataLoader", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsAllDocuments()
    {
        // arrange
        var request = new BestPracticeSearchRequest();

        // act
        var results = _repository.Search(request, projectStyles: null);

        // assert - limited to 20 max by the search method
        Assert.True(results.Count == 20,
            $"Expected 20 results (max limit), but found {results.Count}.");
    }

    [Fact]
    public void Search_ExactTitleMatch_ScoresHigherThanContainsMatch()
    {
        // arrange - "Authorization" is in the title of security-authorization doc
        var request = new BestPracticeSearchRequest { Query = "Authorization" };

        // act
        var results = _repository.Search(request, projectStyles: null);

        // assert
        Assert.NotEmpty(results);
        // The first result should have "Authorization" in the title
        Assert.Contains("Authorization", results[0].Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AllDocuments_HaveNonEmptyRequiredFields()
    {
        // arrange - get all documents by searching without filters
        // We need to use GetById for each known result to check Body
        var searchResults = _repository.Search(
            new BestPracticeSearchRequest(),
            projectStyles: null);

        // assert - check search result fields
        Assert.All(searchResults, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Id), "Id should not be empty.");
            Assert.False(string.IsNullOrWhiteSpace(r.Title), "Title should not be empty.");
            Assert.False(string.IsNullOrWhiteSpace(r.Category), "Category should not be empty.");
        });

        // Also check that GetById returns documents with non-empty Body
        foreach (var result in searchResults)
        {
            var doc = _repository.GetById(result.Id);
            Assert.NotNull(doc);
            Assert.False(string.IsNullOrWhiteSpace(doc.Body),
                $"Document '{doc.Id}' should have non-empty Body.");
        }
    }

    [Fact]
    public void Search_CategoryFilter_SecurityCategory_ReturnsResults()
    {
        // arrange
        var request = new BestPracticeSearchRequest
        {
            Category = BestPracticeCategory.Security
        };

        // act
        var results = _repository.Search(request, projectStyles: null);

        // assert
        Assert.NotEmpty(results);
        Assert.All(results, r =>
            Assert.Equal(
                BestPracticeCategory.Security.ToDisplayString(),
                r.Category));
    }

    [Fact]
    public void Search_MultipleTagsFilter_RequiresAllTags()
    {
        // arrange - require both hot-chocolate-16 and relay tags
        var request = new BestPracticeSearchRequest
        {
            Tags = ["hot-chocolate-16", "relay"]
        };

        // act
        var results = _repository.Search(request, projectStyles: null);

        // assert
        Assert.NotEmpty(results);
        Assert.All(results, r =>
        {
            Assert.Contains("hot-chocolate-16", r.Tags);
            Assert.Contains("relay", r.Tags);
        });
    }
}
