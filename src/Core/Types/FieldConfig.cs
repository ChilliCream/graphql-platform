using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    internal class FieldConfig
    {
        public FieldDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        internal bool IsIntrospection { get; set; }

        public string DeprecationReason { get; set; }

        public Func<SchemaContext, IOutputType> Type { get; set; }

        public IEnumerable<InputField> Arguments { get; set; }

        public Func<SchemaContext, FieldResolverDelegate> Resolver { get; set; }
    }
}
