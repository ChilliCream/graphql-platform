// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable UnusedMember.Global

using System.Security.Claims;
using System.Text.Json;
using HotChocolate.Adapters.Mcp.Directives;
using HotChocolate.Authorization;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types;

namespace HotChocolate.Adapters.Mcp;

public sealed class TestSchema
{
    public sealed class Query
    {
        public Book[] GetBooks() => [new("Title")];

        public ResultNullable GetWithNullableVariables(
            JsonElement? any,
            [GraphQLType<Base64StringType>] byte[]? base64String,
            bool? boolean,
            sbyte? @byte,
            [GraphQLType<DateType>] DateOnly? date,
            DateTimeOffset? dateTime,
            decimal? @decimal,
            TestEnum? @enum,
            float? @float,
            [GraphQLType<IdType>] string? id,
            int? @int,
            string?[]? list,
            DateOnly? localDate,
            [GraphQLType<LocalDateTimeType>] DateTime? localDateTime,
            TimeOnly? localTime,
            long? @long,
            Object1Nullable? @object,
            short? @short,
            string? @string,
            TimeSpan? timeSpan,
            [GraphQLType<UnknownType>] string? unknown,
            Uri? uri,
            [GraphQLType<UrlType>] Uri? url,
            Guid? uuid)
            =>
                new(
                    any,
                    base64String,
                    boolean,
                    @byte,
                    date,
                    dateTime,
                    @decimal,
                    @enum,
                    @float,
                    id,
                    @int,
                    list,
                    localDate,
                    localDateTime,
                    localTime,
                    @long,
                    @object,
                    @short,
                    @string,
                    timeSpan,
                    unknown,
                    uri,
                    url,
                    uuid);

        public ResultNonNullable GetWithNonNullableVariables(
            JsonElement any,
            [GraphQLType<NonNullType<Base64StringType>>] byte[] base64String,
            bool boolean,
            sbyte @byte,
            [GraphQLType<NonNullType<DateType>>] DateOnly date,
            DateTimeOffset dateTime,
            decimal @decimal,
            TestEnum @enum,
            float @float,
            [GraphQLType<NonNullType<IdType>>] string id,
            int @int,
            string[] list,
            DateOnly localDate,
            [GraphQLType<NonNullType<LocalDateTimeType>>] DateTime localDateTime,
            TimeOnly localTime,
            long @long,
            Object1NonNullable @object,
            short @short,
            string @string,
            TimeSpan timeSpan,
            [GraphQLType<NonNullType<UnknownType>>] string unknown,
            Uri uri,
            [GraphQLType<NonNullType<UrlType>>] Uri url,
            Guid uuid)
            =>
                new(
                    any,
                    base64String,
                    boolean,
                    @byte,
                    date,
                    dateTime,
                    @decimal,
                    @enum,
                    @float,
                    id,
                    @int,
                    list,
                    localDate,
                    localDateTime,
                    localTime,
                    @long,
                    @object,
                    @short,
                    @string,
                    timeSpan,
                    unknown,
                    uri,
                    url,
                    uuid);

        public ResultDefaulted GetWithDefaultedVariables(
            JsonElement any,
            [GraphQLType<NonNullType<Base64StringType>>] byte[] base64String,
            bool boolean,
            sbyte @byte,
            [GraphQLType<NonNullType<DateType>>] DateOnly date,
            DateTimeOffset dateTime,
            decimal @decimal,
            TestEnum @enum,
            float @float,
            [GraphQLType<NonNullType<IdType>>] string id,
            int @int,
            string[] list,
            DateOnly localDate,
            [GraphQLType<NonNullType<LocalDateTimeType>>] DateTime localDateTime,
            TimeOnly localTime,
            long @long,
            Object1Defaulted @object,
            short @short,
            string @string,
            TimeSpan timeSpan,
            [GraphQLType<NonNullType<UnknownType>>] string unknown,
            Uri uri,
            [GraphQLType<NonNullType<UrlType>>] Uri url,
            Guid uuid)
            =>
                new(
                    any,
                    base64String,
                    boolean,
                    @byte,
                    date,
                    dateTime,
                    @decimal,
                    @enum,
                    @float,
                    id,
                    @int,
                    list,
                    localDate,
                    localDateTime,
                    localTime,
                    @long,
                    @object,
                    @short,
                    @string,
                    timeSpan,
                    unknown,
                    uri,
                    url,
                    uuid);

        public ResultComplex GetWithComplexVariables(
            Object1Complex[] list,
            Object1Complex @object,
            string? nullDefault,
            string?[]? listWithNullDefault,
            Object1Complex? objectWithNullDefault,
            OneOf oneOf,
            OneOf[] oneOfList,
            ObjectWithOneOfField objectWithOneOfField,
            TimeSpan timeSpanDotNet)
            =>
                new(
                    list,
                    @object,
                    nullDefault,
                    listWithNullDefault,
                    objectWithNullDefault,
                    oneOf,
                    oneOfList,
                    objectWithOneOfField,
                    timeSpanDotNet);

