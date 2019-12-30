using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types.Relay;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace MarshmallowPie.GraphQL.Schemas
{
    public class SchemaQueryTests
        : GraphQLTestBase
    {
        public SchemaQueryTests(MongoResource mongoResource)
            : base(mongoResource)
        {
        }

        [Fact]
        public async Task GetNode()
        {
            // arrange
            var serializer = new IdSerializer();
            var schema = new Schema(Guid.NewGuid(), "abc", "def");
            await SchemaRepository.AddSchemaAsync(schema);
            string id = serializer.Serialize("Schema", schema.Id);

            // act
            IExecutionResult result = await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"query($id: ID!) {
                            node(id: $id) {
                                id
                                ... on Schema {
                                    name
                                    description
                                }
                            }
                        }")
                    .SetVariableValue("id", id)
                    .Create());

            // assert
            result.MatchSnapshot(o =>
                o.Assert(fo =>
                    Assert.Equal(id, fo.Field<string>("Data.node.id"))));
        }

        [Fact]
        public async Task GetSchemaById()
        {
            // arrange
            var serializer = new IdSerializer();
            var schema = new Schema(Guid.NewGuid(), "abc", "def");
            await SchemaRepository.AddSchemaAsync(schema);
            string id = serializer.Serialize("Schema", schema.Id);

            // act
            IExecutionResult result = await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"query($id: ID!) {
                            schemaById(id: $id) {
                                id
                                name
                                description
                            }
                        }")
                    .SetVariableValue("id", id)
                    .Create());

            // assert
            result.MatchSnapshot(o =>
                o.Assert(fo =>
                    Assert.Equal(id, fo.Field<string>("Data.schemaById.id"))));
        }

        [Fact]
        public async Task GetSchemasById()
        {
            // arrange
            var serializer = new IdSerializer();
            var schema = new Schema(Guid.NewGuid(), "abc", "def");
            await SchemaRepository.AddSchemaAsync(schema);
            string id = serializer.Serialize("Schema", schema.Id);

            // act
            IExecutionResult result = await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"query($ids: [ID!]!) {
                            schemasById(ids: $ids) {
                                id
                                name
                                description
                            }
                        }")
                    .SetVariableValue("ids", new[] { id })
                    .Create());

            // assert
            result.MatchSnapshot(o =>
                o.Assert(fo =>
                    Assert.Equal(id, fo.Field<string>("Data.schemasById[0].id"))));
        }

        [Fact]
        public async Task GetSchemasByName()
        {
            // arrange
            var serializer = new IdSerializer();
            var schema = new Schema(Guid.NewGuid(), "abc", "def");
            await SchemaRepository.AddSchemaAsync(schema);
            string id = serializer.Serialize("Schema", schema.Id);

            // act
            IExecutionResult result = await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"query($name: String!) {
                            schemas(where: { name: $name }) {
                                nodes {
                                    id
                                    name
                                    description
                                }
                            }
                        }")
                    .SetVariableValue("name", schema.Name)
                    .Create());

            // assert
            result.MatchSnapshot(o =>
                o.Assert(fo =>
                    Assert.Equal(id, fo.Field<string>("Data.schemas.nodes[0].id"))));
        }
    }
}
