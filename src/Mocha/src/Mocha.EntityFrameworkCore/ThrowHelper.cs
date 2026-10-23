namespace Mocha.EntityFrameworkCore;

internal static partial class ThrowHelper
{
    public static InvalidOperationException OutboxNotConfigured(Type type)
        => new($"The DbContext '{type.FullName}' has no outbox. "
            + $"Register an outbox for it with AddEntityFramework<{type.Name}>(), "
            + "for example by calling UsePostgresOutbox().");

    public static InvalidOperationException OutboxNotAttached(Type type)
        => new($"The outbox for '{type.Name}' is not attached to a messaging scope. "
            + "Call db.UseOutbox(bus) before publishing.");

    public static InvalidOperationException OutboxAlreadyAttached()
        => new("This DbContext is already attached to a different messaging scope.");

    public static InvalidOperationException OutboxProviderNotSupported(Type type)
        => new($"The outbox provider for '{type.Name}' does not support DbContext staging. "
            + "Register an IDbContextOutboxProvider with the DbContext's messaging services.");

    public static InvalidOperationException OutboxBusNotSupported(Type type)
        => new($"The message bus '{type.Name}' does not support preparing messages for a DbContext outbox.");

    public static InvalidOperationException ConcurrentOutboxOperation()
        => new("A message is already being prepared for this DbContext. "
            + "Await the operation before publishing another message or saving changes.");

    public static ArgumentException ScheduledOutboxMessage(string parameterName)
        => new("DbContext.Outbox does not support scheduled messages.", parameterName);
}
