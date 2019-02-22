using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public class ObjectTypeDescription
        : TypeDescriptionBase<ObjectTypeDefinitionNode>
    {
        public IsOfType IsOfType { get; set; }

        public BindingBehavior FieldBindingBehavior { get; set; }

        public ICollection<TypeReference> Interfaces { get; } =
            new List<TypeReference>();

        public IBindableList<ObjectFieldDescription> Fields { get; } =
            new BindableList<ObjectFieldDescription>();
    }
}
