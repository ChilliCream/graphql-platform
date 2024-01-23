using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.Types.Relay;

public class IdAttributeTests
{
    [Fact]
    public async Task Id_On_Arguments()
    {
        // arrange
        var idSerializer = new IdSerializer();
        var intId = idSerializer.Serialize("Query", 1);
        var stringId = idSerializer.Serialize("Query", "abc");
        var guidId = idSerializer.Serialize(
            "Query",
            new Guid("26a2dc8f-4dab-408c-88c6-523a0a89a2b5"));

        // act
        var result =
            await SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .AddGlobalObjectIdentification(false)
                .Create()
                .MakeExecutable()
                .ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(@"query foo (
                                $intId: ID!
                                $nullIntId: ID = null
                                $stringId: ID!
                                $nullStringId: ID = null
                                $guidId: ID!
                                $nullGuidId: ID = null)
                            {
                                intId(id: $intId)
                                nullableIntId(id: $intId)
                                nullableIntIdGivenNull: nullableIntId(id: $nullIntId)
                                intIdList(id: [$intId])
                                nullableIntIdList(id: [$intId, $nullIntId])
                                stringId(id: $stringId)
                                nullableStringId(id: $stringId)
                                nullableStringIdGivenNull: nullableStringId(id: $nullStringId)
                                stringIdList(id: [$stringId])
                                nullableStringIdList(id: [$stringId, $nullStringId])
                                guidId(id: $guidId)
                                nullableGuidId(id: $guidId)
                                nullableGuidIdGivenNull: nullableGuidId(id: $nullGuidId)
                                guidIdList(id: [$guidId $guidId])
                                nullableGuidIdList(id: [$guidId $nullGuidId $guidId])
                            }")
                        .SetVariableValue("intId", intId)
                        .SetVariableValue("stringId", stringId)
                        .SetVariableValue("guidId", guidId)
                        .Create());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task InterceptedId_On_Arguments()
    {
        // arrange
        // act
        var result =
            await SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .AddGlobalObjectIdentification(false)
                .Create()
                .MakeExecutable()
                .ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(@"query foo {
                                interceptedId(id: 1)
                                interceptedIds(id: [1, 2])
                            }")
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
        var result =
            await SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .AddGlobalObjectIdentification(false)
                .Create()
                .MakeExecutable()
                .ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"query foo ($someId: ID! $someIntId: ID!) {
                                foo(input: {
                                    someId: $someId someIds: [$someIntId]
                                    someNullableId: $someId someNullableIds: [$someIntId] })
                                {
                                    someId
                                    someNullableId
                                    ... on FooPayload {
                                        someIds
                                        someNullableIds
                                    }
                                }
                            }")
                        .SetVariableValue("someId", someId)
                        .SetVariableValue("someNullableId", null)
                        .SetVariableValue("someIntId", someIntId)
                        .SetVariableValue("someNullableIntId", null)
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
    public async Task Id_On_Objects_Given_Nulls()
    {
        // arrange
        var idSerializer = new IdSerializer();
        var someId = idSerializer.Serialize("Some", "1");
        var someIntId = idSerializer.Serialize("Some", 1);

        // act
        var result =
            await SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .AddGlobalObjectIdentification(false)
                .Create()
                .MakeExecutable()
                .ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"query foo (
                                $someId: ID! $someIntId: ID!
                                $someNullableId: ID
                                $someNullableIntId: ID) {
                                foo(input: {
                                    someId: $someId someIds: [$someIntId]
                                    someNullableId: $someNullableId
                                    someNullableIds: [$someNullableIntId, $someIntId] })
                                {
                                    someId
                                    someNullableId
                                    ... on FooPayload {
                                        someIds
                                        someNullableIds
                                    }
                                }
                            }")
                        .SetVariableValue("someId", someId)
                        .SetVariableValue("someNullableId", null)
                        .SetVariableValue("someIntId", someIntId)
                        .SetVariableValue("someNullableIntId", null)
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
    public async Task InterceptedId_On_Objects()
    {
        // arrange
        var idSerializer = new IdSerializer();
        var someId = idSerializer.Serialize("Some", "1");
        var someIntId = idSerializer.Serialize("Some", 1);

        var executor = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType<FooPayload>()
            .AddGlobalObjectIdentification(false)
            .Create()
            .MakeExecutable();

        // act
        var result = await executor
            .ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
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
                    .SetVariableValue("someId", someId)
                    .SetVariableValue("someIntId", someIntId)
                    .Create());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Id_On_Objects_InvalidType()
    {
        // arrange
        var idSerializer = new IdSerializer();
        var someId = idSerializer.Serialize("Some", Guid.Empty);

        // act
        var result =
            await SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .AddGlobalObjectIdentification(false)
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
        var result =
            await SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .AddGlobalObjectIdentification(false)
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
            .AddGlobalObjectIdentification(false)
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public void EnsureIdIsOnlyAppliedOnce()
    {
        var inspector = new TestTypeInterceptor();

        SchemaBuilder.New()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("abc").ID().ID().ID().ID().Resolve("abc");
            })
            .AddGlobalObjectIdentification(false)
            .TryAddTypeInterceptor(inspector)
            .Create();

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

        public string StringId([ID] string id) => id;
        public string StringIdList([ID] string[] id) =>
            string.Join(", ", id.Select(t => t.ToString()));

        public string NullableStringId([ID] string? id) => id ?? "null";
        public string NullableStringIdList([ID] string?[] id) =>
            string.Join(", ", id.Select(t => t?.ToString() ?? "null"));

        public string GuidId([ID] Guid id) => id.ToString();
        public string GuidIdList([ID] IReadOnlyList<Guid> id) =>
            string.Join(", ", id.Select(t => t.ToString()));

        public string NullableGuidId([ID] Guid? id) => id?.ToString() ?? "null";
        public string NullableGuidIdList([ID] IReadOnlyList<Guid?> id) =>
            string.Join(", ", id.Select(t => t?.ToString() ?? "null"));

        public string InterceptedId([InterceptedID] [ID] int id) => id.ToString();

        public string InterceptedIds([InterceptedID] [ID] int[] id) =>
            string.Join(", ", id.Select(t => t.ToString()));

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

        [ID("Some")] public string SomeId { get; }

        [ID("Some")] public string? SomeNullableId { get; }

        [ID("Some")] public IReadOnlyList<int> SomeIds { get; }

        [ID("Some")] public IReadOnlyList<int?>? SomeNullableIds { get; }

        [ID, InterceptedID,]
        public int? InterceptedId { get; }

        [ID, InterceptedID,]
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

        [ID("Bar")] public IReadOnlyList<int> SomeIds { get; }

        [ID("Bar")] public string? SomeNullableId { get; }

        [ID("Bar")] public IReadOnlyList<int?>? SomeNullableIds { get; }

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

    [AttributeUsage(
        AttributeTargets.Parameter |
        AttributeTargets.Property |
        AttributeTargets.Method)]
    public class InterceptedIDAttribute : DescriptorAttribute
    {
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

        private static void AddInterceptingSerializer(ArgumentDefinition definition) =>
            definition.Formatters.Insert(0, new InterceptingFormatter());

        private sealed class InterceptingFormatter : IInputValueFormatter
        {
            public object? Format(object? runtimeValue) =>
                runtimeValue is IEnumerable<string> list
                    ? list
                        .Select(x => new IdValue("x", "y", int.Parse(x)))
                        .ToArray()
                    : new IdValue("x", "y", int.Parse((string)runtimeValue!));
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
}
