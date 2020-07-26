using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Relay
{
    public class IdAttributeTests
    {
        [Fact]
        public async Task Id_On_Arguments()
        {
            // arrange
            var idSerializer = new IdSerializer();
            string intId = idSerializer.Serialize("Query", 1);
            string stringId = idSerializer.Serialize("Query", "abc");
            string guidId = idSerializer.Serialize("Query", Guid.Empty);

            // act
            IExecutionResult result =
                await SchemaBuilder.New()
                    .AddQueryType<Query>()
                    .Create()
                    .MakeExecutable()
                    .ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery(
                                @"query foo ($intId: ID! $stringId: ID! $guidId: ID!) {
                                    intId(id: $intId)
                                    stringId(id: $stringId)
                                    guidId(id: $guidId)
                                }")
                            .SetVariableValue("intId", intId)
                            .SetVariableValue("stringId", stringId)
                            .SetVariableValue("guidId", guidId)
                            .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        public class Query
        {
            public string IntId([ID] int id) => id.ToString();

            public string StringId([ID] string id) => id.ToString();

            public string GuidId([ID] Guid id) => id.ToString();

        }
    }
}