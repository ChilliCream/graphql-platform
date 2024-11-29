namespace HotChocolate.Types.Pagination;

public class ConnectionPageInfoTests
{
    [InlineData(true, true, "a", "b")]
    [InlineData(true, false, "a", "b")]
    [InlineData(false, true, "a", "b")]
    [InlineData(true, true, null, "b")]
    [InlineData(true, true, "a", null)]
    [Theory]
    public void CreatePageInfo_ArgumentsArePassedCorrectly(
        bool hasNextPage,
        bool hasPreviousPage,
        string? startCursor,
        string? endCursor)
    {
        // arrange
        // act
        var pageInfo = new ConnectionPageInfo(hasNextPage, hasPreviousPage, startCursor, endCursor);

        // assert
        Assert.Equal(hasNextPage, pageInfo.HasNextPage);
        Assert.Equal(hasPreviousPage, pageInfo.HasPreviousPage);
        Assert.Equal(startCursor, pageInfo.StartCursor);
        Assert.Equal(endCursor, pageInfo.EndCursor);
    }
}
