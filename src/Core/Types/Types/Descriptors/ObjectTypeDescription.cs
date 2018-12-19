using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class ObjectTypeDescription
        : TypeDescriptionBase
    {
        public ObjectTypeDefinitionNode SyntaxNode { get; set; }

        public Type ClrType { get; set; }

        public IsOfType IsOfType { get; set; }

        public BindingBehavior FieldBindingBehavior { get; set; }

        public List<ObjectFieldDescription> Fields { get; set; } =
            new List<ObjectFieldDescription>();

        public List<TypeReference> Interfaces { get; set; } =
            new List<TypeReference>();
    }
}
