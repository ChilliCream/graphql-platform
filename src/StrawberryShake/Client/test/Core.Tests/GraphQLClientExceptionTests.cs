namespace StrawberryShake;

public class GraphQLClientExceptionTests
{
    [Fact]
    public void Constructor_ZeroErrors()
    {
        //arrange
        var errors = Array.Empty<IClientError>();

        //act
        var exception = new GraphQLClientException(errors);

        //assert
        Assert.Equal("Unknown error occurred", exception.Message);
    }

    [Fact]
    public void Constructor_OneError()
    {
        //arrange
        var errors = new IClientError[]
        {
            new ClientError("some message"),
        };

        //act
        var exception = new GraphQLClientException(errors);

        //assert
        Assert.Equal("some message", exception.Message);
    }

    [Fact]
    public void Constructor_TwoErrors()
    {
        //arrange
        var errors = new IClientError[]
        {
            new ClientError("first message"),
            new ClientError("second message"),
        };

        //act
        var exception = new GraphQLClientException(errors);

        //assert
        Assert.Equal(
            "Multiple errors occurred:\n- first message\n- second message",
            exception.Message.Replace("\r", ""));
    }
}
