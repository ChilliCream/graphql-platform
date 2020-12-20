using System.Collections.Generic;
using HotChocolate.Data.Neo4J.Language;
using Neo4j.Driver;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace HotChocolate.Data.Neo4J.Tests
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
                        new Dictionary<string, ILiteral>()
                        {
                            {"Released", Cypher.LiteralTrue()},
                            {"Title", Cypher.StringLiteral("The Matrix")},
                            {"ReleaseYear", Cypher.Null()}
                        });

                Node movie2 = Cypher.Node("Movie")
                    .Named("m")
                    .WithProperties(
                        new Dictionary<string, ILiteral>()
                        {
                            {"Released", Cypher.LiteralTrue()},
                            {"Title", Cypher.StringLiteral("The Matrix")},
                            {"ReleaseYear", Cypher.Null()}
                        });


            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}
