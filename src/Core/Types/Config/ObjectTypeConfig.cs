using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class ObjectTypeConfig
    {
        public ObjectTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        internal bool IsIntrospection { get; set; }

        public Func<ITypeRegistry, IEnumerable<InterfaceType>> Interfaces { get; set; }

        public IEnumerable<Field> Fields { get; set; }

        public IsOfType IsOfType { get; set; }
    }



}
