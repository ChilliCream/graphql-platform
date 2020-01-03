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
        public async Task GetSchemaAsNode()
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
        public async Task GetSchemaByName()
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
                            schemaByName(name: $name) {
                                id
                                name
                                description
                            }
                        }")
                    .SetVariableValue("name", "abc")
                    .Create());

            // assert
            result.MatchSnapshot(o =>
                o.Assert(fo =>
                    Assert.Equal(id, fo.Field<string>("Data.schemaByName.id"))));
        }

        [Fact]
        public async Task GetSchemasByNames()
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
                        @"query($names: [String!]!) {
                            schemasByName(names: $names) {
                                id
                                name
                                description
                            }
                        }")
                    .SetVariableValue("names", new[] { schema.Name })
                    .Create());

            // assert
            result.MatchSnapshot(o =>
                o.Assert(fo =>
                    Assert.Equal(id, fo.Field<string>("Data.schemasByName[0].id"))));
        }

        [Fact]
        public async Task QuerySchema()
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

        [Fact]
        public async Task GetSchemaVersionAsNode()
        {
            // arrange
            var serializer = new IdSerializer();
            var schema = new Schema(Guid.NewGuid(), "abc", "def");
            await SchemaRepository.AddSchemaAsync(schema);
            var schemaVersion = new SchemaVersion(
                Guid.NewGuid(), schema.Id, "abc", "def",
                Array.Empty<Tag>(), DateTime.UnixEpoch);
            await SchemaRepository.AddSchemaVersionAsync(schemaVersion);
            string id = serializer.Serialize("SchemaVersion", schemaVersion.Id);

            // act
            IExecutionResult result = await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"query($id: ID!) {
                            node(id: $id) {
                                id
                                ... on SchemaVersion {
                                    sourceText
                                    hash
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
        public async Task GetSchemaVersionById()
        {
            // arrange
            var serializer = new IdSerializer();
            var schema = new Schema(Guid.NewGuid(), "abc", "def");
            await SchemaRepository.AddSchemaAsync(schema);
            var schemaVersion = new SchemaVersion(
                Guid.NewGuid(), schema.Id, "abc", "def",
                Array.Empty<Tag>(), DateTime.UnixEpoch);
            await SchemaRepository.AddSchemaVersionAsync(schemaVersion);
            string id = serializer.Serialize("SchemaVersion", schemaVersion.Id);

            // act
            IExecutionResult result = await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"query($id: ID!) {
                            schemaVersionById(id: $id) {
                                id
                                sourceText
                            }
                        }")
                    .SetVariableValue("id", id)
                    .Create());

            // assert
            result.MatchSnapshot(o =>
                o.Assert(fo =>
                    Assert.Equal(id, fo.Field<string>("Data.schemaVersionById.id"))));
        }

        [Fact]
        public async Task GetSchemaVersionsById()
        {
            // arrange
            var serializer = new IdSerializer();
            var schema = new Schema(Guid.NewGuid(), "abc", "def");
            await SchemaRepository.AddSchemaAsync(schema);
            var schemaVersion = new SchemaVersion(
                Guid.NewGuid(), schema.Id, "abc", "def",
                Array.Empty<Tag>(), DateTime.UnixEpoch);
            await SchemaRepository.AddSchemaVersionAsync(schemaVersion);
            string id = serializer.Serialize("SchemaVersion", schemaVersion.Id);

            // act
            IExecutionResult result = await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"query($ids: [ID!]!) {
                            schemaVersionsById(ids: $ids) {
                                id
                                sourceText
                            }
                        }")
                    .SetVariableValue("ids", new[] { id })
                    .Create());

            // assert
            result.MatchSnapshot(o =>
                o.Assert(fo =>
                    Assert.Equal(id, fo.Field<string>("Data.schemaVersionsById[0].id"))));
        }
    }
}
