using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Configuration;
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

        public MemberInfo Member { get; set; }

        public Func<ITypeRegistry, IOutputType> Type { get; set; }

        public Type NativeNamedType { get; set; }

        public IEnumerable<InputField> Arguments { get; set; }

        public Func<IResolverRegistry, FieldResolverDelegate> Resolver { get; set; }
    }
}
