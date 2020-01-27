using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types.Relay;
using MarshmallowPie.Processing;
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
        public async Task CreateSchema_Duplicate_Name()
        {
            // arrange
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

            // act
            result = await Executor.ExecuteAsync(
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
            result.MatchSnapshot();
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
        public async Task UpdateSchema_DuplicateName()
        {
            // arrange
            var serializer = new IdSerializer();
            var schemaA = new Schema(Guid.NewGuid(), "abc", "def");
            var schemaB = new Schema(Guid.NewGuid(), "def", "ghi");
            await SchemaRepository.AddSchemaAsync(schemaA);
            await SchemaRepository.AddSchemaAsync(schemaB);
            string id = serializer.Serialize("Schema", schemaA.Id);

            // act
            IExecutionResult result = await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"mutation($id: ID!) {
                            updateSchema(input: {
                                id: $id
                                name: ""def""
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
            result.MatchSnapshot();
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
                                sessionId
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
            Assert.Collection(ReceivedMessages,
                t =>
                {
                    PublishDocumentMessage message = Assert.IsType<PublishDocumentMessage>(t);
                    Assert.Equal(schema.Id, message.SchemaId);
                    Assert.Equal(environment.Id, message.EnvironmentId);
                    Assert.Equal(DocumentType.Schema, message.Type);
                    Assert.Null(message.ClientId);
                    Assert.NotNull(message.SessionId);
                    Assert.Collection(message.Tags,
                        t =>
                        {
                            Assert.Equal("version", t.Key);
                            Assert.Equal("1.0.0", t.Value);
                        });
                });

        }

        [Fact]
        public async Task PublishSchema_DifferentTags()
        {
            // arrange
            var serializer = new IdSerializer();

            var schema = new Schema("abc", "def");
            await SchemaRepository.AddSchemaAsync(schema);

            var environment = new Environment("abc", "def");
            await EnvironmentRepository.AddEnvironmentAsync(environment);

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
                                sessionId
                                clientMutationId
                            }
                        }")
                    .SetVariableValue("schemaName", "abc")
                    .SetVariableValue("environmentName", "abc")
                    .SetVariableValue("sourceText", "type Query { a: String }")
                    .SetVariableValue("version", "1.0.0")
                    .Create());

            // act
            result = await Executor.ExecuteAsync(
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
                                sessionId
                                clientMutationId
                            }
                        }")
                    .SetVariableValue("schemaName", "abc")
                    .SetVariableValue("environmentName", "abc")
                    .SetVariableValue("sourceText", "type Query { a: String }")
                    .SetVariableValue("version", "1.1.0")
                    .Create());

            // assert
            result.MatchSnapshot();
        }
    }
}
