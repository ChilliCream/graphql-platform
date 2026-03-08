namespace HotChocolate.Types;

public class DateTimeOptionsTests
{
    [Fact]
    public void DefaultConstructor_ShouldSetDefaultPrecisions()
    {
        // arrange & act
        var options = new DateTimeOptions();

        // assert
        Assert.Equal(DateTimeOptions.DefaultInputPrecision, options.InputPrecision);
        Assert.Equal(DateTimeOptions.DefaultOutputPrecision, options.OutputPrecision);
    }

    [Fact]
    public void DefaultConstants_ShouldBeCorrect()
    {
        // assert
        Assert.Equal(7, DateTimeOptions.DefaultInputPrecision);
        Assert.Equal(7, DateTimeOptions.DefaultOutputPrecision);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void InputPrecision_ValidValues_ShouldSet(byte precision)
    {
        // arrange & act
        var options = new DateTimeOptions { InputPrecision = precision };

        // assert
        Assert.Equal(precision, options.InputPrecision);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void OutputPrecision_ValidValues_ShouldSet(byte precision)
    {
        // arrange & act
        var options = new DateTimeOptions { OutputPrecision = precision };

        // assert
        Assert.Equal(precision, options.OutputPrecision);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(255)]
    public void InputPrecision_InvalidValues_ShouldThrow(byte precision)
    {
        // arrange & act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(()
            => new DateTimeOptions { InputPrecision = precision });

        // assert
        Assert.Equal("InputPrecision", exception.ParamName);
        Assert.Equal(precision, exception.ActualValue);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(255)]
    public void OutputPrecision_InvalidValues_ShouldThrow(byte precision)
    {
        // arrange & act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(()
            => new DateTimeOptions { OutputPrecision = precision });

        // assert
        Assert.Equal("OutputPrecision", exception.ParamName);
        Assert.Equal(precision, exception.ActualValue);
    }
}
