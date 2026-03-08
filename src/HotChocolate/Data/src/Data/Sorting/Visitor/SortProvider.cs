using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// A <see cref="SortProvider{TContext}"/> translates an incoming query to another
/// object structure at runtime
/// </summary>
/// <typeparam name="TContext">The type of the context</typeparam>
public abstract class SortProvider<TContext>
    : Convention<SortProviderConfiguration>
    , ISortProvider
    , ISortProviderConvention
    where TContext : ISortVisitorContext
{
    private readonly List<ISortFieldHandler<TContext>> _fieldHandlers = [];
    private readonly List<ISortOperationHandler<TContext>> _operationHandlers = [];

    private Action<ISortProviderDescriptor<TContext>>? _configure;
    private ISortConvention? _sortConvention;

    protected SortProvider()
    {
        _configure = Configure;
    }

    protected SortProvider(Action<ISortProviderDescriptor<TContext>> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    internal new SortProviderConfiguration? Configuration => base.Configuration;

    /// <inheritdoc />
    public IReadOnlyCollection<ISortFieldHandler> FieldHandlers => _fieldHandlers;

    /// <inheritdoc />
    public IReadOnlyCollection<ISortOperationHandler> OperationHandlers => _operationHandlers;

    public new void Initialize(IConventionContext context)
    {
        base.Initialize(context);
    }

    /// <inheritdoc />
    protected override SortProviderConfiguration CreateConfiguration(IConventionContext context)
    {
        if (_configure is null)
        {
            throw new InvalidOperationException(SortProvider_NoConfigurationSpecified);
        }

        var descriptor = SortProviderDescriptor<TContext>.New();

        _configure(descriptor);
        _configure = null;

        return descriptor.CreateConfiguration();
    }

    void ISortProviderConvention.Initialize(IConventionContext context, ISortConvention convention)
    {
        _sortConvention = convention;
        base.Initialize(context);
    }

    void ISortProviderConvention.Complete(IConventionContext context)
    {
        Complete(context);
    }

    /// <inheritdoc />
    protected internal override void Complete(IConventionContext context)
    {
        if (Configuration!.FieldHandlerConfigurations.Count == 0)
        {
            throw SortProvider_NoFieldHandlersConfigured(this);
        }

        if (Configuration.OperationHandlerConfigurations.Count == 0)
        {
            throw SortProvider_NoOperationHandlersConfigured(this);
        }

        if (_sortConvention is null)
        {
            throw SortConvention_ProviderHasToBeInitializedByConvention(
                GetType(),
                context.Scope);
        }

        var providerContext = new SortProviderContext(
            context.Services,
            this,
            context,
            context.DescriptorContext,
            _sortConvention,
            context.DescriptorContext.TypeInspector,
            context.DescriptorContext.InputParser);

        foreach (var handlerConfiguration in Configuration.FieldHandlerConfigurations)
        {
            try
            {
                var handler = handlerConfiguration.Create<TContext>(providerContext);

                _fieldHandlers.Add(handler);
            }
            catch (Exception exception)
            {
                throw SortProvider_UnableToCreateFieldHandler(this, exception);
            }
        }

        foreach (var operationHandlerConfiguration in Configuration.OperationHandlerConfigurations)
        {
            try
            {
                var handler = operationHandlerConfiguration.Create<TContext>(providerContext);

                _operationHandlers.Add(handler);
            }
            catch (Exception exception)
            {
                throw SortProvider_UnableToCreateOperationHandler(this, exception);
            }
        }
    }

    /// <summary>
    /// This method is called on initialization of the provider but before the provider is
    /// completed. The default implementation of this method does nothing. It can be overridden
    /// by a derived class such that the provider can be further configured before it is
    /// completed
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor that can be used to configure the provider
    /// </param>
    protected virtual void Configure(ISortProviderDescriptor<TContext> descriptor) { }

    /// <summary>
    /// Creates the executor that is attached to the middleware pipeline of the field
    /// </summary>
    /// <param name="argumentName">
    /// The argument name specified in the <see cref="SortConvention"/>
    /// </param>
    /// <typeparam name="TEntityType">The runtime type of the entity</typeparam>
    /// <returns>A middleware</returns>
    public abstract IQueryBuilder CreateBuilder<TEntityType>(string argumentName);

    /// <summary>
    /// Is called on each field that sorting is applied to. This method can be used to
    /// customize a field.
    /// </summary>
    /// <param name="argumentName">
    /// The argument name specified in the <see cref="SortConvention"/>
    /// </param>
    /// <param name="descriptor">The descriptor of the field</param>
    public virtual void ConfigureField(string argumentName, IObjectFieldDescriptor descriptor)
    {
    }

    public virtual ISortMetadata? CreateMetaData(
        ITypeCompletionContext context,
        ISortInputTypeConfiguration typeConfiguration,
        ISortFieldConfiguration fieldConfiguration)
        => null;
}
