using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class ObjectFieldDefinition
        : OutputFieldDefinitionBase
    {
        public Type SourceType { get; set; }

        public Type ResolverType { get; set; }

        public MemberInfo Member { get; set; }

        public Type ResultType { get; set; }

        public FieldResolverDelegate Resolver { get; set; }

        public SubscribeResolverDelegate SubscribeResolver { get; set; }

        public IList<FieldMiddleware> MiddlewareComponents { get; } =
            new List<FieldMiddleware>();
    }
}
