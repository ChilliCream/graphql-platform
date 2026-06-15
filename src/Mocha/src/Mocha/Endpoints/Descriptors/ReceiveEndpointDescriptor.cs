namespace Mocha;

/// <summary>
/// Base class for receive endpoint descriptors that provides fluent configuration of consumer bindings, error handling, concurrency, and receive middleware.
/// </summary>
/// <typeparam name="T">The receive endpoint configuration type.</typeparam>
public abstract class ReceiveEndpointDescriptor<T>(IMessagingConfigurationContext context)
    : MessagingDescriptorBase<T>(context)
    , IReceiveEndpointDescriptor<T> where T : ReceiveEndpointConfiguration
{
    protected internal override T Configuration { get; protected set; } = null!;

    public IReceiveEndpointDescriptor<T> Handler<THandler>() where THandler : class, IHandler
    {
        Configuration.ConsumerIdentities.Add(typeof(THandler));
        return this;
    }

    public IReceiveEndpointDescriptor<T> Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        Configuration.ConsumerIdentities.Add(typeof(TConsumer));
        return this;
    }

    /// <summary>
    /// Binds a handler to this receive endpoint by its runtime type.
    /// </summary>
    /// <param name="handlerType">The handler type to bind.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    public IReceiveEndpointDescriptor<T> Handler(Type handlerType)
    {
        Configuration.ConsumerIdentities.Add(handlerType);
        return this;
    }

    /// <summary>
    /// Binds a consumer to this receive endpoint by its runtime type.
    /// </summary>
    /// <param name="consumerType">The consumer type to bind.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    public IReceiveEndpointDescriptor<T> Consumer(Type consumerType)
    {
        ArgumentNullException.ThrowIfNull(consumerType);
        Configuration.ConsumerIdentities.Add(consumerType);
        return this;
    }

    /// <inheritdoc />
    public IReceiveEndpointDescriptor<T> Receives<TMessage>()
    {
        Configuration.ReceivedMessageTypes.Add(typeof(TMessage));
        return this;
    }

    /// <inheritdoc />
    public IReceiveEndpointDescriptor<T> Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var descriptor = new ReceiveTypeBindDescriptor();
        configure(descriptor);

        var messageType = typeof(TMessage);
        Configuration.ReceivedMessageTypes.Add(messageType);
        MergeTypeBindIntent(messageType, descriptor);
        return this;
    }

    /// <inheritdoc />
    public IReceiveEndpointDescriptor<T> Receives(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        Configuration.ReceivedMessageTypes.Add(messageType);
        return this;
    }

    /// <inheritdoc />
    public IReceiveEndpointDescriptor<T> AutoBind(bool enabled)
    {
        Configuration.AutoBind = enabled;
        return this;
    }

    private void MergeTypeBindIntent(Type messageType, ReceiveTypeBindDescriptor incoming)
    {
        if (!Configuration.TypeBinds.TryGetValue(messageType, out var existing))
        {
            Configuration.TypeBinds[messageType] = new ReceiveTypeBindIntent(
                messageType,
                incoming.ResolvedAutoBind);
            return;
        }

        // Merge: explicit AutoBind from the incoming descriptor wins over an implied value;
        // if both are null (neither configured), keep null.
        var mergedAutoBind = incoming.ResolvedAutoBind ?? existing.AutoBind;

        Configuration.TypeBinds[messageType] = new ReceiveTypeBindIntent(
            messageType,
            mergedAutoBind);
    }

    public IReceiveEndpointDescriptor<T> Kind(ReceiveEndpointKind kind)
    {
        Configuration.Kind = kind;
        return this;
    }

    public IReceiveEndpointDescriptor<T> MaxConcurrency(int maxConcurrency)
    {
        Configuration.MaxConcurrency = maxConcurrency;
        return this;
    }

    public IReceiveEndpointDescriptor<T> FaultEndpoint(string address)
    {
        Configuration.ErrorEndpoint = new Uri(address);
        return this;
    }

    public IReceiveEndpointDescriptor<T> SkippedEndpoint(string address)
    {
        Configuration.SkippedEndpoint = new Uri(address);
        return this;
    }

    public IReceiveEndpointDescriptor<T> UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        if (before is not null && after is not null)
        {
            throw ThrowHelper.BeforeAndAfterConflict();
        }

        if (before is null && after is null)
        {
            Configuration.ReceiveMiddlewares.Add(configuration);
            return this;
        }

        if (before is not null)
        {
            Configuration.ReceivePipelineModifiers.Prepend(configuration, before);
        }
        else
        {
            Configuration.ReceivePipelineModifiers.Append(configuration, after);
        }

        return this;
    }
}
