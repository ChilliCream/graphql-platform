using System;
using System.Collections.Generic;
using System.Linq;
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
            var intId = idSerializer.Serialize("Query", 1);
            var stringId = idSerializer.Serialize("Query", "abc");
            var guidId = idSerializer.Serialize("Query", Guid.Empty);

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
                                    intIdList(id: [$intId])
                                    stringId(id: $stringId)
                                    stringIdList(id: [$stringId])
                                    guidId(id: $guidId)
                                    guidIdList(id: [$guidId $guidId])
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
            var someId = idSerializer.Serialize("Some", "1");
            var someIntId = idSerializer.Serialize("Some", 1);

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
                                @"query foo ($someId: ID! $someIntId: ID!) {
                                    foo(input: { someId: $someId someIds: [$someIntId] }) {
                                        someId
                                        ... on FooPayload {
                                            someIds
                                        }
                                    }
                                }")
                            .SetVariableValue("someId", someId)
                            .SetVariableValue("someIntId", someIntId)
                            .Create());

            // assert
            new
            {
                result = result.ToJson(),
                someId
            }.MatchSnapshot();
        }

        [Fact]
        public async Task PolyId_On_Objects()
        {
            // arrange
            var idSerializer = new IdSerializer();
            var someId = "1";
            var someIntId = 1;

            // act
            IExecutionResult result =
                await SchemaBuilder.New()
                    .AddQueryType<Query>()
                    .AddType<FooPayload>()
                    .TryAddTypeInterceptor<PolymorphicGlobalIdsTypeInterceptor>()
                    .Create()
                    .MakeExecutable()
                    .ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery(
                                @"query foo ($someId: ID! $someIntId: ID!) {
                                    foo(input: { someId: $someId someIds: [$someIntId] }) {
                                        someId
                                        ... on FooPayload {
                                            someIds
                                        }
                                    }
                                }")
                            .SetVariableValue("someId", someId)
                            .SetVariableValue("someIntId", someIntId)
                            .Create());

            // assert
            new
            {
                result = result.ToJson(),
                someId,
                someIntId
            }.MatchSnapshot();
        }

        [Fact]
        public async Task Id_On_Objects_InvalidType()
        {
            // arrange
            var idSerializer = new IdSerializer();
            var someId = idSerializer.Serialize("Some", Guid.Empty);

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
                                    foo(input: { someId: $someId someIds: [$someId] }) {
                                        someId
                                        ... on FooPayload {
                                            someIds
                                        }
                                    }
                                }")
                            .SetVariableValue("someId", someId)
                            .Create());

            // assert
            new
            {
                result = result.ToJson(),
                someId
            }.MatchSnapshot();
        }

        [Fact]
        public async Task Id_On_Objects_InvalidId()
        {
            // arrange
            var someId = "abc";

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
                                    foo(input: { someId: $someId someIds: [$someId] }) {
                                        someId
                                        ... on FooPayload {
                                            someIds
                                        }
                                    }
                                }")
                            .SetVariableValue("someId", someId)
                            .Create());

            // assert
            new
            {
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
            public string IntIdList([ID] int[] id) =>
                string.Join("-", id.Select(t => t.ToString()));
            public string StringId([ID] string id) => id;
            public string StringIdList([ID] string[] id) =>
                string.Join("-", id.Select(t => t.ToString()));
            public string GuidId([ID] Guid id) => id.ToString();
            public string GuidIdList([ID] IReadOnlyList<Guid> id) =>
                string.Join("-", id.Select(t => t.ToString()));
            public IFooPayload Foo(FooInput input) => new FooPayload { SomeId = input.SomeId };
        }

        public class FooInput
        {
            public FooInput(string someId, IReadOnlyList<int> someIds)
            {
                SomeId = someId;
                SomeIds = someIds;
            }

            [ID("Some")] public string SomeId { get; set; }

            [ID("Some")] public IReadOnlyList<int> SomeIds { get; }
        }

        public class FooPayload : IFooPayload
        {
            [ID("Bar")] public string SomeId { get; set; }

            [ID("Bar")] public IReadOnlyList<int> SomeIds { get; set; }
        }

        public interface IFooPayload
        {
            [ID] string SomeId { get; set; }
        }
    }
}
