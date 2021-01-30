using Neo4j.Driver;
using Squadron;
using Xunit;

namespace HotChocolate.Data.Neo4J.Language
{
    public class IntegrationTests : IClassFixture<Neo4jResource>
    {
        private Neo4jResource _neo4JResource { get; }

        public IntegrationTests(Neo4jResource neo4jResource)
        {
            _neo4JResource = neo4jResource;
        }

        [Fact]
        public async void CreateTest()
        {
            // arrange
            IAsyncSession session = _neo4JResource.GetAsyncSession();

            try
            {
                Node movie1 = Cypher.Node("Movie")
                    .Named("m")
                    .WithProperties(
                        "Released", Cypher.LiteralTrue(),
                            "Title", Cypher.LiteralOf("The Matrix"),
                            "ReleaseYear", Cypher.Null()
                        );

                Node movie2 = Cypher.Node("Movie")
                    .Named("m")
                    .WithProperties(

                            "Released", Cypher.LiteralTrue(),
                            "Title", Cypher.LiteralOf("The Matrix"),
                            "ReleaseYear", Cypher.Null()
                        );
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}
