using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Sorting;

[Obsolete("Use HotChocolate.Data.")]
public class SortOperationKindType
    : EnumType<SortOperationKind>
{
    protected override void Configure(IEnumTypeDescriptor<SortOperationKind> descriptor)
    {
        base.Configure(descriptor);
        descriptor.Value(SortOperationKind.Asc);
        descriptor.Value(SortOperationKind.Desc);
    }

    protected override EnumTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        var definition = base.CreateDefinition(context);
        var convention =
            context.DescriptorContext.GetSortingNamingConvention();

        definition.Name = convention.GetSortingOperationKindTypeName(
            context.DescriptorContext, definition.RuntimeType);

        foreach (var value in definition.Values)
        {
            ConfigureEnumValue(value, convention);
        }

        return definition;
    }

    private void ConfigureEnumValue(
        EnumValueDefinition definition,
        ISortingNamingConvention convention)
    {
        switch (definition.RuntimeValue)
        {
            case SortOperationKind.Asc:
                definition.Name = convention.SortKindAscName;
                break;
            case SortOperationKind.Desc:
                definition.Name = convention.SortKindDescName;
                break;
        }
    }
}