using HotChocolate.Execution.Processing;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Data.ThrowHelper;
using static Microsoft.Extensions.DependencyInjection.ActivatorUtilities;

namespace HotChocolate.Data.Projections;

public class ProjectionConvention
    : Convention<ProjectionConventionDefinition>
    , IProjectionConvention
{
    private Action<IProjectionConventionDescriptor>? _configure;
    private IProjectionProvider _provider = default!;

    public const string IsProjectedKey = nameof(IsProjectedKey);
    public const string AlwaysProjectedFieldsKey = nameof(AlwaysProjectedFieldsKey);

    protected ProjectionConvention()
    {
        _configure = Configure;
    }

    public ProjectionConvention(Action<IProjectionConventionDescriptor> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

    internal new ProjectionConventionDefinition? Definition => base.Definition;

    protected override ProjectionConventionDefinition CreateDefinition(
        IConventionContext context)
    {
        if (_configure is null)
        {
            throw new InvalidOperationException(
                DataResources.ProjectionConvention_NoConfigurationSpecified);
        }

        var descriptor = ProjectionConventionDescriptor.New(
            context.DescriptorContext,
            context.Scope);

        _configure(descriptor);
        _configure = null;

        return descriptor.CreateDefinition();
    }

    protected virtual void Configure(IProjectionConventionDescriptor descriptor)
    {
    }

    protected internal override void Complete(IConventionContext context)
    {
        if (Definition?.Provider is null)
        {
            throw ProjectionConvention_NoProviderFound(GetType(), Definition?.Scope);
        }

        if (Definition.ProviderInstance is null)
        {
            _provider =
                (IProjectionProvider)GetServiceOrCreateInstance(context.Services, Definition.Provider) ??
                throw ProjectionConvention_NoProviderFound(GetType(), Definition.Scope);
        }
        else
        {
            _provider = Definition.ProviderInstance;
        }

        if (_provider is IProjectionProviderConvention init)
        {
            var extensions =
                CollectExtensions(context.Services, Definition);
            init.Initialize(context);
            MergeExtensions(context, init, extensions);
            init.Complete(context);
        }
    }

    public IQueryBuilder CreateBuilder<TEntityType>() =>
        _provider.CreateBuilder<TEntityType>();

    public ISelectionSetOptimizer CreateOptimizer() =>
        new ProjectionOptimizer(_provider);

    private static IReadOnlyList<IProjectionProviderExtension> CollectExtensions(
        IServiceProvider serviceProvider,
        ProjectionConventionDefinition definition)
    {
        List<IProjectionProviderExtension> extensions = [];
        extensions.AddRange(definition.ProviderExtensions);

        foreach (var extensionType in definition.ProviderExtensionsTypes)
        {
            extensions.Add((IProjectionProviderExtension)GetServiceOrCreateInstance(serviceProvider, extensionType));
        }

        return extensions;
    }

    private static void MergeExtensions(
        IConventionContext context,
        IProjectionProviderConvention provider,
        IReadOnlyList<IProjectionProviderExtension> extensions)
    {
        if (provider is Convention providerConvention)
        {
            for (var m = 0; m < extensions.Count; m++)
            {
                if (extensions[m] is IProjectionProviderConvention extensionConvention)
                {
                    extensionConvention.Initialize(context);
                    extensions[m].Merge(context, providerConvention);
                    extensionConvention.Complete(context);
                }
            }
        }
    }
}
