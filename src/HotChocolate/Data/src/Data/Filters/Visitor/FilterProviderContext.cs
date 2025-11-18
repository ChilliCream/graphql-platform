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
    public IServiceProvider SchemaServices => schemaServices;

    public IFilterProvider FilterProvider => filterProvider;

    public IConventionContext ConventionContext => conventionContext;

    public IDescriptorContext DescriptorContext => descriptorContext;

    public IFilterConvention FilterConvention => filterConvention;

    public ITypeConverter TypeConverter => typeConverter;

    public ITypeInspector TypeInspector => typeInspector;

    public InputParser InputParser => inputParser;

    public InputFormatter InputFormatter => inputFormatter;
}
