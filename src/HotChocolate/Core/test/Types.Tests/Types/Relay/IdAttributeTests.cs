using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

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
                                intIdList(id: [$intId])
                                nullableIntIdList(id: [$intId, $nullIntId])
                                optionalIntIdList(id: [$intId])
                                stringId(id: $stringId)
                                nullableStringId(id: $stringId)
                                nullableStringIdGivenNull: nullableStringId(id: $nullStringId)
                                optionalStringId(id: $stringId)
                                optionalStringIdGivenNothing: optionalStringId
                                stringIdList(id: [$stringId])
                                nullableStringIdList(id: [$stringId, $nullStringId])
                                optionalStringIdList(id: [$stringId])
                                guidId(id: $guidId)
                                nullableGuidId(id: $guidId)
                                nullableGuidIdGivenNull: nullableGuidId(id: $nullGuidId)
                                optionalGuidId(id: $guidId)
                                optionalGuidIdGivenNothing: optionalGuidId
                                guidIdList(id: [$guidId $guidId])
                                nullableGuidIdList(id: [$guidId $nullGuidId $guidId])
                                optionalGuidIdList(id: [$guidId $guidId])
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
                        .Build());

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
                                interceptedIds(id: [1, 2])
                            }")
                        .Build());

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
                        .Build());

        // assert
        new
        {
            result = result.ToJson(),
            someId,
            someIntId,
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
                        .Build());

        // assert
        new
        {
            result = result.ToJson(),
            someId,
            someIntId,
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
            .BuildRequestExecutorAsync();

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
                            {"someIntId", someIntId},
                        })
                    .Build());

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
                        .SetVariableValues(new Dictionary<string, object?> { {"someId", someId }, })
                        .Build());

        // assert
        new
        {
            result = result.ToJson(),
            someId,
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
                        .SetVariableValues(new Dictionary<string, object?> { {"someId", someId}, })
                        .Build());

        // assert
        new
        {
            result = result.ToJson(),
            someId,
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
                            {"someIntId", legacySomeIntId},
                        })
                        .Build());

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
                            {"customId", legacyStronglyTypedId},
                        })
                        .Build());

        // assert
        new
        {
            result = result.ToJson(), legacySomeStronglyTypedId = legacyStronglyTypedId,
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
                .BuildSchemaAsync();

        schema.ToString().MatchSnapshot();
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
            .BuildSchemaAsync();

        Assert.Equal(1, inspector.Count);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public class Query
    {
        public string IntId([ID] int id) => id.ToString();
        public string IntIdList([ID] int[] id) =>
            string.Join(", ", id.Select(t => t.ToString()));

        public string NullableIntId([ID] int? id) => id?.ToString() ?? "null";
        public string NullableIntIdList([ID] int?[] id) =>
            string.Join(", ", id.Select(t => t?.ToString() ?? "null"));

        public string OptionalIntId([DefaultValue("UXVlcnk6MA==")][ID] Optional<int> id) =>
            id.HasValue ? id.Value.ToString() : "NO VALUE";
        public string OptionalIntIdList([DefaultValue(new int[] {})][ID] Optional<int[]> id) =>
            id.HasValue ? string.Join(", ", id.Value.Select(t => t.ToString())) : "NO VALUE";

        public string StringId([ID] string id) => id;
        public string StringIdList([ID] string[] id) =>
            string.Join(", ", id.Select(t => t.ToString()));

        public string NullableStringId([ID] string? id) => id ?? "null";
        public string NullableStringIdList([ID] string?[] id) =>
            string.Join(", ", id.Select(t => t?.ToString() ?? "null"));

        public string OptionalStringId(
            [DefaultValue("UXVlcnk6")][ID] Optional<string> id) =>
            id.HasValue ? id.Value : "NO VALUE";
        public string OptionalStringIdList(
            [DefaultValue(new string[] {})][ID] Optional<string[]> id) =>
            id.HasValue ? string.Join(", ", id.Value) : "NO VALUE";

        public string GuidId([ID] Guid id) => id.ToString();
        public string GuidIdList([ID] IReadOnlyList<Guid> id) =>
            string.Join(", ", id.Select(t => t.ToString()));

        public string NullableGuidId([ID] Guid? id) => id?.ToString() ?? "null";
        public string NullableGuidIdList([ID] IReadOnlyList<Guid?> id) =>
            string.Join(", ", id.Select(t => t?.ToString() ?? "null"));

        public string OptionalGuidId(
            [DefaultValue("UXVlcnk6AAAAAAAAAAAAAAAAAAAAAA==")][ID] Optional<Guid> id) =>
            id.HasValue ? id.Value.ToString() : "NO VALUE";
        public string OptionalGuidIdList(
            [DefaultValue(new object[] {})][ID] Optional<Guid[]> id) =>
            id.HasValue ? string.Join(", ", id.Value.Select(t => t.ToString())) : "NO VALUE";

        public string InterceptedId([InterceptedID("Query")] [ID] int id) => id.ToString();

        public string InterceptedIds([InterceptedID("Query")] [ID] int[] id) =>
            string.Join(", ", id.Select(t => t.ToString()));

        public string CustomId([ID] StronglyTypedId id) =>
            id.ToString();

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
        [DefaultValue(new int[] {})]
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
            $"{nameof(SomeId)}: {SomeId}, " +
            $"{nameof(SomeIds)}: [{string.Join(", ", SomeIds)}], " +
            $"{nameof(SomeNullableId)}: {SomeNullableId}, " +
            $"{nameof(SomeNullableIds)}: [{string.Join(", ", SomeNullableIds ?? Array.Empty<int?>())}]" +
            $"{nameof(InterceptedId)}: {InterceptedId}" +
            $"{nameof(InterceptedIds)}: [{string.Join(", ", InterceptedIds ?? Array.Empty<int>())}]";
    }

    public interface IFooPayload
    {
        [ID] string SomeId { get; }

        [ID] public string? SomeNullableId { get; }

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
        AttributeTargets.Parameter |
        AttributeTargets.Property |
        AttributeTargets.Method)]
    public class InterceptedIDAttribute(string typeName) : DescriptorAttribute
    {
        public string TypeName { get; } = typeName;

        protected internal override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            switch (descriptor)
            {
                case IInputFieldDescriptor dc when element is PropertyInfo:
                    dc.Extend().OnBeforeCompletion((_, d) => AddInterceptingSerializer(d));
                    break;
                case IArgumentDescriptor dc when element is ParameterInfo:
                    dc.Extend().OnBeforeCompletion((_, d) => AddInterceptingSerializer(d));
                    break;
            }
        }

        private void AddInterceptingSerializer(ArgumentDefinition definition)
            => definition.Formatters.Insert(0, new InterceptingFormatter(TypeName));

        private sealed class InterceptingFormatter(string typeName) : IInputValueFormatter
        {
            public object Format(object? originalValue)
            {
                return originalValue switch
                {
                    IEnumerable<string> list => list.Select(x => new NodeId(typeName, int.Parse(x))).ToArray(),
                    _ => new NodeId(typeName, int.Parse((string)originalValue!)),
                };
            }
        }
    }

    public class TestTypeInterceptor : TypeInterceptor
    {
        public int Count { get; set; }

        public override void OnValidateType(
            ITypeSystemObjectContext validationContext,
            DefinitionBase definition)
        {
            if (validationContext.Type.Name.EqualsOrdinal("Query") &&
                definition is ObjectTypeDefinition typeDef)
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
}
