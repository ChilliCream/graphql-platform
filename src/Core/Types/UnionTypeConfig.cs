using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class UnionTypeConfig
    {
        public UnionTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Func<IEnumerable<ObjectType>> Types { get; set; }

        public ResolveType TypeResolver { get; set; }
    }
}
