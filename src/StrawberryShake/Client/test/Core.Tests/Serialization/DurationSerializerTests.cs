namespace StrawberryShake.Serialization;

public sealed class DurationSerializerTests
{
    private DurationSerializer Serializer { get; } = new();

    private DurationSerializer CustomSerializer { get; } = new("Abc");

    private DurationSerializer DotNetSerializer { get; } = new(format: DurationFormat.DotNet);

    [Fact]
    public void Parse_Iso8601()
    {
        // arrange
        const string value = "PT1H30M";

        // act
        var result = Serializer.Parse(value);

        // assert
        Assert.Equal(1, result.Hours);
        Assert.Equal(30, result.Minutes);
    }

    [Fact]
    public void Parse_Iso8601_With_Days()
    {
        // arrange
        const string value = "P1DT2H30M45S";

        // act
        var result = Serializer.Parse(value);

        // assert
        Assert.Equal(1, result.Days);
        Assert.Equal(2, result.Hours);
        Assert.Equal(30, result.Minutes);
        Assert.Equal(45, result.Seconds);
    }

    [Fact]
    public void Parse_Iso8601_With_Fractional_Seconds()
    {
        // arrange
        const string value = "PT1H30M45.123456789S";

        // act
        var result = Serializer.Parse(value);

        // assert
        Assert.Equal(1, result.Hours);
        Assert.Equal(30, result.Minutes);
        Assert.Equal(45, result.Seconds);
        Assert.Equal(123, result.Milliseconds);
    }

    [Fact]
    public void Parse_DotNet()
    {
        // arrange
        const string value = "1:30:45";

        // act
        var result = DotNetSerializer.Parse(value);

        // assert
        Assert.Equal(1, result.Hours);
        Assert.Equal(30, result.Minutes);
        Assert.Equal(45, result.Seconds);
    }

    [Fact]
    public void Parse_DotNet_With_Days()
    {
        // arrange
        const string value = "1.02:30:45.1234567";

        // act
        var result = DotNetSerializer.Parse(value);

        // assert
        Assert.Equal(1, result.Days);
        Assert.Equal(2, result.Hours);
        Assert.Equal(30, result.Minutes);
        Assert.Equal(45, result.Seconds);
        Assert.Equal(123, result.Milliseconds);
    }

    [Fact]
    public void Parse_Invalid_Iso8601_Throws()
    {
        // arrange
        const string value = "invalid";

        // act
        void Action() => Serializer.Parse(value);

        // assert
        Assert.Throws<GraphQLClientException>(Action);
    }

    [Fact]
    public void Parse_Invalid_DotNet_Throws()
    {
        // arrange
        const string value = "invalid";

        // act
        void Action() => DotNetSerializer.Parse(value);

        // assert
        Assert.Throws<GraphQLClientException>(Action);
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
    public void Format_Value_Iso8601()
    {
        // arrange
        var value = new TimeSpan(1, 30, 45);

        // act
        var result = Serializer.Format(value);

        // assert
        Assert.Equal("PT1H30M45S", result);
    }

    [Fact]
    public void Format_Value_Iso8601_With_Days()
    {
        // arrange
        var value = new TimeSpan(1, 2, 30, 45);

        // act
        var result = Serializer.Format(value);

        // assert
        Assert.Equal("P1DT2H30M45S", result);
    }

    [Fact]
    public void Format_Value_Iso8601_With_Fractional_Seconds()
    {
        // arrange
        var value = new TimeSpan(0, 1, 30, 45, 123, 456).Add(TimeSpan.FromTicks(7));

        // act
        var result = Serializer.Format(value);

        // assert
        Assert.Equal("PT1H30M45.1234567S", result);
    }

    [Fact]
    public void Format_Value_DotNet()
    {
        // arrange
        var value = new TimeSpan(1, 30, 45);

        // act
        var result = DotNetSerializer.Format(value);

        // assert
        Assert.Equal("01:30:45", result);
    }

    [Fact]
    public void Format_Value_DotNet_With_Days()
    {
        // arrange
        var value = new TimeSpan(1, 2, 30, 45);

        // act
        var result = DotNetSerializer.Format(value);

        // assert
        Assert.Equal("1.02:30:45", result);
    }

    [Fact]
    public void Format_Value_DotNet_With_Fractional_Seconds()
    {
        // arrange
        var value = new TimeSpan(0, 1, 30, 45, 123, 456).Add(TimeSpan.FromTicks(7));

        // act
        var result = DotNetSerializer.Format(value);

        // assert
        Assert.Equal("01:30:45.1234567", result);
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
        Assert.Equal("Duration", typeName);
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
