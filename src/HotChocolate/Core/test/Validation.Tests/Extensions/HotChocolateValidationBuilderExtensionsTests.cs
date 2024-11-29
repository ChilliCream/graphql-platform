using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation.Extensions;

public class HotChocolateValidationBuilderExtensionsTests
{
    [Fact]
    public void AddMaxExecutionDepthRule1_Builder_Is_Null()
    {
        void Fail()
            => HotChocolateValidationBuilderExtensions.AddMaxExecutionDepthRule(null!, 5);
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddMaxExecutionDepthRule2_Builder_Is_Null()
    {
        void Fail()
            => HotChocolateValidationBuilderExtensions.AddMaxExecutionDepthRule(null!, 5, true);
        Assert.Throws<ArgumentNullException>(Fail);
    }
}
