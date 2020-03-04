using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class ObjectTypeDefinition
        : TypeDefinitionBase<ObjectTypeDefinitionNode>
    {
        public override Type ClrType
        {
            get => base.ClrType;
            set
            {
                base.ClrType = value;
                FieldBindingType = value;
            }
        }

        public Type FieldBindingType { get; set; }

        public IsOfType IsOfType { get; set; }

        public IList<ITypeReference> Interfaces { get; } =
            new LazyList<ITypeReference>();

        public IBindableList<ObjectFieldDefinition> Fields { get; } =
            new BindableList<ObjectFieldDefinition>();

        internal override IEnumerable<ILazyTypeConfiguration>
            GetConfigurations()
        {
            var configs = ImmutableList<ILazyTypeConfiguration>.Empty;

            if (Configurations.Count > 0)
            {
                configs = configs.AddRange(Configurations);
            }

            foreach (ObjectFieldDefinition field in Fields)
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
