using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;

namespace HotChocolate.Types.Relay;

public class IdDescriptorTests
{
    [Fact]
    public async Task Id_On_Arguments()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<QueryType>()
            .AddType<FooPayloadType>()
            .AddGlobalObjectIdentification(false)
            .BuildRequestExecutorAsync();

        var idSerializer = executor.Schema.Services.GetRequiredService<INodeIdSerializer>();
        var intId = idSerializer.Format("Query", 1);
        var stringId = idSerializer.Format("Query", "abc");
        var guidId = idSerializer.Format("Query", Guid.Empty);

        // act
        var result = await executor.ExecuteAsync(
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
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<QueryType>()
            .AddType<FooPayloadType>()
            .AddGlobalObjectIdentification(false)
            .BuildRequestExecutorAsync();

        var idSerializer = executor.Schema.Services.GetRequiredService<INodeIdSerializer>();
        var someId = idSerializer.Format("Some", 1);

        // act
        var result = await executor.ExecuteAsync(
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
        new
        {
            result = result.ToJson(),
            someId,
        }.MatchSnapshot();
    }

    [Fact]
    public void Id_Type_Is_Correctly_Inferred()
    {
        SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType<FooPayloadType>()
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor
                .Field(t => t.IntId(default))
                .Argument("id", a => a.ID());

            descriptor
                .Field(t => t.StringId(default))
                .Argument("id", a => a.ID());

            descriptor
                .Field(t => t.GuidId(default))
                .Argument("id", a => a.ID());

            descriptor
                .Field(t => t.Foo(default))
                .Argument("input", a => a.Type<FooInputType>())
                .Type<FooPayloadInterfaceType>();
        }
    }

    public class FooInputType : InputObjectType<FooInput>
    {
        protected override void Configure(IInputObjectTypeDescriptor<FooInput> descriptor)
        {
            descriptor
                .Field(t => t.SomeId)
                .ID("Some");
        }
    }

    public class FooPayloadType : ObjectType<FooPayload>
    {
        protected override void Configure(IObjectTypeDescriptor<FooPayload> descriptor)
        {
            descriptor.Implements<FooPayloadInterfaceType>();

            descriptor
                .Field(t => t.SomeId)
                .ID("Bar");
        }
    }

    public class FooPayloadInterfaceType : InterfaceType<IFooPayload>
    {
        protected override void Configure(IInterfaceTypeDescriptor<IFooPayload> descriptor)
        {
            descriptor
                .Field(t => t.SomeId)
                .ID();
        }
    }

    public class Query
    {
        public string IntId(int id) => id.ToString();
        public string StringId(string id) => id;
        public string GuidId(Guid id) => id.ToString();
        public IFooPayload Foo(FooInput input) => new FooPayload { SomeId = input.SomeId, };
    }

    public class FooInput
    {
        public string SomeId { get; set; }
    }

    public class FooPayload : IFooPayload
    {
        public string SomeId { get; set; }
    }

    public interface IFooPayload
    {
        string SomeId { get; set; }
    }
}