        public int GetWithVariableMinMaxValues() => 1;

        public IPet GetWithInterfaceType() => new Cat(Name: "Whiskers", IsPurring: true);

        [GraphQLType("PetUnion!")]
        public IPet GetWithUnionType() => new Cat(Name: "Whiskers", IsPurring: true);

        // Implicitly open-world by default, unless annotated otherwise.
        public int ImplicitOpenWorldQuery() => 1;

        [McpToolAnnotations(OpenWorldHint = true)]
        public int ExplicitOpenWorldQuery() => 1;

        [McpToolAnnotations(OpenWorldHint = false)]
        public int ExplicitClosedWorldQuery() => 1;

        // The query field is closed-world, but the subfield is (explicitly) open-world.
        [McpToolAnnotations(OpenWorldHint = false)]
        public ExplicitOpenWorld ExplicitOpenWorldSubfieldQuery() => new();

        // The query field is closed-world, and the subfield is also (explicitly) closed-world.
        [McpToolAnnotations(OpenWorldHint = false)]
        public ExplicitClosedWorld ExplicitClosedWorldSubfieldQuery() => new();

        public int GetWithErrors()
        {
            throw new GraphQLException(
                ErrorBuilder
                    .New()
                    .SetMessage("Error 1")
                    .SetCode("Code 1")
                    .SetException(new Exception("Exception 1"))
                    .Build());
        }

        [Authorize(Roles = ["Admin"])]
        public string? GetWithAuth(ClaimsPrincipal principal) => principal.Identity?.Name;
    }

    public sealed class Mutation
    {
        public Book AddBook() => new("Title");

        // Destructive by default, unless annotated otherwise.
        public int ImplicitDestructiveMutation() => 1;

        [McpToolAnnotations(DestructiveHint = true)]
        public int ExplicitDestructiveMutation() => 1;

        [McpToolAnnotations(DestructiveHint = false)]
        public int ExplicitNonDestructiveMutation() => 1;

        // Non-idempotent by default, unless annotated otherwise.
        public int ImplicitNonIdempotentMutation() => 1;

        [McpToolAnnotations(IdempotentHint = false)]
        public int ExplicitNonIdempotentMutation() => 1;

        [McpToolAnnotations(IdempotentHint = true)]
        public int ExplicitIdempotentMutation() => 1;

        [McpToolAnnotations(OpenWorldHint = true)]
        public int ExplicitOpenWorldMutation() => 1;

        [McpToolAnnotations(OpenWorldHint = false)]
        public int ExplicitClosedWorldMutation() => 1;
    }

    public sealed class Subscription
    {
        public Book BookAdded() => new("Title");
    }

    public sealed record Book(string Title);

    public enum TestEnum
    {
        Value1,
        Value2
    }

    public sealed record Object1Nullable(
        [property: GraphQLDescription("field1A description")] Object2Nullable? Field1A);
    public sealed record Object2Nullable(
        [property: GraphQLDescription("field1B description")] Object3Nullable? Field1B);
    public sealed record Object3Nullable(
        [property: GraphQLDescription("field1C description")] TimeOnly? Field1C);

    public sealed record Object1NonNullable(
        [property: GraphQLDescription("field1A description")] Object2NonNullable Field1A);
    public sealed record Object2NonNullable(
        [property: GraphQLDescription("field1B description")] Object3NonNullable Field1B);
    public sealed record Object3NonNullable(
        [property: GraphQLDescription("field1C description")] TimeOnly Field1C);

    public sealed record Object1Defaulted(
        [property: GraphQLDescription("field1A description")]
        [property: DefaultValueSyntax("""{ field1B: { field1C: "12:00:00" } }""")]
        Object2Defaulted Field1A);
    public sealed record Object2Defaulted(
        [property: GraphQLDescription("field1B description")]
        [property: DefaultValueSyntax("""{ field1C: "12:00:00" }""")]
        Object3Defaulted Field1B);
    public sealed record Object3Defaulted(
        [property: GraphQLDescription("field1C description")]
        [property: DefaultValueSyntax("\"12:00:00\"")]
        TimeOnly Field1C);

    public sealed record Object1Complex(
        [property: GraphQLDescription("field1A description")]
        [property: DefaultValueSyntax("""{ field1B: [{ field1C: "12:00:00" }] }""")]
        Object2Complex Field1A);
    public sealed record Object2Complex(
        [property: GraphQLDescription("field1B description")]
        [property: DefaultValueSyntax("""[{ field1C: "12:00:00" }]""")]
        Object3Complex[] Field1B);
    public sealed record Object3Complex(
        [property: GraphQLDescription("field1C description")]
        [property: DefaultValueSyntax("\"12:00:00\"")]
        TimeOnly? Field1C);

