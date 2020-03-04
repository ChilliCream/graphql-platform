using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class InputObjectTypeDefinition
        : TypeDefinitionBase<InputObjectTypeDefinitionNode>
    {
        public IBindableList<InputFieldDefinition> Fields { get; } =
            new BindableList<InputFieldDefinition>();

        internal override IEnumerable<ILazyTypeConfiguration> GetConfigurations()
        {
            var configs = ImmutableList<ILazyTypeConfiguration>.Empty;

            if (Configurations.Count > 0)
            {
                configs = configs.AddRange(Configurations);
            }

            foreach (InputFieldDefinition field in Fields)
            {
                if (Configurations.Count > 0)
                {
                    configs = configs.AddRange(field.Configurations);
                }
            }

            return configs;
        }
    }
}
