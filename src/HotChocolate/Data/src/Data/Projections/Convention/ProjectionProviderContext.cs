using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections;

public readonly struct ProjectionProviderContext(
    IServiceProvider schemaServices,
    IConventionContext conventionContext,
    IDescriptorContext descriptorContext,
    ITypeInspector iTypeInspector)
{
    public IServiceProvider SchemaServices { get; } = schemaServices;

    public IConventionContext ConventionContext { get; } = conventionContext;

    public IDescriptorContext DescriptorContext { get; } = descriptorContext;

    public ITypeInspector ITypeInspector { get; } = iTypeInspector;
}
