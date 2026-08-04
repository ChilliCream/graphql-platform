using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Scheduling;

namespace Mocha.Tests;

public class SchedulingDispatchContextExtensionsTests
{
    [Fact]
    public void SkipScheduler_Should_SetFeatureFlag_When_Called()
    {
        // arrange
        var context = new DispatchContext();

        // act
        context.SkipScheduler();

        // assert
        var feature = context.Features.GetOrSet<SchedulingMiddlewareFeature>();
        Assert.True(feature.SkipScheduler);
    }
}
