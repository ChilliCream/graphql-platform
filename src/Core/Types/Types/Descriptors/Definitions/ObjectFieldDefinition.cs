using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class ObjectFieldDefinition
        : OutputFieldDefinitionBase
    {
        public Type ResolverType { get; set; }

        public MemberInfo Member { get; set; }

        public FieldResolverDelegate Resolver { get; set; }

        public IList<FieldMiddleware> MiddlewareComponents { get; } =
            new List<FieldMiddleware>();
    }
}
