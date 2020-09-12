using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class ObjectTypeDefinition
        : TypeDefinitionBase<ObjectTypeDefinitionNode>
        , IComplexOutputTypeDefinition
    {
        public override Type RuntimeType
        {
            get => base.RuntimeType;
            set
            {
                base.RuntimeType = value;
                FieldBindingType = value;
            }
        }

        public Type FieldBindingType { get; set; }

        public IList<Type> KnownClrTypes { get; } = new List<Type>();

        public IsOfType IsOfType { get; set; }

        public IList<ITypeReference> Interfaces { get; } =
            new List<ITypeReference>();

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
