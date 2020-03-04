using System.Collections.Generic;
using System.Collections.Immutable;
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
            var configs = ImmutableList<ILazyTypeConfiguration>.Empty;

            if (Configurations.Count > 0)
            {
                configs = configs.AddRange(Configurations);
            }

            foreach (EnumValueDefinition value in Values)
            {
                if (value.Configurations.Count > 0)
                {
                    configs = configs.AddRange(value.Configurations);
                }
            }

            return configs;
        }
    }
}
