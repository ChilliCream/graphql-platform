using Microsoft.Extensions.DependencyInjection;
using Mocha.Scheduling;

namespace Mocha.EntityFrameworkCore;

internal static class SchedulerSignalServiceProviderExtensions
{
    /// <summary>
    /// Notifies the scheduler of the application with the current time of the application.
    /// </summary>
    public static void NotifyScheduler(this IServiceProvider services)
        => services.GetRequiredService<ISchedulerSignal>().Notify(services.GetTimeProvider().GetUtcNow());
}
