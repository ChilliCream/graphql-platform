using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class InterfaceTypeDefinition
        : TypeDefinitionBase<InterfaceTypeDefinitionNode>
    {
        public ResolveAbstractType ResolveAbstractType { get; set; }

        public IBindableList<InterfaceFieldDefinition> Fields { get; } =
            new BindableList<InterfaceFieldDefinition>();

        internal override IEnumerable<ILazyTypeConfiguration> GetConfigurations()
        {
            var configs = ImmutableList<ILazyTypeConfiguration>.Empty;

            if (Configurations.Count > 0)
            {
                configs = configs.AddRange(Configurations);
            }

            foreach (InterfaceFieldDefinition field in Fields)
            {
                if (field.Configurations.Count > 0)
                {
                    configs = configs.AddRange(field.Configurations);
                }

                foreach (ArgumentDefinition argument in field.Arguments)
                {
                    if (argument.Configurations.Count > 0)
                    {
                        configs = configs.AddRange(argument.Configurations);
                    }
                }
            }

            return configs;
        }
    }
}
