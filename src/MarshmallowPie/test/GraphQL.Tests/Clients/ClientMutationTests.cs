using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types.Relay;
using MarshmallowPie.Storage.FileSystem;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace MarshmallowPie.GraphQL.Clients
{
    public class ClientMutationTests
        : GraphQLTestBase
    {
        public ClientMutationTests(MongoResource mongoResource, FileStorageResource fileStorageResource)
            : base(mongoResource, fileStorageResource)
        {
        }

        [Fact]
        public async Task CreateClient()
        {
            // arrange
            var serializer = new IdSerializer();
            var schema = new Schema(Guid.NewGuid(), "abc", "def");
            await SchemaRepository.AddSchemaAsync(schema);
            string schemaId = serializer.Serialize("Schema", schema.Id);

            // act
            IExecutionResult result = await Executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"mutation($schemaId: ID! $name: String! $description: String) {
                            createClient(input: {
                                schemaId: $schemaId
                                name: $name
                                description: $description
                            })
                            {
                                schema {
                                    name
                                    description
                                }
                                client {
                                    name
                                    description
                                }
                            }
                        }")
                    .SetVariableValue("schemaId", schemaId)
                    .SetVariableValue("name", "client_abc")
                    .SetVariableValue("description", "client_def")
                    .Create());

            // assert
            result.MatchSnapshot();
        }
    }
}
