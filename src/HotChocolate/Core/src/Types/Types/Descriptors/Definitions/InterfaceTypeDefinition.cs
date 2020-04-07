using System;
using System.Collections.Generic;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class InterfaceTypeDefinition
        : TypeDefinitionBase<InterfaceTypeDefinitionNode>
        , IComplexOutputTypeDefinition
    {
        public IList<Type> KnownClrTypes { get; } = new List<Type>();

        public ResolveAbstractType? ResolveAbstractType { get; set; }

        public IList<ITypeReference> Interfaces { get; } =
            new List<ITypeReference>();

        public IBindableList<InterfaceFieldDefinition> Fields { get; } =
            new BindableList<InterfaceFieldDefinition>();

        internal override IEnumerable<ILazyTypeConfiguration> GetConfigurations()
        {
            var configs = new List<ILazyTypeConfiguration>();

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
