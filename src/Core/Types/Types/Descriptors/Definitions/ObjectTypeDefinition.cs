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
    }
}
