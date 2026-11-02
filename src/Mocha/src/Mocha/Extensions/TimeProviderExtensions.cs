using Microsoft.Extensions.DependencyInjection;

namespace Mocha;

internal static class TimeProviderExtensions
{
    public static TimeProvider GetTimeProvider(this IServiceProvider serviceProvider)
    {
        var timeProvider = serviceProvider.GetService<TimeProvider>();

        return timeProvider ?? TimeProvider.System;
    }
}
