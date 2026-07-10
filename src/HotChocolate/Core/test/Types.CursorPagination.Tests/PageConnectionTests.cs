using GreenDonut.Data;

namespace HotChocolate.Types.Pagination;

public class PageConnectionTests
{
    [Fact]
    public void ImplicitConversion_Should_WrapPage_When_ConvertingPageToConnection()
    {
        // arrange
        var page = Page<string>.Create(
            ["a", "b", "c"],
            hasNextPage: true,
            hasPreviousPage: false,
            item => item,
            totalCount: 10);

        // act
        PageConnection<string> connection = page;

        // assert
        Assert.Same(page, connection.Nodes);
        Assert.Equal(10, connection.TotalCount);
        Assert.Equal(3, connection.Edges!.Count);
        Assert.True(connection.PageInfo.HasNextPage);
    }

    [Fact]
    public void ImplicitConversion_Should_ThrowArgumentNullException_When_PageIsNull()
    {
        // arrange
        Page<string>? page = null;

        // act
        PageConnection<string> Convert() => page!;

        // assert
        Assert.Throws<ArgumentNullException>(Convert);
    }
}
