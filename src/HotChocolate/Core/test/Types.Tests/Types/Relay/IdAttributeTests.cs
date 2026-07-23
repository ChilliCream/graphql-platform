using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Relay;

public class IdAttributeTests
{
    [Fact]
    public async Task Id_On_Arguments()
    {
        // arrange
        var intId = Convert.ToBase64String("Query:1"u8);
        var stringId = Convert.ToBase64String("Query:abc"u8);
        var guidId = Convert.ToBase64String(
            Combine("Query:"u8, new Guid("26a2dc8f-4dab-408c-88c6-523a0a89a2b5").ToByteArray()));
        var customId = Convert.ToBase64String("Query:1-2"u8);

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .AddGlobalObjectIdentification(false)
                .AddNodeIdValueSerializer<StronglyTypedIdNodeIdValueSerializer>()
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(
                            """
                            query foo(
                                $intId: ID!
                                $nullIntId: ID = null
                                $stringId: ID!
                                $nullStringId: ID = null
                                $guidId: ID!
                                $nullGuidId: ID = null
                                $customId: ID!
                                $nullCustomId: ID = null
                            ) {
                                intId(id: $intId)
                                nullableIntId(id: $intId)
                                nullableIntIdGivenNull: nullableIntId(id: $nullIntId)
                                optionalIntId(id: $intId)
                                optionalIntIdGivenNothing: optionalIntId
                                intIdList(ids: [$intId])
                                nullableIntIdList(ids: [$intId, $nullIntId])
                                optionalIntIdList(ids: [$intId])
                                stringId(id: $stringId)
                                nullableStringId(id: $stringId)
                                nullableStringIdGivenNull: nullableStringId(id: $nullStringId)
                                optionalStringId(id: $stringId)
                                optionalStringIdGivenNothing: optionalStringId
                                stringIdList(ids: [$stringId])
                                nullableStringIdList(ids: [$stringId, $nullStringId])
                                optionalStringIdList(ids: [$stringId])
                                guidId(id: $guidId)
                                nullableGuidId(id: $guidId)
                                nullableGuidIdGivenNull: nullableGuidId(id: $nullGuidId)
                                optionalGuidId(id: $guidId)
                                optionalGuidIdGivenNothing: optionalGuidId
                                guidIdList(ids: [$guidId $guidId])
                                nullableGuidIdList(ids: [$guidId $nullGuidId $guidId])
                                optionalGuidIdList(ids: [$guidId $guidId])
                                customId(id: $customId)
                                nullableCustomId(id: $customId)
                                nullableCustomIdGivenNull: nullableCustomId(id: $nullCustomId)
                                customIds(ids: [$customId $customId])
                                nullableCustomIds(ids: [$customId $nullCustomId $customId])
                            }
                            """)
                        .SetVariableValues(
                            new Dictionary<string, object?>
                            {
                                { "intId", intId },
                                { "stringId", stringId },
                                { "guidId", guidId },
                                { "customId", customId }
                            })
                        .Build(),
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task InterceptedId_On_Arguments()
    {
        // arrange
        // act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(@"query foo {
                                interceptedId(id: 1)
                                interceptedIds(ids: [1, 2])
                            }")
                        .Build(),
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Id_On_Objects()
    {
        // arrange
        var someId = Convert.ToBase64String("Some:1"u8);
        var someIntId = Convert.ToBase64String("Some:1"u8);

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(
                            """
                            query foo($someId: ID!, $someIntId: ID!) {
                                foo(
                                    input: {
                                        someId: $someId
                                        someIds: [$someIntId]
                                        someNullableId: $someId
                                        someNullableIds: [$someIntId]
                                        someOptionalId: $someId
                                        someOptionalIds: [$someIntId]
                                    }
                                ) {
                                    someId
                                    someNullableId
                                    ... on FooPayload {
                                        someIds
                                        someNullableIds
                                    }
                                }
                            }
                            """)
                        .SetVariableValues(
                            new Dictionary<string, object?>
                            {
                                { "someId", someId },
                                { "someIntId", someIntId }
                            })
                        .Build(),
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new
        {
            result = result.ToJson(),
            someId,
            someIntId
        }.MatchSnapshot();
    }

    [Fact]
    public async Task Id_On_Objects_Given_Nulls()
    {
        // arrange
        var someId = Convert.ToBase64String("Some:1"u8);
        var someIntId = Convert.ToBase64String("Some:1"u8);

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(
                            """
                            query foo(
                                $someId: ID!
                                $someIntId: ID!
                                $someNullableId: ID
                                $someNullableIntId: ID
                            ) {
                                foo(
                                    input: {
                                        someId: $someId
                                        someIds: [$someIntId]
                                        someNullableId: $someNullableId
                                        someNullableIds: [$someNullableIntId, $someIntId]
                                    }
                                ) {
                                    someId
                                    someNullableId
                                    ... on FooPayload {
                                        someIds
                                        someNullableIds
                                    }
                                }
                            }
                            """)
                        .SetVariableValues(
                            new Dictionary<string, object?>
                            {
                                { "someId", someId },
                                { "someNullableId", null },
                                { "someIntId", someIntId },
                                { "someNullableIntId", null }
                            })
                        .Build(),
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new
        {
            result = result.ToJson(),
            someId,
            someIntId
        }.MatchSnapshot();
    }

    [Fact]
    public async Task InterceptedId_On_Objects()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddType<FooPayload>()
            .AddGlobalObjectIdentification(false)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var someId = Convert.ToBase64String("Some:1"u8);
        var someIntId = Convert.ToBase64String("Some:1"u8);

        // act
        var result = await executor
            .ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        @"query foo($someId: ID! $someIntId: ID!) {
                                foo(input: {
                                    someId: $someId
                                    someIds: [$someIntId]
                                    interceptedId: 1
                                    interceptedIds: [1, 2] })
                                {
                                    someId
                                    someIds
                                    interceptedId
                                    interceptedIds
                                }
                            }")
                    .SetVariableValues(
                        new Dictionary<string, object?>
                        {
                            {"someId", someId },
                            {"someIntId", someIntId}
                        })
                    .Build(),
                TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Id_On_Objects_InvalidType()
    {
        // arrange
        var someId = Convert.ToBase64String(Combine("Query:"u8, Guid.Empty.ToByteArray()));

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(
                            """
                            query foo ($someId: ID!) {
                                foo(input: { someId: $someId someIds: [$someId] }) {
                                    someId
                                    ... on FooPayload {
                                        someIds
                                    }
                                }
                            }
                            """)
                        .SetVariableValues(new Dictionary<string, object?> { { "someId", someId } })
                        .Build(),
                    cancellationToken: TestContext.Current.CancellationToken);

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
        const string someId = "abc";

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(
                            """
                            query foo ($someId: ID!) {
                                foo(input: { someId: $someId someIds: [$someId] }) {
                                    someId
                                    ... on FooPayload {
                                        someIds
                                    }
                                }
                            }
                            """)
                        .SetVariableValues(new Dictionary<string, object?> { { "someId", someId } })
                        .Build(),
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new
        {
            result = result.ToJson(),
            someId
        }.MatchSnapshot();
    }

    [Fact]
    public async Task Id_On_Objects_Legacy_StringAndIntId()
    {
        // arrange
        var legacySomeStringId = Convert.ToBase64String("Some\ndtest"u8);
        var legacySomeIntId = Convert.ToBase64String("Some\ni123"u8);

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(
                            """
                            query foo ($someId: ID! $someIntId: ID!) {
                                foo(input: { someId: $someId someIds: [$someIntId] }) {
                                    someId
                                    ... on FooPayload {
                                        someIds
                                    }
                                }
                            }
                            """)
                        .SetVariableValues(new Dictionary<string, object?>
                        {
                            {"someId", legacySomeStringId},
                            {"someIntId", legacySomeIntId}
                        })
                        .Build(),
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new
        {
            result = result.ToJson(),
            legacySomeStringId,
            legacySomeIntId
        }.MatchSnapshot();
    }

    [Fact]
    public async Task Id_On_Objects_Legacy_StronglyTypedId()
    {
        // arrange
        var legacyStronglyTypedId = Convert.ToBase64String("Product\nd123-456"u8);

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .AddGlobalObjectIdentification(false)
                .AddNodeIdValueSerializer<StronglyTypedIdNodeIdValueSerializer>()
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(
                            """
                            query foo ($customId: ID!, $nullCustomId: ID = null) {
                                nullableCustomId(id: $customId)
                                nullableCustomIdGivenNull: nullableCustomId(id: $nullCustomId)
                                customIds(ids: [$customId $customId])
                                nullableCustomIds(ids: [$customId $nullCustomId $customId])
                            }
                            """)
                        .SetVariableValues(new Dictionary<string, object?>
                        {
                            {"customId", legacyStronglyTypedId}
                        })
                        .Build(),
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new
        {
            result = result.ToJson(),
            legacySomeStronglyTypedId = legacyStronglyTypedId
        }.MatchSnapshot();
    }

    [Fact]
    public async Task Id_Type_Is_Correctly_Inferred()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .AddGlobalObjectIdentification(false)
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Id_On_Interface_Forwards_TypeName_To_Multiple_Implementers()
    {
        // arrange & act
        // two object types inherit the same default-implementation interface field; both encode
        // the value with the interface's forwarded type name from the shared serializer.
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<MultiQuery>()
                .AddType<MultiA>()
                .AddType<MultiB>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    """
                    {
                        a { id }
                        b { id }
                    }
                    """);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors);
        using var document = JsonDocument.Parse(operationResult.ToJson());
        var data = document.RootElement.GetProperty("data");
        Assert.Equal(
            "Shared:7",
            Encoding.UTF8.GetString(
                Convert.FromBase64String(data.GetProperty("a").GetProperty("id").GetString()!)));
        Assert.Equal(
            "Shared:7",
            Encoding.UTF8.GetString(
                Convert.FromBase64String(data.GetProperty("b").GetProperty("id").GetString()!)));
    }

    [Fact]
    public async Task Id_On_Interface_Override_Uses_Concrete_Name_Inherit_Uses_Interface_Name()
    {
        // arrange & act
        // A overrides the interface member and declares its own [ID("Bar")] (merge path).
        // B inherits the interface default implementation [ID("Foo")] (copy path).
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<OverrideQuery>()
                .AddType<OverrideA>()
                .AddType<OverrideB>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    """
                    {
                        a { id }
                        b { id }
                    }
                    """);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors);
        using var document = JsonDocument.Parse(operationResult.ToJson());
        var data = document.RootElement.GetProperty("data");
        // A encodes with the concrete name "Bar" and is not double-encoded.
        Assert.Equal(
            "Bar:1",
            Encoding.UTF8.GetString(
                Convert.FromBase64String(data.GetProperty("a").GetProperty("id").GetString()!)));
        // B encodes with the interface default implementation's name "Foo".
        Assert.Equal(
            "Foo:100",
            Encoding.UTF8.GetString(
                Convert.FromBase64String(data.GetProperty("b").GetProperty("id").GetString()!)));
    }

    [Fact]
    public async Task Id_On_Interface_Forwards_TypeName_To_SDL()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<NamedNodeQuery>()
                .AddType<NamedNode>()
                .AddType<GenericNamedNode>()
                .AddGlobalObjectIdentification(false)
                .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Id_On_Interface_With_String_Name_Forwards_TypeName_When_Inherited()
    {
        // arrange & act
        // the implementing object type does not redeclare the id field, so the interface's
        // forwarded type name must flow through to the object field serializer.
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<NamedNodeQuery>()
                .AddType<NamedNode>()
                .AddType<GenericNamedNode>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    """
                    {
                        namedNode {
                            id
                        }
                    }
                    """);

        // assert
        // the decoded id proves the forwarded type name ("Foo") is used, not the inferred one.
        Assert.Equal("Foo:1", Encoding.UTF8.GetString(Convert.FromBase64String(GetId(result, "namedNode"))));
    }

    [Fact]
    public async Task Id_On_Interface_With_Generic_Name_Forwards_TypeName_When_Inherited()
    {
        // arrange & act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<GenericNamedNodeQuery>()
                .AddType<GenericNamedNode>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    """
                    {
                        genericNamedNode {
                            id
                        }
                    }
                    """);

        // assert
        // the decoded id proves the generic type argument's GraphQL name ("Renamed") is used.
        Assert.Equal(
            "Renamed:1",
            Encoding.UTF8.GetString(Convert.FromBase64String(GetId(result, "genericNamedNode"))));
    }

    [Fact]
    public async Task Id_On_Abstract_Interface_Member_Does_Not_Encode_Inherited_Value()
    {
        // arrange & act
        // the interface declares an abstract [ID] member, so the implementing type supplies the
        // value and must opt into the global id encoding itself. The inherited value stays raw.
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<AbstractIdNodeQuery>()
                .AddType<AbstractIdNodeType>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    """
                    {
                        abstractIdNode {
                            id
                        }
                    }
                    """);

        // assert
        // the value is the raw "1", not the global id encoded "Foo:1".
        Assert.Equal("1", GetId(result, "abstractIdNode"));
    }

    [Fact]
    public async Task Id_On_Abstract_Interface_Member_Still_Rewrites_Type_To_ID()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<AbstractIdNodeQuery>()
                .AddType<AbstractIdNodeType>()
                .AddGlobalObjectIdentification(false)
                .BuildSchemaAsync();

        // assert
        // the type rewrite is unconditional, only the value serialization is gated off.
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Id_On_Abstract_Interface_Member_Encodes_When_Object_Opts_In()
    {
        // arrange & act
        // the same abstract interface member, but the implementing object declares its own [ID],
        // so the object opts into the global id encoding.
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<OptInIdNodeQuery>()
                .AddType<OptInIdNodeType>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    """
                    {
                        optInIdNode {
                            id
                        }
                    }
                    """);

        // assert
        // the object's own [ID] uses the inferred object type name "OptInIdNode".
        Assert.Equal(
            "OptInIdNode:1",
            Encoding.UTF8.GetString(Convert.FromBase64String(GetId(result, "optInIdNode"))));
    }

    [Fact]
    public async Task EnsureIdIsOnlyAppliedOnce()
    {
        var inspector = new TestTypeInterceptor();

        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("abc").ID().ID().ID().ID().Resolve("abc");
            })
            .AddGlobalObjectIdentification(false)
            .TryAddTypeInterceptor(inspector)
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, inspector.Count);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    [Fact]
    public async Task Bare_Id_On_Properties_With_Different_Runtime_Types()
    {
        // arrange & act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<ThingQuery>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(
                            """
                            {
                                thing {
                                    id
                                    anotherTypeId
                                }
                            }
                            """)
                        .Build(),
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The value could not be formatted into an ID for the type `Thing`.",
                  "path": [
                    "thing",
                    "anotherTypeId"
                  ],
                  "extensions": {
                    "originalValue": "26a2dc8f-4dab-408c-88c6-523a0a89a2b5"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task Invalid_Id_Does_Not_Erase_Sibling_Data()
    {
        // arrange & act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<ProbeQuery>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    """
                    {
                      byId(id: "invalid")
                      unrelated
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The node ID string has an invalid format.",
                  "path": [
                    "byId"
                  ],
                  "extensions": {
                    "originalValue": "invalid"
                  }
                }
              ],
              "data": {
                "byId": null,
                "unrelated": "value"
              }
            }
            """);
    }

    [Fact]
    public async Task Invalid_Id_On_Skipped_Field_Does_Not_Error()
    {
        // arrange & act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<ProbeQuery>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync(
                    """
                    {
                      byId(id: "invalid") @skip(if: true)
                      unrelated
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "unrelated": "value"
              }
            }
            """);
    }

    public class ProbeQuery
    {
        public int? GetById([ID] int id) => id;

        public string GetUnrelated() => "value";
    }

    public class ThingQuery
    {
        public Thing GetThing() => new();
    }

    public class Thing
    {
        [ID]
        public int Id { get; set; } = 1;

        [ID]
        public Guid AnotherTypeId { get; set; } = new("26a2dc8f-4dab-408c-88c6-523a0a89a2b5");
    }

    public class Query
    {
        public int IntId([ID] int id) => id;

        public int[] IntIdList([ID] int[] ids) => ids;

        public int? NullableIntId([ID] int? id) => id;

        public int?[] NullableIntIdList([ID] int?[] ids) => ids;

        public int? OptionalIntId([DefaultValue("UXVlcnk6MA==")][ID] Optional<int> id)
            => id.HasValue ? id.Value : null;

        public int[]? OptionalIntIdList([DefaultValue(new int[] { })][ID] Optional<int[]> ids)
            => ids.HasValue ? ids.Value : null;

        public string StringId([ID] string id) => id;

        public string[] StringIdList([ID] string[] ids) => ids;

        public string? NullableStringId([ID] string? id) => id;

        public string?[] NullableStringIdList([ID] string?[] ids) => ids;

        public string? OptionalStringId([DefaultValue("UXVlcnk6")][ID] Optional<string> id)
            => id.HasValue ? id.Value : null;

        public string[]? OptionalStringIdList([DefaultValue(new string[] { })][ID] Optional<string[]> ids)
            => ids.HasValue ? ids.Value : null;

        public Guid GuidId([ID] Guid id) => id;

        public IReadOnlyList<Guid> GuidIdList([ID] IReadOnlyList<Guid> ids) => ids;

        public Guid? NullableGuidId([ID] Guid? id) => id;

        public IReadOnlyList<Guid?> NullableGuidIdList([ID] IReadOnlyList<Guid?> ids) => ids;

        public Guid? OptionalGuidId([DefaultValue("UXVlcnk6AAAAAAAAAAAAAAAAAAAAAA==")][ID] Optional<Guid> id)
            => id.HasValue ? id.Value : null;

        public Guid[]? OptionalGuidIdList([DefaultValue(new object[] { })][ID] Optional<Guid[]> ids)
            => ids.HasValue ? ids.Value : null;

        public int InterceptedId([InterceptedID("Query")][ID] int id) => id;

        public int[] InterceptedIds([InterceptedID("Query")][ID] int[] ids) => ids;

        public string CustomId([ID] StronglyTypedId id) => id.ToString();

        public string NullableCustomId([ID] StronglyTypedId? id) =>
            id?.ToString() ?? "null";

        public string CustomIds([ID] List<StronglyTypedId> ids) =>
            string.Join(", ", ids.Select(t => t.ToString()));

        public string NullableCustomIds([ID] List<StronglyTypedId?> ids) =>
            string.Join(", ", ids.Select(t => t?.ToString() ?? "null"));

        public IFooPayload Foo(FooInput input) =>
            new FooPayload(
                input.SomeId,
                input.SomeNullableId,
                input.SomeIds,
                input.SomeNullableIds,
                input.InterceptedId,
                input.InterceptedIds);
    }

    public class FooInput
    {
        public FooInput(
            string someId,
            string? someNullableId,
            Optional<string> someOptionalId,
            IReadOnlyList<int> someIds,
            IReadOnlyList<int?>? someNullableIds,
            Optional<IReadOnlyList<int>> someOptionalIds,
            int? interceptedId,
            IReadOnlyList<int>? interceptedIds)
        {
            SomeId = someId;
            SomeNullableId = someNullableId;
            SomeOptionalId = someOptionalId;
            SomeIds = someIds;
            SomeNullableIds = someNullableIds;
            SomeOptionalIds = someOptionalIds;
            InterceptedId = interceptedId;
            InterceptedIds = interceptedIds;
        }

        [ID("Some")] public string SomeId { get; }

        [ID("Some")] public string? SomeNullableId { get; }

        [ID("Some")]
        [DefaultValue("U29tZTo=")]
        public Optional<string> SomeOptionalId { get; }

        [ID("Some")] public IReadOnlyList<int> SomeIds { get; }

        [ID("Some")] public IReadOnlyList<int?>? SomeNullableIds { get; }

        [ID("Some")]
        [DefaultValue(new int[] { })]
        public Optional<IReadOnlyList<int>> SomeOptionalIds { get; }

        [ID, InterceptedID("FooInput")]
        public int? InterceptedId { get; }

        [ID, InterceptedID("FooInput")]
        public IReadOnlyList<int>? InterceptedIds { get; }
    }

    public class FooPayload : IFooPayload
    {
        public FooPayload(
            string someId,
            string? someNullableId,
            IReadOnlyList<int> someIds,
            IReadOnlyList<int?>? someNullableIds,
            int? interceptedId,
            IReadOnlyList<int>? interceptedIds)
        {
            SomeId = someId;
            SomeNullableId = someNullableId;
            SomeIds = someIds;
            SomeNullableIds = someNullableIds;
            InterceptedId = interceptedId;
            InterceptedIds = interceptedIds;
        }

        [ID("Bar")] public string SomeId { get; }

        [ID("Baz")] public IReadOnlyList<int> SomeIds { get; }

        [ID("Bar")] public string? SomeNullableId { get; }

        [ID("Baz")] public IReadOnlyList<int?>? SomeNullableIds { get; }

        public int? InterceptedId { get; }

        public IReadOnlyList<int>? InterceptedIds { get; }

        public string Raw =>
            $"{nameof(SomeId)}: {SomeId}, "
            + $"{nameof(SomeIds)}: [{string.Join(", ", SomeIds)}], "
            + $"{nameof(SomeNullableId)}: {SomeNullableId}, "
            + $"{nameof(SomeNullableIds)}: [{string.Join(", ", SomeNullableIds ?? Array.Empty<int?>())}]"
            + $"{nameof(InterceptedId)}: {InterceptedId}"
            + $"{nameof(InterceptedIds)}: [{string.Join(", ", InterceptedIds ?? Array.Empty<int>())}]";
    }

    public interface IFooPayload
    {
        [ID] string SomeId { get; }

        [ID] string? SomeNullableId { get; }

        [ID] IReadOnlyList<int> SomeIds { get; }

        [ID] IReadOnlyList<int?>? SomeNullableIds { get; }

        int? InterceptedId { get; }

        IReadOnlyList<int>? InterceptedIds { get; }

        string Raw { get; }
    }

    public class StronglyTypedIdNodeIdValueSerializer : INodeIdValueSerializer
    {
        public bool IsSupported(Type type) => type == typeof(StronglyTypedId);

        public NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written)
        {
            if (value is StronglyTypedId stronglyTypedId)
            {
                var formattedValue = stronglyTypedId.ToString();
                written = Encoding.UTF8.GetBytes(formattedValue, buffer);
                return NodeIdFormatterResult.Success;
            }

            written = 0;
            return NodeIdFormatterResult.InvalidValue;
        }

        public bool TryParse(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out object? value)
        {
            var formattedValue = Encoding.UTF8.GetString(buffer);
            value = StronglyTypedId.Parse(formattedValue);
            return true;
        }
    }

    public record StronglyTypedId(int Part1, int Part2)
    {
        public override string ToString()
        {
            return $"{Part1}-{Part2}";
        }

        public static StronglyTypedId Parse(string value)
        {
            var parts = value.Split('-');
            return new StronglyTypedId(int.Parse(parts[0]), int.Parse(parts[1]));
        }
    }

    [AttributeUsage(
        AttributeTargets.Parameter
        | AttributeTargets.Property
        | AttributeTargets.Method)]
    public class InterceptedIDAttribute(string typeName) : DescriptorAttribute
    {
        public string TypeName { get; } = typeName;

        protected internal override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider? attributeProvider)
        {
            switch (descriptor)
            {
                case IInputFieldDescriptor dc when attributeProvider is PropertyInfo:
                    dc.Extend().OnBeforeCompletion((_, d) => AddInterceptingSerializer(d));
                    break;
                case IArgumentDescriptor dc when attributeProvider is ParameterInfo:
                    dc.Extend().OnBeforeCompletion((_, d) => AddInterceptingSerializer(d));
                    break;
            }
        }

        private void AddInterceptingSerializer(ArgumentConfiguration definition)
            => definition.Formatters.Insert(0, new InterceptingFormatter(TypeName));

        private sealed class InterceptingFormatter(string typeName) : IInputValueFormatter
        {
            public object Format(object? originalValue)
            {
                return originalValue switch
                {
                    IEnumerable<string> list => list.Select(x => new NodeId(typeName, int.Parse(x))).ToArray(),
                    _ => new NodeId(typeName, int.Parse((string)originalValue!))
                };
            }
        }
    }

    public class TestTypeInterceptor : TypeInterceptor
    {
        public int Count { get; set; }

        public override void OnValidateType(
            ITypeSystemObjectContext context,
            TypeSystemConfiguration configuration)
        {
            if (context.Type.Name.EqualsOrdinal("Query")
                && configuration is ObjectTypeConfiguration typeDef)
            {
                Count = typeDef.Fields
                    .Single(t => t.Name.EqualsOrdinal("abc"))
                    .GetResultConverters()
                    .Count;
            }
        }
    }

    private static byte[] Combine(ReadOnlySpan<byte> s1, ReadOnlySpan<byte> s2)
    {
        var buffer = new byte[s1.Length + s2.Length];
        s1.CopyTo(buffer);
        s2.CopyTo(buffer.AsSpan()[s1.Length..]);
        return buffer;
    }

    private static string GetId(IExecutionResult result, string fieldName)
    {
        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors);
        using var document = JsonDocument.Parse(operationResult.ToJson());
        return document.RootElement
            .GetProperty("data")
            .GetProperty(fieldName)
            .GetProperty("id")
            .GetString()!;
    }

    public class NamedNodeQuery
    {
        public INamedNode GetNamedNode() => new NamedNode();

        public IGenericNamedNode GetGenericNamedNode() => new GenericNamedNode();
    }

    [InterfaceType("NamedNodeInterface")]
    public interface INamedNode
    {
        [ID("Foo")]
        int GetId() => 1;
    }

    public class NamedNode : INamedNode;

    public class GenericNamedNodeQuery
    {
        public IGenericNamedNode GetGenericNamedNode() => new GenericNamedNode();
    }

    [InterfaceType("GenericNamedNodeInterface")]
    public interface IGenericNamedNode
    {
        [ID<RenamedIdTarget>]
        int GetId() => 1;
    }

    public class GenericNamedNode : IGenericNamedNode;

    [GraphQLName("Renamed")]
    public class RenamedIdTarget
    {
        public int Id { get; set; } = 1;
    }

    public class AbstractIdNodeQuery
    {
        public IAbstractIdNode GetAbstractIdNode() => new AbstractIdNode();
    }

    [InterfaceType("AbstractIdNodeInterface")]
    public interface IAbstractIdNode
    {
        [ID("Foo")]
        int Id { get; }
    }

    public class AbstractIdNodeType : ObjectType<AbstractIdNode>
    {
        protected override void Configure(IObjectTypeDescriptor<AbstractIdNode> descriptor)
        {
            descriptor.Name("AbstractIdNode");
            descriptor.BindFieldsExplicitly();
            descriptor.Implements<InterfaceType<IAbstractIdNode>>();
        }
    }

    public class AbstractIdNode : IAbstractIdNode
    {
        public int Id => 1;
    }

    public class OptInIdNodeQuery
    {
        public IAbstractIdNode GetOptInIdNode() => new OptInIdNode();
    }

    public class OptInIdNodeType : ObjectType<OptInIdNode>
    {
        protected override void Configure(IObjectTypeDescriptor<OptInIdNode> descriptor)
        {
            descriptor.Name("OptInIdNode");
            descriptor.Implements<InterfaceType<IAbstractIdNode>>();
            descriptor.Field(t => t.Id).ID();
        }
    }

    public class MultiQuery
    {
        public IMultiNode GetA() => new MultiA();
        public IMultiNode GetB() => new MultiB();
    }

    [InterfaceType("MultiNode")]
    public interface IMultiNode
    {
        [ID("Shared")]
        int GetId() => 7;
    }

    public class MultiA : IMultiNode;

    public class MultiB : IMultiNode;

    public class OptInIdNode : IAbstractIdNode
    {
        public int Id => 1;
    }

    public class OverrideQuery
    {
        public IOverrideNode GetA() => new OverrideA();
        public IOverrideNode GetB() => new OverrideB();
    }

    [InterfaceType("OverrideNode")]
    public interface IOverrideNode
    {
        [ID("Foo")]
        int GetId() => 100;
    }

    // A overrides the member and declares its own [ID("Bar")]: A goes through the merge path
    // (the object redeclares the field) and must encode with the concrete name "Bar".
    public class OverrideA : IOverrideNode
    {
        [ID("Bar")]
        public int GetId() => 1;
    }

    // B inherits the default implementation: B goes through the copy path and must encode with
    // the interface name "Foo".
    public class OverrideB : IOverrideNode;
}
