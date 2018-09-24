using System;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    internal class ObjectFieldDescription
        : ObjectFieldDescriptionBase
    {
        public Type SourceType { get; set; }

        public Type ResolverType { get; set; }

        public MemberInfo Member { get; set; }

        public FieldResolverDelegate Resolver { get; set; }
    }
}
