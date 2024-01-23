using Xunit;

namespace HotChocolate.Validation.Options;

public class ValidationOptionsTests
{
    [InlineData(5, 5)]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    [Theory]
    public void MaxDepth(int value, int expected)
    {
        var options = new ValidationOptions { MaxAllowedExecutionDepth = value, };
        Assert.Equal(expected, options.MaxAllowedExecutionDepth);
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public void SkipIntrospection(bool value)
    {
        var options = new ValidationOptions { SkipIntrospectionFields = value, };
        Assert.Equal(value, options.SkipIntrospectionFields);
    }

    [InlineData(int.MaxValue, int.MaxValue)]
    [InlineData(20, 20)]
    [InlineData(5, 5)]
    [InlineData(1, 1)]
    [InlineData(0, 5)]
    [Theory]
    public void MaxAllowedErrors(int value, int expected)
    {
        var options = new ValidationOptions { MaxAllowedErrors = value, };
        Assert.Equal(expected, options.MaxAllowedErrors);
    }
}
