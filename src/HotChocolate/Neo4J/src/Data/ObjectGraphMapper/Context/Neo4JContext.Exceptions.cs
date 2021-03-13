using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
    public partial class Neo4JContext
    {
        private static readonly Neo4jException _duplicateNodeEntityKey =
            new ("Duplicate node entity key");

        private static readonly Neo4jException _unsupportedNodeEntityType =
            new ("Unsupported entity type");

        private static readonly Neo4jException _unsupportedRelationshipType =
            new ("Unsupported relationship type");

        private static readonly Neo4jException _illegalNodeEntityException =
            new ("Invalid neo4j node entity");

        private static readonly Neo4jException _idIsMissingException =
            new ("Id property is missing");

        private static readonly Neo4jException _duplicateIdException =
            new ("Duplicate id property");

        private static readonly Neo4jException _illegalIdFormatException =
            new ("Id property is not nullable long");
    }
}