    [OneOf]
    public sealed record OneOf(
        [property: GraphQLDescription("field1 description")] int? Field1,
        [property: GraphQLDescription("field2 description")] string? Field2);

    public sealed record ObjectWithOneOfField(
        [property: GraphQLDescription("field description")]
        [property: DefaultValueSyntax("{ field1: 1 }")]
        OneOf Field);

    public sealed record ResultNullable(
        JsonElement? Any,
        [property: GraphQLType<Base64StringType>] byte[]? Base64String,
        bool? Boolean,
        sbyte? Byte,
        [property: GraphQLType<DateType>] DateOnly? Date,
        DateTimeOffset? DateTime,
        decimal? Decimal,
        TestEnum? Enum,
        float? Float,
        [property: GraphQLType<IdType>] string? Id,
        int? Int,
        string?[]? List,
        DateOnly? LocalDate,
        [property: GraphQLType<LocalDateTimeType>] DateTime? LocalDateTime,
        TimeOnly? LocalTime,
        long? Long,
        Object1Nullable? Object,
        short? Short,
        string? String,
        TimeSpan? TimeSpan,
        [property: GraphQLType<UnknownType>] string? Unknown,
        Uri? Uri,
        [property: GraphQLType<UrlType>] Uri? Url,
        Guid? Uuid);

    public sealed record ResultNonNullable(
        JsonElement Any,
        [property: GraphQLType<NonNullType<Base64StringType>>] byte[] Base64String,
        bool Boolean,
        sbyte Byte,
        [property: GraphQLType<NonNullType<DateType>>] DateOnly Date,
        DateTimeOffset DateTime,
        decimal Decimal,
        TestEnum Enum,
        float Float,
        [property: GraphQLType<NonNullType<IdType>>] string Id,
        int Int,
        string[] List,
        DateOnly LocalDate,
        [property: GraphQLType<NonNullType<LocalDateTimeType>>] DateTime LocalDateTime,
        TimeOnly LocalTime,
        long Long,
        Object1NonNullable Object,
        short Short,
        string String,
        TimeSpan TimeSpan,
        [property: GraphQLType<NonNullType<UnknownType>>] string Unknown,
        Uri Uri,
        [property: GraphQLType<NonNullType<UrlType>>] Uri Url,
        Guid Uuid);

    public sealed record ResultDefaulted(
        JsonElement Any,
        [property: GraphQLType<NonNullType<Base64StringType>>] byte[] Base64String,
        bool Boolean,
        sbyte Byte,
        [property: GraphQLType<NonNullType<DateType>>] DateOnly Date,
        DateTimeOffset DateTime,
        decimal Decimal,
        TestEnum Enum,
        float Float,
        [property: GraphQLType<NonNullType<IdType>>] string Id,
        int Int,
        string[] List,
        DateOnly LocalDate,
        [property: GraphQLType<NonNullType<LocalDateTimeType>>] DateTime LocalDateTime,
        TimeOnly LocalTime,
        long Long,
        Object1Defaulted Object,
        short Short,
        string String,
        TimeSpan TimeSpan,
        [property: GraphQLType<NonNullType<UnknownType>>] string Unknown,
        Uri Uri,
        [property: GraphQLType<NonNullType<UrlType>>] Uri Url,
        Guid Uuid);

    public sealed record ResultComplex(
        Object1Complex[] List,
        Object1Complex Object,
        string? NullDefault,
        string?[]? ListWithNullDefault,
        Object1Complex? ObjectWithNullDefault,
        OneOf OneOf,
        OneOf[] OneOfList,
        ObjectWithOneOfField ObjectWithOneOfField,
        TimeSpan TimeSpanDotNet);

    [UnionType(name: "PetUnion")]
    public interface IPet
    {
        string Name { get; }
    }

    public sealed record Cat(string Name, bool IsPurring) : IPet;
    public sealed record Dog(string Name, bool IsBarking) : IPet;

    private sealed class UnknownType() : ScalarType<string, StringValueNode>("Unknown")
    {
        protected override string OnCoerceInputLiteral(StringValueNode valueLiteral)
            => valueLiteral.Value;

        protected override string OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
            => inputValue.GetString()!;

        protected override void OnCoerceOutputValue(string runtimeValue, ResultElement resultValue)
            => resultValue.SetStringValue(runtimeValue);

        protected override StringValueNode OnValueToLiteral(string runtimeValue)
            => new StringValueNode(runtimeValue);
    }

    public sealed class ExplicitOpenWorld
    {
        [McpToolAnnotations(OpenWorldHint = true)]
        public int ExplicitOpenWorldField() => 1;
    }

    public sealed class ExplicitClosedWorld
    {
        [McpToolAnnotations(OpenWorldHint = false)]
        public int ExplicitClosedWorldField() => 1;
    }
}
