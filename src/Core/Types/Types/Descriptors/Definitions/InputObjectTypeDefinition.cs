using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class InputObjectTypeDefinition
        : TypeDefinitionBase<InputObjectTypeDefinitionNode>
    {
        public IBindableList<InputFieldDefinition> Fields { get; }
            = new BindableList<InputFieldDefinition>();

        internal override IEnumerable<ITypeConfigration> GetConfigurations()
        {
            var configs = new List<ITypeConfigration>();
            configs.AddRange(Configurations);

            foreach (InputFieldDefinition field in Fields)
            {
                configs.AddRange(field.Configurations);
            }

            return configs;
        }
    }
}
