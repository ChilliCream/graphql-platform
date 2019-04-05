using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class InterfaceTypeDefinition
        : TypeDefinitionBase<InterfaceTypeDefinitionNode>
    {
        public ResolveAbstractType ResolveAbstractType { get; set; }

        public IBindableList<InterfaceFieldDefinition> Fields { get; } =
            new BindableList<InterfaceFieldDefinition>();

        internal override IEnumerable<ITypeConfigration> GetConfigurations()
        {
            var configs = new List<ITypeConfigration>();
            configs.AddRange(Configurations);

            foreach (InterfaceFieldDefinition field in Fields)
            {
                configs.AddRange(field.Configurations);

                foreach (ArgumentDefinition argument in field.Arguments)
                {
                    configs.AddRange(argument.Configurations);
                }
            }

            return configs;
        }
    }
}
