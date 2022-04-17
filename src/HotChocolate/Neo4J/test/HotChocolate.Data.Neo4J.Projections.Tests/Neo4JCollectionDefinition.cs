using Xunit;

namespace HotChocolate.Data.Neo4J.Projections;

[CollectionDefinition("Database")]
public class Neo4JCollectionDefinition : ICollectionFixture<Neo4JFixture>
{
    // This class is empty on purpose
}
