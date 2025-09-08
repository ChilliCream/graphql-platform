namespace StrawberryShake.Serialization;

public class LocalDateSerializerTests
{
    private LocalDateSerializer Serializer { get; } = new();

    private LocalDateSerializer CustomSerializer { get; } = new("Abc");

    [Fact]
    public void Parse()
    {
        // arrange
        const string value = "2011-08-30";

        // act
        var result = Serializer.Parse(value);

        // assert
        Assert.Equal(2011, result.Year);
        Assert.Equal(8, result.Month);
        Assert.Equal(30, result.Day);
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
        var value = new DateOnly(2011, 8, 30);

        // act
        var result = Serializer.Format(value);

        // assert
        Assert.Equal("2011-08-30", result);
    }

    [Fact]
    public void Format_Exception()
    {
        // arrange
        const int value = 1;

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
        Assert.Equal("LocalDate", typeName);
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
