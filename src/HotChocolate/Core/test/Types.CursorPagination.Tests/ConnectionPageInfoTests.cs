namespace HotChocolate.Types.Pagination;

public class ConnectionPageInfoTests
{
    [InlineData(true, true, "a", "b")]
    [InlineData(true, false, "a", "b")]
    [InlineData(false, true, "a", "b")]
    [InlineData(true, true, null, "b")]
    [InlineData(true, true, "a", null)]
    [Theory]
    public async Task CreatePageInfo_ArgumentsArePassedCorrectly(
        bool hasNextPage,
        bool hasPreviousPage,
        string? startCursor,
        string? endCursor)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var pageInfo = new ConnectionPageInfo(hasNextPage, hasPreviousPage, startCursor, endCursor);

        // assert
        Assert.Equal(hasNextPage, await pageInfo.HasNextPageAsync(cancellationToken));
        Assert.Equal(hasPreviousPage, await pageInfo.HasPreviousPageAsync(cancellationToken));
        Assert.Equal(startCursor, await pageInfo.GetStartCursorAsync(cancellationToken));
        Assert.Equal(endCursor, await pageInfo.GetEndCursorAsync(cancellationToken));
    }

    [Fact]
    public async Task Empty_Should_ResolveNoFlagsAndNoCursors_When_Read()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var pageInfo = ConnectionPageInfo.Empty;

        // act
        var snapshot = new
        {
            HasNextPage = await pageInfo.HasNextPageAsync(cancellationToken),
            HasPreviousPage = await pageInfo.HasPreviousPageAsync(cancellationToken),
            StartCursor = await pageInfo.GetStartCursorAsync(cancellationToken),
            EndCursor = await pageInfo.GetEndCursorAsync(cancellationToken),
            ForwardCursors = await pageInfo.GetForwardCursorsAsync(cancellationToken),
            BackwardCursors = await pageInfo.GetBackwardCursorsAsync(cancellationToken)
        };

        // assert
        snapshot.MatchInlineSnapshot(
            """
            {
              "HasNextPage": false,
              "HasPreviousPage": false,
              "StartCursor": null,
              "EndCursor": null,
              "ForwardCursors": [],
              "BackwardCursors": []
            }
            """);
    }

    [Fact]
    public async Task GetForwardCursorsAsync_Should_ReturnEmpty_When_CursorsAreStored()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var pageInfo = new ConnectionPageInfo(
            hasNextPage: true,
            hasPreviousPage: true,
            startCursor: "a",
            endCursor: "b");

        // act
        var snapshot = new
        {
            ForwardCursors = await pageInfo.GetForwardCursorsAsync(cancellationToken),
            BackwardCursors = await pageInfo.GetBackwardCursorsAsync(cancellationToken)
        };

        // assert
        snapshot.MatchInlineSnapshot(
            """
            {
              "ForwardCursors": [],
              "BackwardCursors": []
            }
            """);
    }
}
