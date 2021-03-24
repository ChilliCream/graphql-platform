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
        private List<Type>? _knownClrTypes;
        private List<ITypeReference>? _interfaces;

        public IList<Type> KnownRuntimeTypes => _knownClrTypes ??= new List<Type>();

        public ResolveAbstractType? ResolveAbstractType { get; set; }

        public IList<ITypeReference> Interfaces => _interfaces ??= new List<ITypeReference>();

        public IBindableList<InterfaceFieldDefinition> Fields { get; } =
            new BindableList<InterfaceFieldDefinition>();

        internal override IEnumerable<ILazyTypeConfiguration> GetConfigurations()
        {
            var configs = new List<ILazyTypeConfiguration>();

            configs.AddRange(Configurations);

            foreach (InterfaceFieldDefinition field in Fields)
            {
                configs.AddRange(field.Configurations);

                foreach (ArgumentDefinition argument in field.GetArguments())
                {
                    configs.AddRange(argument.Configurations);
                }
            }

            return configs;
        }

        internal IReadOnlyList<Type> GetKnownClrTypes()
        {
            if (_knownClrTypes is null)
            {
                return Array.Empty<Type>();
            }

            return _knownClrTypes;
        }

        internal IReadOnlyList<ITypeReference> GetInterfaces()
        {
            if (_interfaces is null)
            {
                return Array.Empty<ITypeReference>();
            }

            return _interfaces;
        }
    }
}
