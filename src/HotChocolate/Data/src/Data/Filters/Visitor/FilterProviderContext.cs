using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters;

public readonly struct FilterProviderContext(
    IServiceProvider schemaServices,
    IFilterProvider filterProvider,
    IConventionContext conventionContext,
    IDescriptorContext descriptorContext,
    IFilterConvention filterConvention,
    ITypeConverter typeConverter,
    ITypeInspector typeInspector,
    InputParser inputParser,
    InputFormatter inputFormatter)

{
    public IServiceProvider SchemaServices { get; } = schemaServices;

    public IFilterProvider FilterProvider { get; } = filterProvider;

    public IConventionContext ConventionContext { get; } = conventionContext;

    public IDescriptorContext DescriptorContext { get; } = descriptorContext;

    public IFilterConvention FilterConvention { get; } = filterConvention;

    public ITypeConverter TypeConverter { get; } = typeConverter;

    public ITypeInspector TypeInspector { get; } = typeInspector;

    public InputParser InputParser { get; } = inputParser;

    public InputFormatter InputFormatter { get; } = inputFormatter;
}
