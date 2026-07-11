using Microsoft.Extensions.DependencyInjection;

namespace Mocha;

/// <summary>
/// Helpers for consumer-side reply behavior and bus access.
/// </summary>
internal static class ConsumeContextExtensions
{
    /// <summary>
    /// Creates reply options from the incoming message metadata when a response channel is available.
    /// </summary>
    /// <remarks>
    /// Headers are copied so replies and faults remain linked to the original request and downstream
    /// workflows (for example saga headers) keep working. The correlation id is echoed when present,
    /// so callers that correlate by a different mechanism (such as a saga header) are still supported.
    /// </remarks>
    public static bool TryCreateResponseOptions(this IConsumeContext context, out ReplyOptions options)
    {
        options = ReplyOptions.Default;
        var replyTo = context.ResponseAddress;
        if (replyTo is null)
        {
            return false;
        }

        options = new ReplyOptions
        {
            Headers = [],
            CorrelationId = context.CorrelationId,
            ConversationId = context.ConversationId,
            ReplyAddress = replyTo
        };

        foreach (var header in context.Headers)
        {
            options.Headers.Add(header.Key, header.Value);
        }

        return true;
    }

    public static IMessageBus GetBus(this IConsumeContext context)
    {
        return context.Services.GetRequiredService<IMessageBus>();
    }
}
