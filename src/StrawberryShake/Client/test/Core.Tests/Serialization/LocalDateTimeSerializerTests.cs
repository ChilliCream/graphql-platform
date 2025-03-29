namespace StrawberryShake.Serialization;

public class LocalDateTimeSerializerTests
{
    private LocalDateTimeSerializer Serializer { get; } = new();

    private LocalDateTimeSerializer CustomSerializer { get; } = new("Abc");

    [Fact]
    public void Parse()
    {
        // arrange
        var value = "2011-08-30T13:22:53";

        // act
        var result = Serializer.Parse(value);

        // assert
        Assert.Equal(2011, result.Year);
        Assert.Equal(8, result.Month);
        Assert.Equal(30, result.Day);
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
        var value = new DateTime(2011, 8, 30, 13, 22, 53, 108);

        // act
        var result = Serializer.Format(value);

        // assert
        Assert.Equal("2011-08-30T13:22:53", result);
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
        Assert.Equal("LocalDateTime", typeName);
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
