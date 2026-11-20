namespace Mocha.Outbox;

internal static class ThrowHelper
{
    public static InvalidOperationException SkipOutboxNotSupportedForDbContextOutbox()
        => new($"{nameof(OutboxDispatchContextExtensions.SkipOutbox)}() cannot be used for a message published "
            + "or sent through dbContext.Outbox(bus), because the message would be sent before the DbContext "
            + "transaction commits.");

    public static InvalidOperationException EnvelopeNotSet()
        => new("Envelope is not set");
}
