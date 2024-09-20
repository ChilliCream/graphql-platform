using IOPath = System.IO.Path;

namespace HotChocolate.PersistedOperations.FileSystem;

public class DefaultOperationDocumentFileMapTests
{
    [Fact]
    public void Root_Must_Not_Be_Null()
    {
        // arrange
        // act
        var map = new DefaultOperationDocumentFileMap();

        // assert
        Assert.NotNull(map.Root);
    }

    [Fact]
    public void MapToFilePath_Convert_Base64_To_UrlCompatibleBase64()
    {
        // arrange
        var map = new DefaultOperationDocumentFileMap();

        // act
        var path = map.MapToFilePath("/+=");

        // assert
        Assert.Equal(IOPath.Combine(map.Root, "-_.graphql"), path);
    }

    [Fact]
    public void MapToFilePath_Convert_Base64_To_UrlCompatibleBase64_2()
    {
        // arrange
        var map = new DefaultOperationDocumentFileMap();

        // act
        var path = map.MapToFilePath("/+=========");

        // assert
        Assert.Equal(IOPath.Combine(map.Root, "-_.graphql"), path);
    }

    [Fact]
    public void MapToFilePath()
    {
        // arrange
        var map = new DefaultOperationDocumentFileMap();

        // act
        var path = map.MapToFilePath("abc_def");

        // assert
        Assert.Equal(IOPath.Combine(map.Root, "abc_def.graphql"), path);
    }

    [InlineData(null)]
    [InlineData("")]
    [Theory]
    public void MapToFilePath_QueryId_Is_Null(string? queryId)
    {
        // arrange
        var map = new DefaultOperationDocumentFileMap();

        // act
        void Action() => map.MapToFilePath(queryId!);

        // assert
        Assert.Throws<ArgumentNullException>((Action) Action);
    }
}
