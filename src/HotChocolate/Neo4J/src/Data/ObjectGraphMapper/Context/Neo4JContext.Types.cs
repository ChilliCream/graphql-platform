using System;

namespace HotChocolate.Data.Neo4J
{
    public partial class Neo4JContext
    {
        private static readonly Type _nodeType = typeof(Neo4JNodeAttribute);
        private static readonly Type _relationshipType = typeof(Neo4JRelationshipAttribute);
        private static readonly Type _nodeIdType = typeof(Neo4JNodeIdAttribute);
        private static readonly Type _ignoredType = typeof(Neo4JIgnoreAttribute);
    }
}
