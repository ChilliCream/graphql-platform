using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Sorting
{
    public class SortOperationKindType
        : EnumType<SortOperationKind>
    {
        protected override EnumTypeDefinition CreateDefinition(IInitializationContext context)
        {
            var definition = base.CreateDefinition(context);
            var convention = context.DescriptorContext
                .GetConventionOrDefault(SortingNamingConventionBase.Default);

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
            switch (definition.Value)
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
}
