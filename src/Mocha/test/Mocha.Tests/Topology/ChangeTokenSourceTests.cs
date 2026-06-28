using Microsoft.Extensions.Primitives;

namespace Mocha.Tests.Topology;

public sealed class ChangeTokenSourceTests
{
    [Fact]
    public void Current_Should_ReturnUnchangedToken_When_Created()
    {
        // arrange
        var source = new ChangeTokenSource();

        // act
        var token = source.Current;

        // assert
        Assert.False(token.HasChanged);
        Assert.True(token.ActiveChangeCallbacks);
    }

    [Fact]
    public void Rotate_Should_FirePreviousToken_When_Called()
    {
        // arrange
        var source = new ChangeTokenSource();
        var token = source.Current;
        var fired = false;
        token.RegisterChangeCallback(_ => fired = true, null);

        // act
        source.Rotate();

        // assert
        Assert.True(fired);
        Assert.True(token.HasChanged);
    }

    [Fact]
    public void Rotate_Should_ExposeNextTokenBeforeCallback_When_CallbackRuns()
    {
        // arrange
        var source = new ChangeTokenSource();
        var token = source.Current;
        IChangeToken? nextToken = null;
        token.RegisterChangeCallback(_ => nextToken = source.Current, null);

        // act
        source.Rotate();

        // assert
        Assert.NotNull(nextToken);
        Assert.NotSame(token, nextToken);
        Assert.False(nextToken!.HasChanged);
    }

    [Fact]
    public void Rotate_Should_LeaveNewCurrentTokenUnchanged_When_PreviousTokenIsCanceled()
    {
        // arrange
        var source = new ChangeTokenSource();
        source.Rotate();
        var token = source.Current;
        var fired = false;
        token.RegisterChangeCallback(_ => fired = true, null);

        // act
        source.Rotate();

        // assert
        Assert.True(fired);
        Assert.True(token.HasChanged);
        Assert.False(source.Current.HasChanged);
    }
}
