using System;
using System.Collections.Generic;
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

        var intId = Convert.ToBase64String("Query:1"u8);
        var stringId = Convert.ToBase64String("Query:abc"u8);
        var guidId = Convert.ToBase64String(Combine("Query:"u8, Guid.Empty.ToByteArray()));

        // act
        var result =
            await SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddType<FooPayloadType>()
                .AddGlobalObjectIdentification(false)
                .Create()
                .MakeExecutable()
                .ExecuteAsync(
                    OperationRequestBuilder.Create()
                        .SetDocument(
                            @"query foo ($intId: ID! $stringId: ID! $guidId: ID!) {
                                    intId(id: $intId)
                                    stringId(id: $stringId)
                                    guidId(id: $guidId)
                                }")
                        .SetVariableValues(
                            new Dictionary<string, object>
                            {
                                { "intId", intId },
                                { "stringId", stringId },
                                { "guidId", guidId },
                            })
                        .Build());

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

        var someId = Convert.ToBase64String("Some:1"u8);

        // act
        var result =
            await SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddType<FooPayloadType>()
                .AddGlobalObjectIdentification(false)
                .Create()
                .MakeExecutable()
                .ExecuteAsync(
                    OperationRequestBuilder.Create()
                        .SetDocument(
                            @"query foo ($someId: ID!) {
                                foo(input: { someId: $someId }) {
                                    someId
                                }
                            }")
                        .SetVariableValues(new Dictionary<string, object> { { "someId", someId }, })
                        .Build());

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

    private static byte[] Combine(ReadOnlySpan<byte> s1, ReadOnlySpan<byte> s2)
    {
        var buffer = new byte[s1.Length + s2.Length];
        s1.CopyTo(buffer);
        s2.CopyTo(buffer.AsSpan()[s1.Length..]);
        return buffer;
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
