using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Services;

using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp.Serve.BestPractices;

public sealed class TextSearchEngineTests
{
    private static List<BestPracticeDocument> CreateTestDocuments()
    {
        return
        [
            new BestPracticeDocument
            {
                Id = "dataloader-basic",
                Title = "DataLoader Basics",
                Category = BestPracticeCategory.DataLoader,
                Tags = ["hot-chocolate-16"],
                Styles = [],
                Keywords = "batching n+1 nplusone batch fetch load group request cache",
                Abstract = "Learn how to use DataLoader for efficient batching.",
                Body = "DataLoader groups individual requests into batches. "
                    + "This reduces the number of database round trips."
            },
            new BestPracticeDocument
            {
                Id = "resolver-patterns",
                Title = "Resolver Patterns",
                Category = BestPracticeCategory.Resolvers,
                Tags = ["hot-chocolate-16"],
                Styles = [],
                Keywords = "resolver function method handler field data fetch return pure static",
                Abstract = "Best practices for writing resolvers in Hot Chocolate.",
                Body = "Resolvers should be pure functions. "
                    + "Use dependency injection for services."
            },
            new BestPracticeDocument
            {
                Id = "pagination-relay",
                Title = "Relay-Style Pagination",
                Category = BestPracticeCategory.Pagination,
                Tags = ["relay", "hot-chocolate-16"],
                Styles = [],
                Keywords = "list array collection items connections edges nodes paging page pages results",
                Abstract = "Implement cursor-based pagination with Relay spec.",
                Body = "Cursor-based pagination provides stable results. "
                    + "Use Connection types for paginated fields."
            },
            new BestPracticeDocument
            {
                Id = "error-handling",
                Title = "Error Handling Strategies",
                Category = BestPracticeCategory.ErrorHandling,
                Tags = ["hot-chocolate-16"],
                Styles = [],
                Keywords = "exception error catch handle try throw filter format translate",
                Abstract = "Handle errors gracefully in your GraphQL API.",
                Body = "Use error filters for consistent error formatting. "
                    + "Map exceptions to GraphQL errors."
            },
            new BestPracticeDocument
            {
                Id = "security-auth",
                Title = "Authorization and Authentication",
                Category = BestPracticeCategory.Security,
                Tags = ["hot-chocolate-16"],
                Styles = [],
                Keywords = "auth authorization authenticate permission role policy claim protect guard",
                Abstract = "Secure your GraphQL API with proper auth patterns.",
                Body = "Use the Authorize attribute for field-level security. "
                    + "Implement policies for complex authorization rules."
            }
        ];
    }

