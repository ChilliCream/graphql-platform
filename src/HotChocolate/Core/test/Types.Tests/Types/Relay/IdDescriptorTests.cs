using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Relay;

public class IdDescriptorTests
{
    [Fact]
    public async Task Id_On_Arguments()
    {
        // arrange
        var intId = Convert.ToBase64String("Query:1"u8);
        var stringId = Convert.ToBase64String("Query:abc"u8);
        var guidId = Convert.ToBase64String(Combine("Another:"u8, Guid.Empty.ToByteArray()));

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QueryType>()
                .AddType<FooPayloadType>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
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
        var someId = Convert.ToBase64String("Some:1"u8);
        var anotherId = Convert.ToBase64String("Another:1"u8);

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QueryType>()
                .AddType<FooPayloadType>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(
                            """
                            query foo ($someId: ID!, $anotherId: ID!) {
                                foo(input: { someId: $someId, anotherId: $anotherId }) {
                                    someId
                                    anotherId
                                }
                            }
                            """)
                        .SetVariableValues(new Dictionary<string, object>
                        {
                            { "someId", someId },
                            { "anotherId", anotherId }
                        })
                        .Build());

        // assert
        new
        {
            result = result.ToJson(),
            someId,
            anotherId
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
                .Argument("id", a => a.ID<Another>());

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

            descriptor
                .Field(t => t.AnotherId)
                .ID<Another>();
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

            descriptor
                .Field(t => t.AnotherId)
                .ID<Another>();
        }
    }

    public class FooPayloadInterfaceType : InterfaceType<IFooPayload>
    {
        protected override void Configure(IInterfaceTypeDescriptor<IFooPayload> descriptor)
        {
            descriptor
                .Field(t => t.SomeId)
                .ID();

            descriptor
                .Field(t => t.AnotherId)
                .ID();
        }
    }

    public class Query
    {
        public string IntId(int id) => id.ToString();
        public string StringId(string id) => id;
        public string GuidId(Guid id) => id.ToString();
        public IFooPayload Foo(FooInput input)
            => new FooPayload { SomeId = input.SomeId, AnotherId = input.AnotherId };
    }

    public class FooInput
    {
        public string SomeId { get; set; }
        public string AnotherId { get; set; }
    }

    public class FooPayload : IFooPayload
    {
        public string SomeId { get; set; }
        public string AnotherId { get; set; }
    }

    public interface IFooPayload
    {
        string SomeId { get; set; }
        string AnotherId { get; set; }
    }

    private class Another;
}
