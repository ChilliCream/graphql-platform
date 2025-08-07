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
                                { "guidId", guidId }
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

    [Fact]
    public async Task Id_Honors_CustomTypeNaming_OutputFields()
    {
        // arrange
        var services = new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<MutationWithRenamedIds>()
            .AddMutationConventions()
            .ModifyOptions(o => o.StrictValidation = false)
            .AddGlobalObjectIdentification();

        // act
        var result = await services.ExecuteRequestAsync(
            """
            mutation {
                out: doSomethingElse {
                    renamedUser {
                        userId
                        explicitUserId
                        fooId
                        fluentFooId
                        singleTypeFluentFooId
                        userIdMethod
                        explicitUserIdMethod
                        fooIdMethod
                        fluentFooIdMethod
                        singleTypeFluentFooIdMethod
                    }
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            $$"""
            {
              "data": {
                "out": {
                  "renamedUser": {
                    "userId": "{{Convert.ToBase64String("RenamedUser:1"u8)}}",
                    "explicitUserId": "{{Convert.ToBase64String("RenamedUser:1"u8)}}",
                    "fooId": "{{Convert.ToBase64String("FooFoo:1"u8)}}",
                    "fluentFooId": "{{Convert.ToBase64String("FooFooFluent:1"u8)}}",
                    "singleTypeFluentFooId": "{{Convert.ToBase64String("FooFooFluentSingle:1"u8)}}",
                    "userIdMethod": "{{Convert.ToBase64String("RenamedUser:1"u8)}}",
                    "explicitUserIdMethod": "{{Convert.ToBase64String("RenamedUser:1"u8)}}",
                    "fooIdMethod": "{{Convert.ToBase64String("FooFoo:1"u8)}}",
                    "fluentFooIdMethod": "{{Convert.ToBase64String("FooFooFluent:1"u8)}}",
                    "singleTypeFluentFooIdMethod": "{{Convert.ToBase64String("FooFooFluentSingle:1"u8)}}"
                  }
                }
              }
            }
            """
            );
    }
    [Fact]
    public async Task Id_Honors_CustomTypeNaming_ValidInputs()
    {
        // arrange
        var services = new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<MutationWithRenamedIds>()
            .AddMutationConventions()
            .ModifyOptions(o => o.StrictValidation = false)
            .AddGlobalObjectIdentification();

        var userId = Convert.ToBase64String("RenamedUser:100"u8);
        var fooId = Convert.ToBase64String("FooFoo:300"u8);
        var fluentFooId = Convert.ToBase64String("FooFooFluent:500"u8);
        var singleTypeFluentFooId = Convert.ToBase64String("FooFooFluentSingle:600"u8);

        // act
        var result =
            await services.ExecuteRequestAsync($$"""
                                                 mutation {
                                                     validAnyIdInput1: acceptsAnyId(input: { id:"{{userId}}"}) { int }
                                                     validAnyIdInput2: acceptsAnyId(input: { id:"{{fooId}}"}) { int }
                                                     validAnyIdInput3: acceptsAnyId(input: { id:"{{fluentFooId}}"}) { int }
                                                     validAnyIdInput4: acceptsAnyId(input: { id:"{{singleTypeFluentFooId}}"}) { int }

                                                     validUserIdInput: acceptsUserId(input: { id:"{{userId}}"}) { int }
                                                     validFooIdInput: acceptsFooId(input: { id:"{{fooId}}"}) { int }
                                                     validFluentFooIdInput: acceptsFluentFooId(input: { id:"{{fluentFooId}}"}) { int }
                                                     validSingleTypeFluentFooIdInput: acceptsSingleTypeFluentFooId(input: { id:"{{singleTypeFluentFooId}}"}) { int }
                                                 }
                                                 """);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Id_Honors_CustomTypeNaming_Throws_On_InvalidInputs()
    {
        // arrange
        var services = new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<MutationWithRenamedIds>()
            .AddMutationConventions()
            .ModifyOptions(o => o.StrictValidation = false)
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .AddErrorFilter(x => new Error { Message = x.Message })
            .AddGlobalObjectIdentification();

        var userId = Convert.ToBase64String("RenamedUser:100"u8);
        var fooId = Convert.ToBase64String("FooFoo:300"u8);
        var fluentFooId = Convert.ToBase64String("FooFooFluent:500"u8);
        var singleTypeFluentFooId = Convert.ToBase64String("FooFooFluentSingle:600"u8);

        // act
        var result = await services.ExecuteRequestAsync($$"""
                                                          mutation {
                                                              validUserIdInput: acceptsUserId(input: { id:"{{fooId}}"}) { int }
                                                              validFooIdInput: acceptsFooId(input: { id:"{{fluentFooId}}"}) { int }
                                                              validFluentFooIdInput: acceptsFluentFooId(input: { id:"{{singleTypeFluentFooId}}"}) { int }
                                                              validSingleTypeFluentFooIdInput: acceptsSingleTypeFluentFooId(input: { id:"{{userId}}"}) { int }
                                                          }
                                                          """);

        // assert
        result.MatchSnapshot(postFix: "InvalidArgs");
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
                .Field(t => t.IntId(0))
                .Argument("id", a => a.ID());

            descriptor
                .Field(t => t.StringId(null))
                .Argument("id", a => a.ID());

            descriptor
                .Field(t => t.GuidId(Guid.Empty))
                .Argument("id", a => a.ID<Another>());

            descriptor
                .Field(t => t.Foo(null))
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

    public class Another
    {
        public string Id { get; set; }
    }

    public class MutationWithRenamedIds
    {
        [GraphQLName("doSomethingElse")]
        public IdContainer DoSomething()
        {
            return new IdContainer();
        }

        public int? AcceptsAnyId([ID] int? id = 0) => id;
        public int? AcceptsUserId([ID<IdContainer>] int? id = 0) => id;
        public int? AcceptsFooId([ID<RenamedFoo>] int? id = 0) => id;
        public int? AcceptsFluentFooId([ID<FluentRenamedFooType>] int? id = 0) => id;
        public int? AcceptsSingleTypeFluentFooId([ID<SingleTypeFluentRenamedFooType>] int? id = 0) => id;
    }

    [GraphQLName("RenamedUser")]
    public class IdContainer
    {
        [ID]
        public int UserId { get; set; } = 1;

        [ID<IdContainer>]
        public int ExplicitUserId { get; set; } = 1;

        [ID<RenamedFoo>]
        public int FooId { get; set; } = 1;

        [ID<FluentRenamedFooType>]
        public int FluentFooId { get; set; } = 1;

        [ID<SingleTypeFluentRenamedFooType>]
        public int SingleTypeFluentFooId { get; set; } = 1;

        [ID]
        public int UserIdMethod() => 1;

        [ID<IdContainer>]
        public int ExplicitUserIdMethod() => 1;

        [ID<RenamedFoo>]
        public int FooIdMethod() => 1;

        [ID<FluentRenamedFooType>]
        public int FluentFooIdMethod() => 1;

        [ID<SingleTypeFluentRenamedFooType>]
        public int SingleTypeFluentFooIdMethod() => 1;
    }

    [GraphQLName("FooFoo")]
    public class RenamedFoo;

    public class FluentRenamedFoo;

    public class FluentRenamedFooType : ObjectType<FluentRenamedFoo>
    {
        protected override void Configure(IObjectTypeDescriptor<FluentRenamedFoo> descriptor) =>
            descriptor.Name("FooFooFluent");
    }

    public class SingleTypeFluentRenamedFooType : ObjectType<SingleTypeFluentRenamedFooType>
    {
        protected override void Configure(IObjectTypeDescriptor<SingleTypeFluentRenamedFooType> descriptor) =>
            descriptor.Name("FooFooFluentSingle")
                .BindFieldsExplicitly();
    }
}
