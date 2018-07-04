using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class ObjectTypeDescription
    {
        public ObjectTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Type NativeType { get; set; }

        public bool IsIntrospection { get; set; }

        public IsOfType IsOfType { get; set; }

        public BindingBehavior FieldBindingBehavior { get; set; }

        public List<ObjectFieldDescription> Fields { get; set; } =
            new List<ObjectFieldDescription>();

        public List<TypeReference> Interfaces { get; set; } =
            new List<TypeReference>();

    }
}
