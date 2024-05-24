using HotChocolate.Execution.Processing;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ErrorHelper;
using static HotChocolate.Data.ThrowHelper;
using static Microsoft.Extensions.DependencyInjection.ActivatorUtilities;

namespace HotChocolate.Data.Projections;

/// <summary>
/// A <see cref="ProjectionProvider"/> translates a incoming query to another
/// object structure at runtime
/// </summary>
public abstract class ProjectionProvider
    : Convention<ProjectionProviderDefinition>
    , IProjectionProvider
    , IProjectionProviderConvention
{
    private Action<IProjectionProviderDescriptor>? _configure;

    private readonly IList<IProjectionFieldHandler> _fieldHandlers =
        new List<IProjectionFieldHandler>();

    private readonly IList<IProjectionFieldInterceptor> _fieldInterceptors =
        new List<IProjectionFieldInterceptor>();

    private readonly IList<IProjectionOptimizer> _optimizer = new List<IProjectionOptimizer>();

    public const string ProjectionContextIdentifier = "ProjectionMiddleware";

    protected ProjectionProvider()
    {
        _configure = Configure;
    }

    public ProjectionProvider(Action<IProjectionProviderDescriptor> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

    internal new ProjectionProviderDefinition? Definition => base.Definition;

    /// <inheritdoc />
    protected override ProjectionProviderDefinition CreateDefinition(
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

        return descriptor.CreateDefinition();
    }

    /// <summary>
    /// This method is called on initialization of the provider but before the provider is
    /// completed. The default implementation of this method does nothing. It can be overriden
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
        if (Definition!.Handlers.Count == 0)
        {
            throw ProjectionProvider_NoHandlersConfigured(this);
        }

        var services = new CombinedServiceProvider(
            new DictionaryServiceProvider(
                (typeof(IConventionContext), context),
                (typeof(IDescriptorContext), context.DescriptorContext),
                (typeof(ITypeInspector), context.DescriptorContext.TypeInspector)),
            context.Services);

        foreach (var (type, instance) in Definition.Handlers)
        {
            if (instance is not null)
            {
                _fieldHandlers.Add(instance);
                continue;
            }

            try
            {
                var field = (IProjectionFieldHandler)GetServiceOrCreateInstance(services, type);
                _fieldHandlers.Add(field);
            }
            catch
            {
                throw new SchemaException(
                    ProjectionConvention_UnableToCreateFieldHandler(this, type));
            }
        }

        foreach (var (type, instance) in Definition.Interceptors)
        {
            if (instance is not null)
            {
                _fieldInterceptors.Add(instance);
                continue;
            }

            try
            {
                var field = (IProjectionFieldInterceptor)GetServiceOrCreateInstance(services, type);
                _fieldInterceptors.Add(field);
            }
            catch
            {
                throw new SchemaException(
                    ProjectionConvention_UnableToCreateFieldHandler(this, type));
            }
        }

        foreach (var (type, instance) in Definition.Optimizers)
        {
            if (instance is not null)
            {
                _optimizer.Add(instance);
                continue;
            }

            try
            {
                var optimizers = (IProjectionOptimizer)GetServiceOrCreateInstance(services, type);
                _optimizer.Add(optimizers);
            }
            catch
            {
                throw new SchemaException(
                    ProjectionConvention_UnableToCreateFieldHandler(this, type));
            }
        }
    }

    public Selection RewriteSelection(
        SelectionSetOptimizerContext context,
        Selection selection)
    {
        for (var i = 0; i < _optimizer.Count; i++)
        {
            if (_optimizer[i].CanHandle(selection))
            {
                selection = _optimizer[i].RewriteSelection(context, selection);
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

                return ProjectionSelection.From(
                    selection,
                    fieldHandler);
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
