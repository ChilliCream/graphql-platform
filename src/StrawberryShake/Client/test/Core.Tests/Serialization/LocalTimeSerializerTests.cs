namespace StrawberryShake.Serialization;

public class LocalTimeSerializerTests
{
    private LocalTimeSerializer Serializer { get; } = new();

    private LocalTimeSerializer CustomSerializer { get; } = new("Abc");

    [Fact]
    public void Parse()
    {
        // arrange
        var value = "13:22:53";

        // act
        var result = Serializer.Parse(value);

        // assert
        Assert.Equal(13, result.Hour);
        Assert.Equal(22, result.Minute);
        Assert.Equal(53, result.Second);
    }

    [Fact]
    public void Format_Null()
    {
        // arrange

        // act
        var result = Serializer.Format(null);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void Format_Value()
    {
        // arrange
        var value = new TimeOnly(13, 22, 53);

        // act
        var result = Serializer.Format(value);

        // assert
        Assert.Equal("13:22:53", result);
    }

    [Fact]
    public void Format_Exception()
    {
        // arrange
        var value = 1;

        // act
        void Action() => Serializer.Format(value);

        // assert
        Assert.Equal(
            "SS0007",
            Assert.Throws<GraphQLClientException>(Action).Errors.Single().Code);
    }

    [Fact]
    public void TypeName_Default()
    {
        // arrange

        // act
        var typeName = Serializer.TypeName;

        // assert
        Assert.Equal("LocalTime", typeName);
    }

    [Fact]
    public void TypeName_Custom()
    {
        // arrange

        // act
        var typeName = CustomSerializer.TypeName;

        // assert
        Assert.Equal("Abc", typeName);
    }
}
