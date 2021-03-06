using System;
using System.Collections.Generic;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
    public class Neo4JContext
    {
        private static readonly Type NodeEntityType = typeof(Neo4JNodeAttribute);
        private static readonly Type NodeIdType = typeof(Neo4JNodeIdAttribute);
        private static readonly Type IgnoredType = typeof(Neo4JIgnoreAttribute);
        private static readonly Type RelationshipEntityType = typeof(Neo4JRelationshipAttribute);

        private static readonly Neo4jException DuplicateNodeEntityKey =
            new("Duplicate node entity key");

        private static readonly Neo4jException UnsupportedNodeEntityType =
            new Neo4jException("Unsupported entity type");

        //private readonly IDictionary<Type, Meta> _allTypes = new Dictionary<Type, Meta>();
    }
}
