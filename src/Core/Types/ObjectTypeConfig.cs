using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class ObjectTypeConfig
        : INamedTypeConfig
    {
        public ObjectTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        internal bool IsIntrospection { get; set; }

        public Func<IEnumerable<InterfaceType>> Interfaces { get; set; }

        public IEnumerable<Field> Fields { get; set; }

        public IsOfType IsOfType { get; set; }
    }



}
