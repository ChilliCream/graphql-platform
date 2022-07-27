using System;

namespace HotChocolate.Data.Projections;

public class ProjectionConventionDescriptorProxy
    : IProjectionConventionDescriptor
{
    private readonly IProjectionConventionDescriptor _descriptor;

    public ProjectionConventionDescriptorProxy(IProjectionConventionDescriptor descriptor)
    {
        _descriptor = descriptor;
    }

    public IProjectionConventionDescriptor Provider<TProvider>()
        where TProvider : class, IProjectionProvider
        => _descriptor.Provider<TProvider>();

    public IProjectionConventionDescriptor Provider<TProvider>(TProvider provider)
        where TProvider : class, IProjectionProvider
        => _descriptor.Provider(provider);

    public IProjectionConventionDescriptor Provider(Type provider)
        => _descriptor.Provider(provider);

    public IProjectionConventionDescriptor AddProviderExtension<TExtension>()
        where TExtension : class, IProjectionProviderExtension
        => _descriptor.AddProviderExtension<TExtension>();

    public IProjectionConventionDescriptor AddProviderExtension<TExtension>(TExtension provider)
        where TExtension : class, IProjectionProviderExtension
        => _descriptor.AddProviderExtension(provider);
}
