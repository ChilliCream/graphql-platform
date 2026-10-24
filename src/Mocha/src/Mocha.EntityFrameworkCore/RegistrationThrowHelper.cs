namespace Mocha.EntityFrameworkCore;

internal static partial class ThrowHelper
{
    public static InvalidOperationException MissingMessagingDbContext(Type contextType)
        => new(
            $"The messaging DbContext '{contextType.FullName}' is not registered. "
            + "Register the context or an IDbContextFactory for it before building the service provider.");

    public static InvalidOperationException SingletonOutboxDbContext(Type contextType)
        => new(
            $"The outbox DbContext '{contextType.FullName}' cannot be registered as a singleton. "
            + "Register it with a scoped or transient lifetime.");

    public static InvalidOperationException AmbiguousOutboxDbContext()
        => new(
            "Multiple outbox DbContext types are registered. "
            + "Call UseDefaultOutbox<TContext>() to select the context for publishing through IMessageBus, "
            + "or publish through db.Outbox to select a context instance.");

    public static InvalidOperationException DefaultOutboxDbContextNotRegistered(Type contextType)
        => new($"The default outbox DbContext '{contextType.FullName}' has no registered outbox provider.");
}
