namespace HotChocolate.Types.Pagination;

public class ConnectionTests
{
    [Fact]
    public void CreateConnection_PageInfoAndEdges_PassedCorrectly()
    {
        // arrange
        var pageInfo = new ConnectionPageInfo(true, true, "a", "b");
        var edges = new List<Edge<string>>();

        // act
        var connection = new Connection(
            edges,
            pageInfo,
            1);

        // assert
        Assert.Equal(pageInfo, connection.Info);
        Assert.Equal(edges, connection.Edges);
    }

    [Fact]
    public void CreateConnection_PageInfoNull_ArgumentNullException()
    {
        // arrange
        var edges = new List<Edge<string>>();

        // act
        void Action() => new Connection<string>(
            edges,
            null!,
            1);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void CreateConnection_EdgesNull_ArgumentNullException()
    {
        // arrange
        var pageInfo = new ConnectionPageInfo(true, true, "a", "b");

        // act
        void Action() => new Connection<string>(
            null!,
            pageInfo,
            1);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void TotalCount_ReturnTotalCount()
    {
        // arrange
        var pageInfo = new ConnectionPageInfo(true, true, "a", "b");
        var edges = new List<Edge<string>>();

        // act
        var connection = new Connection(edges, pageInfo, 2);

        // assert
        Assert.Equal(2, connection.TotalCount);
    }
}
