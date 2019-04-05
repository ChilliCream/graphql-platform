using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types;

namespace HotChocolate
{
    internal sealed class SchemaDefinition
    {
        public string Description { get; set; }
        public IDirectiveCollection Directives { get; set; }
        public IReadOnlySchemaOptions Options { get; set; }

        public IServiceProvider Services { get; set; }

        public ObjectType QueryType { get; set; }
        public ObjectType MutationType { get; set; }
        public ObjectType SubscriptionType { get; set; }

        public IReadOnlyList<INamedType> Types { get; set; }
        public IReadOnlyList<DirectiveType> DirectiveTypes { get; set; }
    }
}
