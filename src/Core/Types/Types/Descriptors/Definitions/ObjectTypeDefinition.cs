using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class ObjectTypeDefinition
        : TypeDefinitionBase<ObjectTypeDefinitionNode>
    {
        public IsOfType IsOfType { get; set; }

        public ICollection<ITypeReference> Interfaces { get; } =
            new List<ITypeReference>();

        public BindingBehavior FieldBindingBehavior { get; set; }

        public IBindableList<ObjectFieldDefinition> Fields { get; } =
            new BindableList<ObjectFieldDefinition>();

        internal override IEnumerable<ILazyTypeConfiguration> GetConfigurations()
        {
            var configs = new List<ILazyTypeConfiguration>();
            configs.AddRange(Configurations);

            foreach (ObjectFieldDefinition field in Fields)
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
