using System;
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

        public IEnumerable<ITypeReference> GetDependencies()
        {
            var dependencies = new List<ITypeReference>();

            dependencies.AddRange(Interfaces);

            foreach (ObjectFieldDefinition field in Fields)
            {
                dependencies.AddRange(field.GetDependencies());
            }

            return dependencies;
        }
    }
}
