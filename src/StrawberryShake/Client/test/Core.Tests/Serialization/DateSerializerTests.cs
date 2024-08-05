namespace StrawberryShake.Serialization;

public class DateSerializerTests
{
    private DateSerializer Serializer { get; } = new();

    private DateSerializer CustomSerializer { get; } = new("Abc");

    [Fact]
    public void Parse()
    {
        // arrange
        var value = "2012-11-29";

        // act
        var result = Serializer.Parse(value);

        // assert
        Assert.Equal(2012, result.Date.Year);
        Assert.Equal(11, result.Date.Month);
        Assert.Equal(29, result.Date.Day);
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
        var value = new DateTime(2012, 11, 29);

        // act
        var result = Serializer.Format(value);

        // assert
        Assert.Equal("2012-11-29", result);
    }

    [Fact]
    public void Format_Exception()
    {
        // arrange
        var value = "1";

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
        Assert.Equal("Date", typeName);
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
