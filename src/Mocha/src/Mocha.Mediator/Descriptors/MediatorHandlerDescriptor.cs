namespace Mocha.Mediator;

/// <summary>
/// Provides a fluent API for configuring a mediator handler during setup.
/// Auto-detects handler kind by inspecting the handler type's interfaces.
/// </summary>
public class MediatorHandlerDescriptor
    : MediatorDescriptorBase<MediatorHandlerConfiguration>
    , IMediatorHandlerDescriptor
{
    /// <summary>
    /// Creates a new handler descriptor that auto-detects handler metadata
    /// from the specified handler type's interfaces.
    /// </summary>
    /// <param name="context">The mediator configuration context.</param>
    /// <param name="handlerType">The concrete handler implementation type.</param>
    public MediatorHandlerDescriptor(IMediatorConfigurationContext context, Type handlerType)
        : base(context)
    {
        ArgumentNullException.ThrowIfNull(handlerType);

        Configuration = new MediatorHandlerConfiguration();
        Configuration.HandlerType = handlerType;
    }

    protected internal override MediatorHandlerConfiguration Configuration { get; protected set; }

    /// <summary>
    /// Builds and returns the finalized <see cref="MediatorHandlerConfiguration"/> from this
    /// descriptor's accumulated settings. If no configuration was applied via
    /// <see cref="MediatorDescriptorBase{T}.Extend"/>, auto-detects handler metadata
    /// from the handler type's interfaces.
    /// </summary>
    public MediatorHandlerConfiguration CreateConfiguration()
    {
        if (Configuration.MessageType is null)
        {
            DetectHandler(Configuration.HandlerType!);
        }

        return Configuration;
    }

    private void DetectHandler(Type handlerType)
    {
        foreach (var @interface in handlerType.GetInterfaces())
        {
            if (!@interface.IsGenericType)
            {
                continue;
            }

            var genericDef = @interface.GetGenericTypeDefinition();

            if (genericDef == typeof(ICommandHandler<,>))
            {
                var args = @interface.GetGenericArguments();
                Configuration.MessageType = args[0];
                Configuration.ResponseType = args[1];
                Configuration.Kind = MediatorHandlerKind.CommandResponse;
                return;
            }

            if (genericDef == typeof(ICommandHandler<>))
            {
                Configuration.MessageType = @interface.GetGenericArguments()[0];
                Configuration.Kind = MediatorHandlerKind.Command;
                return;
            }

            if (genericDef == typeof(IQueryHandler<,>))
            {
                var args = @interface.GetGenericArguments();
                Configuration.MessageType = args[0];
                Configuration.ResponseType = args[1];
                Configuration.Kind = MediatorHandlerKind.Query;
                return;
            }

            if (genericDef == typeof(INotificationHandler<>))
            {
                Configuration.MessageType = @interface.GetGenericArguments()[0];
                Configuration.Kind = MediatorHandlerKind.Notification;
                return;
            }
        }

        throw new InvalidOperationException(
            $"Type '{handlerType}' does not implement any known mediator handler interface.");
    }
}
