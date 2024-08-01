namespace HotChocolate.Types;

public class NonNullTypeTests
{
    [Fact]
    public void EnsureInnerTypeIsCorrectlySet()
    {
        // arrange
        var innerType = new StringType();

        // act
        var type = new NonNullType(innerType);

        // assert
        Assert.Equal(innerType, type.Type);
    }

    [Fact]
    public void EnsureNativeTypeIsCorrectlyDetected()
    {
        // act
        var type = new NonNullType(new StringType());

        // assert
        Assert.Equal(typeof(string), type.RuntimeType);
    }

    [Fact]
    public void InnerType_Cannot_Be_A_NonNullType()
    {
        // act
        void Action() => new NonNullType(new NonNullType(new StringType()));

        // assert
        Assert.Throws<ArgumentException>(Action);
    }
}
