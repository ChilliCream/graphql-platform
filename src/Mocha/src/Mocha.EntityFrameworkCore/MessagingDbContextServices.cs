using Mocha.Outbox;
using Mocha.Scheduling;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// The application singletons that the messaging interceptors and the outbox of a DbContext use.
/// </summary>
/// <param name="outboxSignal">The outbox signal of the application, or <c>null</c> when it has no outbox.</param>
/// <param name="schedulerSignal">The scheduler signal of the application, or <c>null</c> when it has no scheduler.</param>
/// <param name="timeProvider">The time provider of the application.</param>
internal sealed class MessagingDbContextServices(
    IOutboxSignal? outboxSignal,
    ISchedulerSignal? schedulerSignal,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Gets the outbox signal of the application, or <c>null</c> when the application has no outbox.
    /// </summary>
    public IOutboxSignal? OutboxSignal { get; } = outboxSignal;

    /// <summary>
    /// Gets the scheduler signal of the application, or <c>null</c> when the application has no scheduler.
    /// </summary>
    public ISchedulerSignal? SchedulerSignal { get; } = schedulerSignal;

    /// <summary>
    /// Gets the time provider of the application.
    /// </summary>
    public TimeProvider TimeProvider { get; } = timeProvider;
}
