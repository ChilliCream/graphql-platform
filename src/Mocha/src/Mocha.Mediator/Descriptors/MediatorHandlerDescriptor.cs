using System.Diagnostics.CodeAnalysis;

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
    public MediatorHandlerDescriptor(IMediatorConfigurationContext context, Type handlerType) : base(context)
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
#pragma warning disable IL2026
            DetectHandler(Configuration.HandlerType!);
#pragma warning restore IL2026
        }

        return Configuration;
    }

    [RequiresUnreferencedCode(
        "Handler detection uses reflection. Use source-generated AddHandlerConfiguration for AOT compatibility.")]
    private void DetectHandler(Type handlerType)
    {
        var found = false;

        foreach (var @interface in handlerType.GetInterfaces())
        {
            if (!@interface.IsGenericType)
            {
                continue;
            }

            var genericDef = @interface.GetGenericTypeDefinition();

            if (genericDef == typeof(ICommandHandler<,>))
            {
                if (found)
                {
                    throw ThrowHelper.MultipleHandlerInterfaces(handlerType);
                }

                var args = @interface.GetGenericArguments();
                Configuration.MessageType = args[0];
                Configuration.ResponseType = args[1];
                Configuration.Kind = MediatorHandlerKind.CommandResponse;
                found = true;
            }
            else if (genericDef == typeof(ICommandHandler<>))
            {
                if (found)
                {
                    throw ThrowHelper.MultipleHandlerInterfaces(handlerType);
                }

                Configuration.MessageType = @interface.GetGenericArguments()[0];
                Configuration.Kind = MediatorHandlerKind.Command;
                found = true;
            }
            else if (genericDef == typeof(IQueryHandler<,>))
            {
                if (found)
                {
                    throw ThrowHelper.MultipleHandlerInterfaces(handlerType);
                }

                var args = @interface.GetGenericArguments();
                Configuration.MessageType = args[0];
                Configuration.ResponseType = args[1];
                Configuration.Kind = MediatorHandlerKind.Query;
                found = true;
            }
            else if (genericDef == typeof(INotificationHandler<>))
            {
                if (found)
                {
                    throw ThrowHelper.MultipleHandlerInterfaces(handlerType);
                }

                Configuration.MessageType = @interface.GetGenericArguments()[0];
                Configuration.Kind = MediatorHandlerKind.Notification;
                found = true;
            }
        }

        if (!found)
        {
            throw ThrowHelper.HandlerInterfaceNotFound(handlerType);
        }
    }
}
