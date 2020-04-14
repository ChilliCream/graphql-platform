using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Sorting.Conventions;

namespace HotChocolate.Types.Sorting
{
    public class SortOperationKindType
        : EnumType<SortOperationKind>
    {
        protected override void Configure(IEnumTypeDescriptor<SortOperationKind> descriptor)
        {
            base.Configure(descriptor);
            descriptor.Value(SortOperationKind.Asc);
            descriptor.Value(SortOperationKind.Desc);
        }

        protected override EnumTypeDefinition CreateDefinition(IInitializationContext context)
        {
            EnumTypeDefinition definition = base.CreateDefinition(context);
            ISortingConvention? convention =
                context.DescriptorContext.GetSortingConvention();

            definition.Name = convention.GetOperationKindTypeName(
                context.DescriptorContext, definition.ClrType);

            foreach (EnumValueDefinition value in definition.Values)
            {
                ConfigureEnumValue(value, convention);
            }

            return definition;
        }

        private void ConfigureEnumValue(
            EnumValueDefinition definition,
            ISortingConvention convention)
        {
            switch (definition.Value)
            {
                case SortOperationKind.Asc:
                    definition.Name = convention.GetAscendingName();
                    break;
                case SortOperationKind.Desc:
                    definition.Name = convention.GetDescendingName();
                    break;
            }
        }
    }
}
