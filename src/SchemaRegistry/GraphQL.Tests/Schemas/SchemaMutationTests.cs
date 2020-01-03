using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types.Relay;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace MarshmallowPie.GraphQL.Schemas
{
    public class SchemaMutationTests
        : GraphQLTestBase
    {
        public SchemaMutationTests(MongoResource mongoResource)
            : base(mongoResource)
        {
        }

        [Fact]
        public async Task CreateSchema()
        {
            // arrange
            // act
            IExecutionResult result = await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"mutation {
                            createSchema(input: {
                                name: ""abc""
                                description: ""def""
                                clientMutationId: ""ghi"" }) {
                                schema {
                                    id
                                    name
                                    description
                                }
                                clientMutationId
                            }
                        }")
                    .Create());

            // assert
            result.MatchSnapshot(o =>
                o.Assert(fo =>
                    Assert.NotNull(fo.Field<string>("Data.createSchema.schema.id"))));
        }

        [Fact]
        public async Task UpdateSchema()
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
                        @"mutation($id: ID!) {
                            updateSchema(input: {
                                id: $id
                                name: ""abc2""
                                description: ""def2""
                                clientMutationId: ""ghi"" }) {
                                schema {
                                    id
                                    name
                                    description
                                }
                                clientMutationId
                            }
                        }")
                    .SetVariableValue("id", id)
                    .Create());

            // assert
            result.MatchSnapshot(o =>
                o.Assert(fo =>
                    Assert.NotNull(fo.Field<string>("Data.updateSchema.schema.id"))));
        }

        [Fact]
        public async Task PublishSchema()
        {
            // arrange
            var serializer = new IdSerializer();

            var schema = new Schema("abc", "def");
            await SchemaRepository.AddSchemaAsync(schema);

            var environment = new Environment("abc", "def");
            await EnvironmentRepository.AddEnvironmentAsync(environment);

            // act
            IExecutionResult result = await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"mutation(
                            $schemaName: String!
                            $environmentName: String!
                            $sourceText: String!
                            $version: String!) {
                            publishSchema(input: {
                                schemaName: $schemaName
                                environmentName: $environmentName
                                sourceText: $sourceText
                                tags: [ { key: ""version"" value: $version } ]
                                clientMutationId: ""ghi"" }) {
                                report {
                                    environment {
                                        name
                                    }
                                    schemaVersion {
                                        hash
                                    }
                                }
                                clientMutationId
                            }
                        }")
                    .SetVariableValue("schemaName", "abc")
                    .SetVariableValue("environmentName", "abc")
                    .SetVariableValue("sourceText", "type Query { a: String }")
                    .SetVariableValue("version", "1.0.0")
                    .Create());

            // assert
            result.MatchSnapshot();
        }
    }
}
