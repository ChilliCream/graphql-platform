namespace StrawberryShake.Serialization;

public class DateTimeSerializerTests
{
    private DateTimeSerializer Serializer { get; } = new();

    private DateTimeSerializer CustomSerializer { get; } = new("Abc");

    [Fact]
    public void Parse()
    {
        // arrange
        var value = "2011-08-30T13:22:53.108Z";

        // act
        var result = Serializer.Parse(value);

        // assert
        Assert.Equal(2011, result.Year);
        Assert.Equal(8, result.Month);
        Assert.Equal(30, result.Day);
        Assert.Equal(13, result.Hour);
        Assert.Equal(22, result.Minute);
        Assert.Equal(53, result.Second);
        Assert.Equal(0, result.Offset.Hours);
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
        var value = new DateTimeOffset(2011, 8, 30, 13, 22, 53, 108, TimeSpan.Zero);

        // act
        var result = Serializer.Format(value);

        // assert
        Assert.Equal("2011-08-30T13:22:53.108Z", result);
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
        Assert.Equal("DateTime", typeName);
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
