using Microsoft.Extensions.Logging;
using Mocha.Middlewares;

namespace Mocha;

internal static class MessageLogging
{
    public static IDisposable? BeginMessageScope(this ILogger logger, IMessageContext context, LogLevel level)
    {
        if (!logger.IsEnabled(level))
        {
            return null;
        }

        return logger.BeginScope(new Dictionary<string, object?>
        {
            ["InstanceId"] = (context as IExecutionContext)?.Runtime?.Host.InstanceId,
            ["Transport"] = context.Transport?.Name,
            ["Endpoint"] = context.Endpoint?.Name,
            ["SourceAddress"] = context.Endpoint?.Source?.Address,
            ["MessageId"] = context.MessageId,
            ["MessageType"] = context.MessageType?.Identity ?? context.Envelope?.MessageType,
            ["CorrelationId"] = context.CorrelationId,
            ["ConversationId"] = context.ConversationId,
            ["CausationId"] = context.CausationId,
            ["DeliveryCount"] = context.DeliveryCount
        });
    }

    public static IDisposable? BeginMessageScope(this ILogger logger, IDispatchContext context, LogLevel level)
    {
        if (!logger.IsEnabled(level))
        {
            return null;
        }

        return logger.BeginScope(new Dictionary<string, object?>
        {
            ["InstanceId"] = context.Runtime?.Host.InstanceId,
            ["Transport"] = context.Transport?.Name,
            ["Endpoint"] = context.Endpoint?.Name,
            ["DestinationAddress"] = context.DestinationAddress,
            ["MessageId"] = context.Envelope?.MessageId ?? context.MessageId,
            ["MessageType"] = context.Envelope?.MessageType ?? context.MessageType?.Identity,
            ["CorrelationId"] = context.Envelope?.CorrelationId ?? context.CorrelationId,
            ["ConversationId"] = context.Envelope?.ConversationId ?? context.ConversationId,
            ["CausationId"] = context.Envelope?.CausationId ?? context.CausationId
        });
    }
}
