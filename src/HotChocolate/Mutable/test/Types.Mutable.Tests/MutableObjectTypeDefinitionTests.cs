namespace HotChocolate.Types.Mutable;

public class MutableObjectTypeDefinitionTests
{
    [Fact]
    public void SetDeprecationReason_Should_MarkAsDeprecated()
    {
        // arrange
        var objectType = new MutableObjectTypeDefinition("Foo");

        // act
        objectType.DeprecationReason = "Use Bar.";

        // assert
        Assert.True(objectType.IsDeprecated);
        Assert.Equal("Use Bar.", objectType.DeprecationReason);
    }

    [Fact]
    public void IsDeprecated_Should_ClearDeprecationReason_When_SetToFalse()
    {
        // arrange
        var objectType = new MutableObjectTypeDefinition("Foo")
        {
            DeprecationReason = "Use Bar."
        };

        // act
        objectType.IsDeprecated = false;

        // assert
        Assert.False(objectType.IsDeprecated);
        Assert.Null(objectType.DeprecationReason);
    }
}
