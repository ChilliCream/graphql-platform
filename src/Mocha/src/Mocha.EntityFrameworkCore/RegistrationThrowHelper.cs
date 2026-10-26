namespace Mocha.EntityFrameworkCore;

internal static partial class ThrowHelper
{
    public static InvalidOperationException AmbiguousOutboxDbContext()
        => new(
            "Multiple outbox DbContext types are registered. "
            + "Call UseDefaultOutbox<TContext>() to select the context for publishing through IMessageBus, "
            + "or publish through db.Outbox(bus) to select a context instance.");

    public static InvalidOperationException DefaultOutboxDbContextNotRegistered(Type contextType)
        => new($"The default outbox DbContext '{contextType.FullName}' has no registered outbox.");
}
