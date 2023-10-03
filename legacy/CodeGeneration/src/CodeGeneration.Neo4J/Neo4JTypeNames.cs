namespace HotChocolate.CodeGeneration.Neo4J
{
    public static class Neo4JTypeNames
    {
        public const string Driver = "Neo4j.Driver";
        public const string Neo4J = TypeNames.Data + "." + nameof(Neo4J);
        public const string Execution = Neo4J + "." + nameof(Execution);
        public const string Neo4JExecutable = Execution + "." + nameof(Neo4JExecutable);
        public const string IAsyncSession = Driver + "." + nameof(IAsyncSession);
        public const string UseNeo4JDatabaseAttribute = Neo4J + "." + 
            nameof(UseNeo4JDatabaseAttribute);
        public const string Neo4JRelationshipAttribute = Neo4J + "." + 
            nameof(Neo4JRelationshipAttribute);
        public const string Neo4JRelationshipDirection = Neo4J + "." + "RelationshipDirection";
        public const string Neo4JDataRequestBuilderExtensions = Neo4J + "." + 
            nameof(Neo4JDataRequestBuilderExtensions);
    }
}
