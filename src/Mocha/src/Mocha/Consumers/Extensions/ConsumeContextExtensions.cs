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
    /// Correlation id and headers are copied so replies/faults remain linked to the original request
    /// and downstream workflows (for example saga headers) keep working.
    /// </remarks>
    public static bool TryCreateResponseOptions(this IConsumeContext context, out ReplyOptions options)
    {
        options = ReplyOptions.Default;
        var replyTo = context.ResponseAddress;
        if (replyTo is null)
        {
            return false;
        }

        if (context.CorrelationId is not { } correlationId)
        {
            return false;
        }

        options = new ReplyOptions
        {
            Headers = [],
            CorrelationId = correlationId,
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