    [Fact]
    public void Search_NullQuery_ReturnsEmpty()
    {
        var engine = new TextSearchEngine(CreateTestDocuments());
        var results = engine.Search(null);
        Assert.Empty(results);
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsEmpty()
    {
        var engine = new TextSearchEngine(CreateTestDocuments());
        var results = engine.Search("");
        Assert.Empty(results);
    }

    [Fact]
    public void Search_WhitespaceQuery_ReturnsEmpty()
    {
        var engine = new TextSearchEngine(CreateTestDocuments());
        var results = engine.Search("   ");
        Assert.Empty(results);
    }

    [Fact]
    public void Search_ExactTitleMatch_RanksFirst()
    {
        var engine = new TextSearchEngine(CreateTestDocuments());
        var results = engine.Search("DataLoader");

        Assert.NotEmpty(results);
        Assert.Equal(0, results[0].DocumentIndex); // "DataLoader Basics"
    }

    [Fact]
    public void Search_TypoQuery_StillFindsRelevantDocs()
    {
        var engine = new TextSearchEngine(CreateTestDocuments());

        // "datalodr" shares trigrams with "dataloader"
        var results = engine.Search("datalodr");
        Assert.NotEmpty(results);

        // The DataLoader doc should appear in the results
        Assert.Contains(results, r => r.DocumentIndex == 0);
    }

    [Fact]
    public void Search_MultiWordQuery_CombinesTermScores()
    {
        var engine = new TextSearchEngine(CreateTestDocuments());
        var results = engine.Search("Relay pagination cursor");

        Assert.NotEmpty(results);
        // The pagination doc should rank highest since it contains all three terms
        Assert.Equal(2, results[0].DocumentIndex); // "Relay-Style Pagination"
    }

    [Fact]
    public void Search_MaxResults_IsRespected()
    {
        var engine = new TextSearchEngine(CreateTestDocuments());
        var results = engine.Search("hot chocolate", maxResults: 2);

        Assert.True(results.Count <= 2);
    }

    [Fact]
    public void Search_Scores_AreDescending()
    {
        var engine = new TextSearchEngine(CreateTestDocuments());
        var results = engine.Search("DataLoader batching");

        for (var i = 1; i < results.Count; i++)
        {
            Assert.True(results[i - 1].Score >= results[i].Score,
                $"Score at index {i - 1} ({results[i - 1].Score}) "
                + $"should be >= score at index {i} ({results[i].Score})");
        }
    }

    [Fact]
    public void Search_SpecialCharacters_HandledGracefully()
    {
        var engine = new TextSearchEngine(CreateTestDocuments());

        // Should not throw and should return results or empty
        var results = engine.Search("@#$%^&*()");
        Assert.NotNull(results);
    }

    [Fact]
    public void Search_IrrelevantQuery_ScoresLowerThanRelevantQuery()
    {
        var engine = new TextSearchEngine(CreateTestDocuments());
        var irrelevantResults = engine.Search("kubernetes deployment yaml");
        var relevantResults = engine.Search("DataLoader batching");

        // Relevant query should produce a higher top score than irrelevant query
        var irrelevantTopScore = irrelevantResults.Count > 0
            ? irrelevantResults[0].Score
            : 0.0;
        var relevantTopScore = relevantResults.Count > 0
            ? relevantResults[0].Score
            : 0.0;

        Assert.True(relevantTopScore > irrelevantTopScore,
            $"Relevant top score ({relevantTopScore}) should exceed "
            + $"irrelevant top score ({irrelevantTopScore})");
    }

    [Fact]
    public void Search_TitleMatch_ScoresHigherThanBodyOnly()
    {
        var engine = new TextSearchEngine(CreateTestDocuments());

        // "Authorization" appears in title of security-auth doc (index 4)
        // and "Authorize" in body of the same doc
        var results = engine.Search("Authorization");

        Assert.NotEmpty(results);
        Assert.Equal(4, results[0].DocumentIndex); // security-auth
    }

    [Theory]
    [InlineData("list")]
    [InlineData("array")]
    [InlineData("collection")]
    public void Search_ConceptualSynonym_FindsPaginationDoc(string query)
    {
        var engine = new TextSearchEngine(CreateTestDocuments());
        var results = engine.Search(query);

        Assert.NotEmpty(results);
        // The pagination doc (index 2) should appear in results via keywords
        Assert.Contains(results, r => r.DocumentIndex == 2);
    }

    [Fact]
    public void Search_KeywordMatch_RanksHigherThanNoMatch()
    {
        var engine = new TextSearchEngine(CreateTestDocuments());
        var results = engine.Search("array");

        Assert.NotEmpty(results);
        // pagination-relay doc has "array" in Keywords — should rank high
        var paginationResult = results.First(r => r.DocumentIndex == 2);
        Assert.True(paginationResult.Score > 0);
    }

    [Fact]
    public void Tokenize_SplitsOnNonAlphanumeric()
    {
        var tokens = TextSearchEngine.Tokenize("hello-world foo_bar baz123");

        Assert.Contains("hello", tokens);
        Assert.Contains("world", tokens);
        Assert.Contains("foo_bar", tokens);
        Assert.Contains("baz123", tokens);
    }

    [Fact]
    public void Tokenize_DiscardsShortTokens()
    {
        var tokens = TextSearchEngine.Tokenize("I am a test");

        Assert.DoesNotContain("i", tokens);
        Assert.DoesNotContain("a", tokens);
        Assert.Contains("am", tokens);
        Assert.Contains("test", tokens);
    }

    [Fact]
    public void Tokenize_LowercasesTokens()
    {
        var tokens = TextSearchEngine.Tokenize("DataLoader GraphQL");

        Assert.Contains("dataloader", tokens);
        Assert.Contains("graphql", tokens);
    }
}
