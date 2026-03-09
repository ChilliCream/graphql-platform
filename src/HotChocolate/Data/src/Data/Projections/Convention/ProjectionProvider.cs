using HotChocolate.Execution.Processing;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data.Projections;

/// <summary>
/// A <see cref="ProjectionProvider"/> translates an incoming query to another
/// object structure at runtime
/// </summary>
public abstract class ProjectionProvider
    : Convention<ProjectionProviderConfiguration>
    , IProjectionProvider
    , IProjectionProviderConvention
{
    private Action<IProjectionProviderDescriptor>? _configure;
    private readonly IList<IProjectionFieldHandler> _fieldHandlers = [];
    private readonly IList<IProjectionFieldInterceptor> _fieldInterceptors = [];
    private readonly IList<IProjectionOptimizer> _optimizers = [];

    protected ProjectionProvider()
    {
        _configure = Configure;
    }

    protected ProjectionProvider(Action<IProjectionProviderDescriptor> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

    internal new ProjectionProviderConfiguration? Configuration => base.Configuration;

    /// <inheritdoc />
    protected override ProjectionProviderConfiguration CreateConfiguration(
        IConventionContext context)
    {
        if (_configure is null)
        {
            throw new InvalidOperationException(ProjectionConvention_NoConfigurationSpecified);
        }

        var descriptor = ProjectionProviderDescriptor.New(
            context.DescriptorContext,
            context.Scope);

        _configure(descriptor);
        _configure = null;

        return descriptor.CreateConfiguration();
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
    protected virtual void Configure(IProjectionProviderDescriptor descriptor)
    {
    }

    void IProjectionProviderConvention.Complete(IConventionContext context)
    {
        Complete(context);
    }

    protected internal override void Complete(IConventionContext context)
    {
        if (Configuration!.FieldHandlerConfigurations.Count == 0)
        {
            throw ProjectionProvider_NoHandlersConfigured(this);
        }

        var providerContext = new ProjectionProviderContext(
            context.Services,
            context,
            context.DescriptorContext,
            context.DescriptorContext.TypeInspector);

        foreach (var handlerConfiguration in Configuration.FieldHandlerConfigurations)
        {
            try
            {
                var handler = handlerConfiguration.Create(providerContext);

                _fieldHandlers.Add(handler);
            }
            catch (Exception exception)
            {
                throw new SchemaException(
                    ErrorHelper.ProjectionConvention_UnableToCreateFieldHandler(this, exception));
            }
        }

        foreach (var interceptorConfiguration in Configuration.FieldInterceptorConfigurations)
        {
            try
            {
                var interceptor = interceptorConfiguration.Create(providerContext);

                _fieldInterceptors.Add(interceptor);
            }
            catch (Exception exception)
            {
                throw new SchemaException(
                    ErrorHelper.ProjectionConvention_UnableToCreateFieldHandler(this, exception));
            }
        }

        foreach (var optimizerConfiguration in Configuration.OptimizerConfigurations)
        {
            try
            {
                var optimizer = optimizerConfiguration.Create(providerContext);

                _optimizers.Add(optimizer);
            }
            catch (Exception exception)
            {
                throw new SchemaException(
                    ErrorHelper.ProjectionConvention_UnableToCreateFieldHandler(this, exception));
            }
        }
    }

    public Selection RewriteSelection(
        SelectionSetOptimizerContext context,
        Selection selection)
    {
        for (var i = 0; i < _optimizers.Count; i++)
        {
            if (_optimizers[i].CanHandle(selection))
            {
                selection = _optimizers[i].RewriteSelection(context, selection);
            }
        }

        for (var i = 0; i < _fieldHandlers.Count; i++)
        {
            if (_fieldHandlers[i].CanHandle(selection))
            {
                var fieldHandler = _fieldHandlers[i];

                for (var m = 0; m < _fieldInterceptors.Count; m++)
                {
                    if (_fieldInterceptors[m].CanHandle(selection))
                    {
                        fieldHandler = fieldHandler.Wrap(_fieldInterceptors[m]);
                    }
                }

                var features = context.CreateSelectionFeatures(selection);
                features.SetSafe(fieldHandler);
                return selection;
            }
        }

        return selection;
    }

    void IProjectionProviderConvention.Initialize(IConventionContext context)
        => Initialize(context);

    public new void Initialize(IConventionContext context)
        => base.Initialize(context);

    /// <summary>
    /// Creates the executor that is attached to the middleware pipeline of the field
    /// </summary>
    /// <typeparam name="TEntityType">The runtime type of the entity</typeparam>
    /// <returns>A middleware</returns>
    public abstract IQueryBuilder CreateBuilder<TEntityType>();
}
