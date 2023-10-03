using HotChocolate.Data.Neo4J.Testing;

namespace HotChocolate.Data.Neo4J.Integration;

[CollectionDefinition(DefinitionName)]
public class Neo4JDatabaseCollectionFixture : ICollectionFixture<Neo4JDatabase>
{
    internal const string DefinitionName = "Neo4JDatabase";
}
