namespace GreenDonut.Data;

public class PagingArgumentsTests
{
    [Fact]
    public void IncludeItems_Should_BeTrue_When_Default()
    {
        // arrange
        var arguments = default(PagingArguments);

        // act
        var includeItems = arguments.IncludeItems;

        // assert
        Assert.True(includeItems);
    }

    [Fact]
    public void IncludeItems_Should_BeTrue_When_CreatedWithParameterlessNew()
    {
        // arrange
        var arguments = new PagingArguments();

        // act
        var includeItems = arguments.IncludeItems;

        // assert
        Assert.True(includeItems);
    }

    [Fact]
    public void IncludeItems_Should_BeTrue_When_CreatedWithObjectInitializer()
    {
        // arrange
        var arguments = new PagingArguments { First = 5 };

        // act
        var includeItems = arguments.IncludeItems;

        // assert
        Assert.True(includeItems);
    }

    [Fact]
    public void IncludeItems_Should_BeTrue_When_CreatedWithConstructor()
    {
        // arrange
        var arguments = new PagingArguments(first: 5);

        // act
        var includeItems = arguments.IncludeItems;

        // assert
        Assert.True(includeItems);
    }

    [Fact]
    public void IncludeItems_Should_BeFalse_When_SetToFalseWithWith()
    {
        // arrange
        var arguments = new PagingArguments(first: 5) with { IncludeItems = false };

        // act
        var includeItems = arguments.IncludeItems;

        // assert
        Assert.False(includeItems);
    }

    [Fact]
    public void Equality_Should_DistinguishIncludeItems_When_OtherwiseEqual()
    {
        // arrange
        var withItemsExcluded = new PagingArguments { IncludeItems = false };
        var withItemsIncluded = new PagingArguments();

        // act
        var areEqual = withItemsExcluded == withItemsIncluded;

        // assert
        Assert.False(areEqual);
    }
}
