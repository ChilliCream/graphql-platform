using Xunit;

namespace GreenDonut;

public class CancellationTokenSourceExtensionsTests
{
    [Fact(DisplayName = "CreateLinkedCancellationToken: Should throw an argument null token if source is null")]
    public void CreateLinkedCancellationTokenSourceNull()
    {
        // arrange
        CancellationTokenSource source = null!;
        var token = new CancellationTokenSource().Token;

        // act
        Action verify = () => source.CreateLinkedCancellationToken(token);

        // assert
        Assert.Throws<ArgumentNullException>("source", verify);
    }

    [Fact(DisplayName = "CreateLinkedCancellationToken: Should return the token of source if token is equal none")]
    public void CreateLinkedCancellationTokenNone()
    {
        // arrange
        var source = new CancellationTokenSource();
        var token = CancellationToken.None;

        // act
        var combinedToken = source
            .CreateLinkedCancellationToken(token);

        // assert
        Assert.Equal(source.Token, combinedToken);
    }

    [Fact(DisplayName = "CreateLinkedCancellationToken: Should return a combined token")]
    public void CreateLinkedCancellationToken()
    {
        // arrange
        var source = new CancellationTokenSource();
        var token = new CancellationTokenSource().Token;

        // act
        var combinedToken = source
            .CreateLinkedCancellationToken(token);

        // assert
        Assert.NotEqual(source.Token, combinedToken);
        Assert.NotEqual(token, combinedToken);
    }
}
