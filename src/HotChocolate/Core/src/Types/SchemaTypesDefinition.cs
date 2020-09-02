using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate
{
    internal sealed class SchemaTypesDefinition
    {
        public ObjectType QueryType { get; set; }
        public ObjectType MutationType { get; set; }
        public ObjectType SubscriptionType { get; set; }

        public IReadOnlyList<INamedType> Types { get; set; }
        public IReadOnlyList<DirectiveType> DirectiveTypes { get; set; }
    }
}
