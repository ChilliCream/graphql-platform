namespace HotChocolate.Types.Pagination;

public class CollectionSegmentTests
{
    [Fact]
    public void CreateCollectionSegment_PageInfoAndItems_PassedCorrectly()
    {
        // arrange
        var pageInfo = new CollectionSegmentInfo(true, true);
        var items = new List<string>();

        // act
        var collection = new CollectionSegment(
            items,
            pageInfo,
            1);

        // assert
        Assert.Equal(pageInfo, collection.Info);
        Assert.Equal(items, collection.Items);
    }

    [Fact]
    public void CreateCollectionSegment_PageInfoNull_ArgumentNullException()
    {
        // arrange
        // act
        void Error() => new CollectionSegment<string>([], null!, 1);

        // assert
        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void CreateCollectionSegment_ItemsNull_ArgumentNullException()
    {
        // arrange
        // act
        void Verify() => new CollectionSegment<string>(
            null!,
            new CollectionSegmentInfo(true, true),
            1);

        // assert
        Assert.Throws<ArgumentNullException>(Verify);
    }

    [Fact]
    public void GetTotalCountAsync_Value_ReturnsTotalCount()
    {
        // arrange
        // act
        var collection = new CollectionSegment(
            [],
            new CollectionSegmentInfo(true, true),
            2);

        // assert
        Assert.Equal(2, collection.TotalCount);
    }
}
