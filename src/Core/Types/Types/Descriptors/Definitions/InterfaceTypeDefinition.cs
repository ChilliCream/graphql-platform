using System;
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

        public IEnumerable<ITypeReference> GetDependencies()
        {
            var dependencies = new List<ITypeReference>();

            foreach (InterfaceFieldDefinition field in Fields)
            {
                dependencies.AddRange(field.GetDependencies());
            }

            return dependencies;
        }
    }
}
