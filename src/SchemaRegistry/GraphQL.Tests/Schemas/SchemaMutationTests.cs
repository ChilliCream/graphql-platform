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
                    Assert.NotNull(fo.Field<string>("Data.createSchema.schema.id"))));
        }
    }
}
