using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Sorting;

public readonly struct SortProviderContext(
    IServiceProvider schemaServices,
    ISortProvider sortProvider,
    IConventionContext conventionContext,
    IDescriptorContext descriptorContext,
    ISortConvention sortConvention,
    ITypeInspector typeInspector,
    InputParser inputParser)
{
    public IServiceProvider SchemaServices { get; } = schemaServices;
    
    public ISortProvider SortProvider { get; } = sortProvider;

    public IConventionContext ConventionContext { get; } = conventionContext;

    public IDescriptorContext DescriptorContext { get; } = descriptorContext;

    public ISortConvention SortConvention { get; } = sortConvention;

    public ITypeInspector TypeInspector { get; } = typeInspector;

    public InputParser InputParser { get; } = inputParser;
}
