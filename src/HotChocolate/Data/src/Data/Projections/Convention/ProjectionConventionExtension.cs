using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections;

public class ProjectionConventionExtension
    : ConventionExtension<ProjectionConventionDefinition>
{
    private Action<IProjectionConventionDescriptor>? _configure;

    protected ProjectionConventionExtension()
    {
        _configure = Configure;
    }

    public ProjectionConventionExtension(Action<IProjectionConventionDescriptor> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

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

    protected internal new void Initialize(IConventionContext context)
    {
        base.Initialize(context);
    }

    protected virtual void Configure(IProjectionConventionDescriptor descriptor)
    {
    }

    public override void Merge(IConventionContext context, Convention convention)
    {
        if (convention is ProjectionConvention projectionConvention &&
            Definition is {} ownDefinition &&
            projectionConvention.Definition is {} otherDefinition)
        {
            otherDefinition.ProviderExtensions.AddRange(
                ownDefinition.ProviderExtensions);

            otherDefinition.ProviderExtensionsTypes.AddRange(
                ownDefinition.ProviderExtensionsTypes);

            if (ownDefinition.Provider is not null)
            {
                otherDefinition.Provider = ownDefinition.Provider;
            }

            if (ownDefinition.ProviderInstance is not null)
            {
                otherDefinition.ProviderInstance = ownDefinition.ProviderInstance;
            }
        }
    }
}
