namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Carries conversation and causation identifiers from a consumer to its outbox publications.
/// </summary>
internal sealed class OutboxMessageCorrelation(string? conversationId, string? causationId)
{
    private static readonly AsyncLocal<OutboxMessageCorrelation?> s_current = new();

    public static ConsumerMiddlewareConfiguration CreateConsumeMiddleware()
        => new(
            static (_, next) => async context =>
            {
                var previous = s_current.Value;
                s_current.Value = new OutboxMessageCorrelation(
                    context.ConversationId,
                    context is IBatchConsumeContext batch ? batch.BatchId : context.MessageId);

                try
                {
                    await next(context);
                }
                finally
                {
                    s_current.Value = previous;
                }
            },
            "OutboxCorrelation");

    public static DispatchMiddlewareConfiguration CreateDispatchMiddleware()
        => new(
            static (_, next) => context =>
            {
                if (OutboxPublishContext.Current is not null && s_current.Value is { } correlation)
                {
                    context.ConversationId = correlation.ConversationId ?? context.ConversationId;
                    context.CausationId ??= correlation.CausationId;
                }

                return next(context);
            },
            "OutboxCorrelation");

    private string? ConversationId { get; } = conversationId;

    private string? CausationId { get; } = causationId;
}
