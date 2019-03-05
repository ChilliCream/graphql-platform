using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class ObjectTypeDefinition
        : TypeDescriptionBase<ObjectTypeDefinitionNode>
    {
        public IsOfType IsOfType { get; set; }

        public BindingBehavior FieldBindingBehavior { get; set; }

        public ICollection<TypeReference> Interfaces { get; } =
            new List<TypeReference>();

        public IBindableList<ObjectFieldDefinition> Fields { get; } =
            new BindableList<ObjectFieldDefinition>();
    }
}
