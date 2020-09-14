using Xunit;

namespace HotChocolate.Types.Pagination
{
    public class ConnectionPageInfoTests
    {
        [InlineData(true, true, "a", "b", null)]
        [InlineData(true, false, "a", "b", null)]
        [InlineData(false, true, "a", "b", null)]
        [InlineData(true, true, null, "b", null)]
        [InlineData(true, true, "a", null, null)]
        [InlineData(true, true, "a", "b", 1)]
        [InlineData(true, false, "a", "b", 2)]
        [InlineData(false, true, "a", "b", 3)]
        [InlineData(true, true, null, "b", 4)]
        [InlineData(true, true, "a", null, 5)]
        [Theory]
        public void CreatePageInfo_ArgumentsArePassedCorrectly(
            bool hasNextPage, bool hasPreviousPage,
            string startCursor, string endCursor,
            int? totalCount)
        {
            // arrange
            // act
            var pageInfo = new ConnectionPageInfo(
                hasNextPage, hasPreviousPage,
                startCursor, endCursor,
                totalCount);

            // assert
            Assert.Equal(hasNextPage, pageInfo.HasNextPage);
            Assert.Equal(hasPreviousPage, pageInfo.HasPreviousPage);
            Assert.Equal(startCursor, pageInfo.StartCursor);
            Assert.Equal(endCursor, pageInfo.EndCursor);
            Assert.Equal(totalCount, pageInfo.TotalCount);
        }
    }
}
