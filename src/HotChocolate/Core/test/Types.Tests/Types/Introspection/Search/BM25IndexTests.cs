namespace HotChocolate.Types.Introspection;

public class BM25IndexTests
{
    [Fact]
    public void Build_Should_CreateIndex_When_DocumentsProvided()
    {
        // arrange
        var documents = new List<BM25Document>
        {
            new(new SchemaCoordinate("Product"), "Product A physical item for sale"),
            new(new SchemaCoordinate("Order"), "Order A customer purchase"),
            new(new SchemaCoordinate("Customer"), "Customer A person who buys")
        };

        // act
        var index = BM25Index.Build(documents);

        // assert
        Assert.Equal(3, index.DocumentCount);
    }

    [Fact]
    public void Build_Should_CreateEmptyIndex_When_NoDocuments()
    {
        // act
        var index = BM25Index.Build([]);

        // assert
        Assert.Equal(0, index.DocumentCount);
    }

    [Fact]
    public void GetCoordinate_Should_ReturnCorrectCoordinate_When_ValidDocId()
    {
        // arrange
        var coordinate = new SchemaCoordinate("Product");
        var documents = new List<BM25Document>
        {
            new(coordinate, "Product")
        };
        var index = BM25Index.Build(documents);

        // act
        var result = index.GetCoordinate(0);

        // assert
        Assert.Equal(coordinate, result);
    }

    [Fact]
    public void Search_Should_ReturnEmpty_When_NoTokensProvided()
    {
        // arrange
        var documents = new List<BM25Document>
        {
            new(new SchemaCoordinate("Product"), "Product item")
        };
        var index = BM25Index.Build(documents);

        // act
        var results = index.Search([]);

        // assert
        Assert.Empty(results);
    }

    [Fact]
    public void Search_Should_ReturnEmpty_When_IndexIsEmpty()
    {
        // arrange
        var index = BM25Index.Build([]);

        // act
        var results = index.Search(["product"]);

        // assert
        Assert.Empty(results);
    }

    [Fact]
    public void Search_Should_ReturnEmpty_When_NoMatchingTokens()
    {
        // arrange
        var documents = new List<BM25Document>
        {
            new(new SchemaCoordinate("Product"), "Product item")
        };
        var index = BM25Index.Build(documents);

        // act
        var results = index.Search(["nonexistent"]);

        // assert
        Assert.Empty(results);
    }

    [Fact]
    public void Search_Should_ReturnMatchingDocument_When_TokenMatches()
    {
        // arrange
        var documents = new List<BM25Document>
        {
            new(new SchemaCoordinate("Product"), "Product item for sale"),
            new(new SchemaCoordinate("Order"), "Order purchase record")
        };
        var index = BM25Index.Build(documents);

        // act
        var results = index.Search(["product"]);

        // assert
        Assert.Single(results);
        Assert.Equal(0, results[0].DocumentId);
        Assert.True(results[0].Score > 0f);
    }

    [Fact]
    public void Search_Should_RankByRelevance_When_MultipleMatches()
    {
        // arrange
        var documents = new List<BM25Document>
        {
            new(new SchemaCoordinate("Product"), "Product item for sale"),
            new(new SchemaCoordinate("ProductReview"), "Product review feedback on product"),
            new(new SchemaCoordinate("Order"), "Order purchase record")
        };
        var index = BM25Index.Build(documents);

        // act
        var results = index.Search(["product"]);

        // assert
        Assert.Equal(2, results.Count);
        // The document with higher term frequency for "product" should score higher.
        Assert.True(results[0].Score >= results[1].Score);
    }

    [Fact]
    public void Search_Should_SortDescending_When_MultipleMatches()
    {
        // arrange
        var documents = new List<BM25Document>
        {
            new(new SchemaCoordinate("A"), "common word here"),
            new(new SchemaCoordinate("B"), "rare unique specialized term"),
            new(new SchemaCoordinate("C"), "common word common word")
        };
        var index = BM25Index.Build(documents);

        // act
        var results = index.Search(["common", "word"]);

        // assert
        Assert.True(results.Count >= 1);

        for (var i = 1; i < results.Count; i++)
        {
            Assert.True(results[i - 1].Score >= results[i].Score);
        }
    }

    [Fact]
    public void Search_Should_ScoreHigher_When_TermIsRare()
    {
        // arrange - "rare" appears in only 1 doc, "common" appears in all
        var documents = new List<BM25Document>
        {
            new(new SchemaCoordinate("A"), "common shared"),
            new(new SchemaCoordinate("B"), "common shared"),
            new(new SchemaCoordinate("C"), "common rare unique")
        };
        var index = BM25Index.Build(documents);

        // act
        var rareResults = index.Search(["rare"]);
        var commonResults = index.Search(["common"]);

        // assert
        // "rare" has higher IDF, so a single match for "rare" should have higher
        // score than a single match for "common" (all else being equal).
        Assert.Single(rareResults);
        Assert.Equal(3, commonResults.Count);

        // The rare term match should score higher than the best common match.
        Assert.True(rareResults[0].Score > commonResults[0].Score);
    }

    [Fact]
    public void Search_Should_HandleMultipleQueryTokens()
    {
        // arrange
        var documents = new List<BM25Document>
        {
            new(new SchemaCoordinate("ProductReview"), "product review feedback"),
            new(new SchemaCoordinate("Product"), "product item"),
            new(new SchemaCoordinate("Review"), "review feedback rating")
        };
        var index = BM25Index.Build(documents);

        // act
        var results = index.Search(["product", "review"]);

        // assert
        // The document matching both tokens should score highest.
        Assert.True(results.Count >= 1);
        Assert.Equal(0, results[0].DocumentId); // ProductReview matches both terms.
    }
}
