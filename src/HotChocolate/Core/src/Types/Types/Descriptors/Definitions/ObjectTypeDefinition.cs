using System;
using System.Collections.Generic;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class ObjectTypeDefinition
        : TypeDefinitionBase<ObjectTypeDefinitionNode>
        , IComplexOutputTypeDefinition
    {
        private List<Type>? _knownClrTypes;
        private List<ITypeReference>? _interfaces;
        private List<ObjectFieldBinding>? _fieldIgnores;

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

        public IList<Type> KnownClrTypes => _knownClrTypes ??= new List<Type>();

        public IList<ObjectFieldBinding> FieldIgnores => _fieldIgnores ??= new List<ObjectFieldBinding>();

        public IsOfType IsOfType { get; set; }

        public bool IsExtension { get; set; }

        public IList<ITypeReference> Interfaces => _interfaces ??=new List<ITypeReference>();

        public IBindableList<ObjectFieldDefinition> Fields { get; } =
            new BindableList<ObjectFieldDefinition>();

        internal override IEnumerable<ILazyTypeConfiguration> GetConfigurations()
        {
            var configs = new List<ILazyTypeConfiguration>();

            configs.AddRange(Configurations);

            foreach (ObjectFieldDefinition field in Fields)
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

        internal IReadOnlyList<ObjectFieldBinding> GetFieldIgnores()
        {
            if (_fieldIgnores is null)
            {
                return Array.Empty<ObjectFieldBinding>();
            }

            return _fieldIgnores;
        }
    }
}
