using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Scheduling;

/// <summary>
/// Provides convenience methods on <see cref="IDispatchContext"/> for scheduling control.
/// </summary>
public static class SchedulingDispatchContextExtensions
{
    /// <summary>
    /// Marks the current dispatch context to bypass the scheduler, causing the message to be sent
    /// directly to the transport.
    /// </summary>
    /// <param name="context">The dispatch context to modify.</param>
    public static void SkipScheduler(this IDispatchContext context)
    {
        var feature = context.Features.GetOrSet<SchedulingMiddlewareFeature>();

        feature.SkipScheduler = true;
    }
}
