namespace HotChocolate.Utilities;

public class ReflectionUtilsTests
{
    [Fact]
    public void GetTypeNameFromGenericType()
    {
        // arrange
        var type = typeof(GenericNonNestedFoo<string>);

        // act
        var typeName = type.GetTypeName();

        // assert
        Assert.Equal(
            "HotChocolate.Utilities.GenericNonNestedFoo<System.String>",
            typeName);
    }

    [Fact]
    public void GetTypeNameFromType()
    {
        // arrange
        var type = typeof(ReflectionUtilsTests);

        // act
        var typeName = type.GetTypeName();

        // assert
        Assert.Equal(
            "HotChocolate.Utilities.ReflectionUtilsTests",
            typeName);
    }

    [Fact]
    public void GetTypeNameFromGenericNestedType()
    {
        // arrange
        var type = typeof(GenericNestedFoo<string>);

        // act
        var typeName = type.GetTypeName();

        // assert
        Assert.Equal(
            "HotChocolate.Utilities.ReflectionUtilsTests"
            + ".GenericNestedFoo<System.String>",
            typeName);
    }

    [Fact]
    public void GetTypeNameFromNestedType()
    {
        // arrange
        var type = typeof(Foo);

        // act
        var typeName = type.GetTypeName();

        // assert
        Assert.Equal(
            "HotChocolate.Utilities.ReflectionUtilsTests.Foo",
            typeName);
    }

    public class GenericNestedFoo<T>(T value)
    {
        public T Value { get; } = value;
    }

    public class Foo(string value)
    {
        public string Value { get; } = value;
    }
}

public class GenericNonNestedFoo<T>(T value)
{
    public T Value { get; } = value;
}
