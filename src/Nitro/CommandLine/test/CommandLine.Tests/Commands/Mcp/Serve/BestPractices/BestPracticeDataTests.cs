using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices;

using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp.Serve.BestPractices;

public sealed class BestPracticeDataTests
{
    [Fact]
    public void GetAll_Returns43Documents()
    {
        // act
        var docs = BestPracticeData.GetAll();

        // assert
        Assert.Equal(43, docs.Count);
    }

    [Fact]
    public void GetAll_AllIdsAreUnique()
    {
        // act
        var docs = BestPracticeData.GetAll();

        // assert
        var ids = docs.Select(d => d.Id).ToList();
        var uniqueIds = ids.Distinct().ToList();
        Assert.Equal(uniqueIds.Count, ids.Count);
    }

    [Fact]
    public void GetAll_AllDocumentsHaveValidCategories()
    {
        // act
        var docs = BestPracticeData.GetAll();

        // assert
        var validCategories = Enum.GetValues<BestPracticeCategory>();
        Assert.All(docs, d => Assert.Contains(d.Category, validCategories));
    }

    [Fact]
    public void GetAll_AllDocumentsHaveNonEmptyRequiredFields()
    {
        // act
        var docs = BestPracticeData.GetAll();

        // assert
        Assert.All(docs, d =>
        {
            Assert.False(string.IsNullOrWhiteSpace(d.Id),
                "Id should not be empty.");
            Assert.False(string.IsNullOrWhiteSpace(d.Title),
                "Title should not be empty.");
            Assert.False(string.IsNullOrWhiteSpace(d.Abstract),
                "Abstract should not be empty.");
            Assert.False(string.IsNullOrWhiteSpace(d.Body),
                $"Document '{d.Id}' should have non-empty Body.");
            Assert.NotNull(d.Tags);
            Assert.NotNull(d.Styles);
        });
    }
}
