using HotChocolate.Analyzers.Configuration;
using Snapshooter.Xunit;
using Xunit;

namespace Analyzers.Tests
{
    public class GraphQLConfigTests
    {
        [Fact]
        public void NewDefaultConfig()
        {
            new GraphQLConfig()
                .ToString()
                .MatchSnapshot();
        }

        [Fact]
        public void ParseNeo4JSettings()
        {
            // arrange
            const string json = @"{
                ""schema"": ""schema.graphql"",
                ""documents"": ""**/*.graphql"",
                ""extensions"": {
                    ""neo4j"": {
                        ""databaseName"": ""abc"",
                        ""namespace"": ""Foo.Bar""
                    }
                }
            }";

            // act
            var config = GraphQLConfig.FromJson(json);

            // assert
            config.MatchSnapshot();
        }
    }
}













