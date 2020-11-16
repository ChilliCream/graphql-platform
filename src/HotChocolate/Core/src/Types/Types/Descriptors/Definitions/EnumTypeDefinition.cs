using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class EnumTypeDefinition
        : TypeDefinitionBase<EnumTypeDefinitionNode>
    {
        public IBindableList<EnumValueDefinition> Values { get; } =
            new BindableList<EnumValueDefinition>();

        internal override IEnumerable<ILazyTypeConfiguration> GetConfigurations()
        {
            var configs = new List<ILazyTypeConfiguration>();

            configs.AddRange(Configurations);

            foreach (EnumValueDefinition value in Values)
            {
                configs.AddRange(value.Configurations);
            }

            return configs;
        }
    }
}
