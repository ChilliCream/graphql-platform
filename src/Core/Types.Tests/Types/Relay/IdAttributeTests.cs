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
                    .AddType<FooPayload>()
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

        [Fact]
        public async Task Id_On_Objects()
        {
            // arrange
            var idSerializer = new IdSerializer();
            string someId = idSerializer.Serialize("Some", 1);

            // act
            IExecutionResult result =
                await SchemaBuilder.New()
                    .AddQueryType<Query>()
                    .AddType<FooPayload>()
                    .Create()
                    .MakeExecutable()
                    .ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery(
                                @"query foo ($someId: ID!) {
                                    foo(input: { someId: $someId }) {
                                        someId
                                    }
                                }")
                            .SetVariableValue("someId", someId)
                            .Create());

            // assert
            new {
                result = result.ToJson(),
                someId
            }.MatchSnapshot();
        }

        [Fact]
        public void Id_Type_Is_Correctly_Inferred()
        {
            SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .Create()
                .ToString()
                .MatchSnapshot();
        }

        public class Query
        {
            public string IntId([ID] int id) => id.ToString();
            public string StringId([ID] string id) => id.ToString();
            public string GuidId([ID] Guid id) => id.ToString();
            public IFooPayload Foo(FooInput input) => new FooPayload { SomeId = input.SomeId };
        }

        public class FooInput
        {
            [ID("Some")] public string SomeId { get; set; }
        }

        public class FooPayload : IFooPayload
        {
            [ID("Bar")] public string SomeId { get; set; }
        }

        public interface IFooPayload
        {
            [ID] string SomeId { get; set; }
        }
    }
}