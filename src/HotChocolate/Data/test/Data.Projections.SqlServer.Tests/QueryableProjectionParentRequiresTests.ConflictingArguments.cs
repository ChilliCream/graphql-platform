using HotChocolate.Execution;

namespace HotChocolate.Data.Projections;

public partial class QueryableProjectionParentRequiresTests
{
    [Theory]
    [InlineData("DifferentFiltering", false)]
    [InlineData("DifferentFiltering", true)]
    [InlineData("TwoFilters", false)]
    [InlineData("TwoFilters", true)]
    [InlineData("DifferentSorting", false)]
    [InlineData("DifferentSorting", true)]
    [InlineData("TwoSorts", false)]
    [InlineData("TwoSorts", true)]
    [InlineData("RequiredFilteredList", false)]
    [InlineData("RequiredFilteredList", true)]
    [InlineData("RequiredSortedList", false)]
    [InlineData("RequiredSortedList", true)]
    [InlineData("NestedFiltering", false)]
    [InlineData("NestedFiltering", true)]
    public async Task Projection_Should_PreserveSelections_When_ListHasConflictingArguments(
        string scenario,
        bool reverse)
    {
        // arrange
        await using var dbContext = await CreateDbContextAsync();
        var executor = scenario.StartsWith("Required", StringComparison.Ordinal)
            ? await CreateExecutorAsync(dbContext, typeof(BookReviewExtensions))
            : await CreateExecutorAsync(dbContext);
        var (first, second) = scenario switch
        {
            "DifferentFiltering" => ("a: reviews(where: { rating: { eq: 4 } }) { text }", "b: reviews { rating }"),
            "TwoFilters" => ("a: reviews(where: { rating: { eq: 4 } }) { text }", "b: reviews(where: { rating: { eq: 2 } }) { text }"),
            "DifferentSorting" => ("a: reviews(order: { rating: ASC }) { text }", "b: reviews { text }"),
            "TwoSorts" => ("a: reviews(order: { rating: ASC }) { text }", "b: reviews(order: { rating: DESC }) { text }"),
            "RequiredFilteredList" => ("a: reviews(where: { rating: { eq: 4 } }) { text }", "averageRating"),
            "RequiredSortedList" => ("a: reviews(order: { rating: ASC }) { text }", "averageRating"),
            "NestedFiltering" => (
                "a: reviews(where: { reviewer: { name: { eq: \"Alice\" } } }) { text reviewer { id } }",
                "b: reviews { rating reviewer { name } }"),
            _ => throw new InvalidOperationException()
        };
        var query = reverse
            ? $"{{ books {{ {second} {first} }} }}"
            : $"{{ books {{ {first} {second} }} }}";

        // act
        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create($"{scenario}_{reverse}_{TestEnvironment.TargetFramework}")
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }
}
