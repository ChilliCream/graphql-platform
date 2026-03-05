using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;

namespace Mocha.Middlewares;

/// <summary>
/// Resolves the runtime message type used for deserialization and routing.
/// </summary>
/// <remarks>
/// Selection first uses the envelope message identity, then falls back to enclosed types to support
/// polymorphic contracts when the declared identity is unknown.
/// Without this step, routing may see an unresolved message type and valid consumers will never be
/// selected for otherwise deserializable payloads.
/// </remarks>
internal sealed class MessageTypeSelectionMiddleware(IMessageTypeRegistry registry)
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        if (context.MessageType is null)
        {
            if (context.Envelope?.MessageType is { } messageIdentity)
            {
                var messageType = registry.GetMessageType(messageIdentity);

                context.MessageType = messageType;
            }
        }

        // Fallback supports compatible subtypes without requiring exact identity matches.
        //
        // TODO i dont really know how this will work with interfaces - specically with
        // deserialization
        if (context.MessageType is null
            && context.Envelope?.EnclosedMessageTypes is { } enclosedMessageTypes)
        {
            foreach (var type in enclosedMessageTypes)
            {
                var enclosedMessageType = registry.GetMessageType(type);

                if (enclosedMessageType is not null)
                {
                    context.MessageType = enclosedMessageType;
                    break;
                }
            }
        }

        await next(context);
    }

    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var registry = context.Services.GetRequiredService<IMessageTypeRegistry>();
                var middleware = new MessageTypeSelectionMiddleware(registry);

                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "MessageTypeSelection");
}
