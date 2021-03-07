using System;
using System.Collections.Generic;
using Neo4j.Driver;

#nullable enable

namespace HotChocolate.Data.Neo4J
{
    public class Neo4JContext
    {
        private static readonly Type _nodeEntityType = typeof(Neo4JNodeAttribute);
        private static readonly Type _nodeIdType = typeof(Neo4JNodeIdAttribute);
        private static readonly Type _ignoredType = typeof(Neo4JIgnoreAttribute);
        private static readonly Type _relationshipEntityType = typeof(Neo4JRelationshipAttribute);

        private static readonly Neo4jException _duplicateNodeEntityKey =
            new Neo4jException("Duplicate node entity key");

        private static readonly Neo4jException _unsupportedNodeEntityType =
            new Neo4jException("Unsupported entity type");

        private static readonly Neo4jException _unsupportedRelationshipType =
            new Neo4jException("Unsupported relationship type");

        private static readonly Neo4jException _illegalNodeEntityException =
            new Neo4jException("Invalid neo4j node entity");

        private static readonly Neo4jException _idIsMissingException =
            new Neo4jException("Id property is missing");

        private static readonly Neo4jException _duplicateIdException =
            new Neo4jException("Duplicate id property");

        private static readonly Neo4jException _illegalIdFormatException =
            new Neo4jException("Id property is not nullable long");

        private ISchema Schema { get; }

        public Neo4JContext()
        {
        }
    }
}
