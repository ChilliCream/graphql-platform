using System;
using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Relay.Descriptors
{
    public class NodeDefinition : DefinitionBase
    {
        public Type? NodeType { get; set; }

        public MemberInfo? IdMember { get; set; }

        public FieldResolverDelegate? Resolver { get; set; }
    }
}
