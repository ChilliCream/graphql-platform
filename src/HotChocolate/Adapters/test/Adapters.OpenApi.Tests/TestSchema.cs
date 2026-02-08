using System.Text.Json;
using HotChocolate.Authorization;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.Adapters.OpenApi;

public sealed class TestSchema
{
    public sealed class Query
    {
        public User? GetUserById([GraphQLDescription("The id of the user")][ID] int id, IResolverContext context)
        {
            if (id == 5)
            {
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("Something went wrong")
                        .SetPath(context.Path)
                        .Build());
            }

            if (id is < 1 or > 3)
            {
                return null;
            }

            return new User(id);
        }

        public User? GetUserByName(string name)
            => new(1) { Name = name };

        [Authorize(Roles = [OpenApiTestBase.AdminRole])]
        public IEnumerable<User?> GetUsers()
            => [new User(1), new User(2), new User(3)];

        public IEnumerable<User> GetUsersWithoutAuth()
            => [new User(1), new User(2), new User(3)];

        public IPet GetWithInterfaceType() => new Cat(Name: "Whiskers", IsPurring: true);

        [GraphQLType("PetUnion!")]
        public IPet GetWithUnionType() => new Cat(Name: "Whiskers", IsPurring: true);

        [GraphQLType("[PetUnion!]!")]
        public List<IPet> GetWithUnionTypeList() => [new Cat(Name: "Whiskers", IsPurring: true), new Dog(Name: "Buddy", IsBarking: true)];

        public List<string?> GetListWithNullableItems() => ["item1", null, "item3"];

        public List<string> GetListWithNonNullItems() => ["item1", "item2", "item3"];

        public List<string> GetList(List<string> input) => input;

        public JsonElement GetJson(JsonElement input) => input;

        public ComplexObject GetComplexObject(ComplexObjectInput input)
        {
            return new ComplexObject(
                input.Any,
                input.Boolean,
                input.Byte,
                input.ByteArray,
                input.Date,
                input.DateTime,
                input.Decimal,
                input.Enum,
                input.Float,
                input.Id,
                input.Int,
                input.Json,
                input.List,
                input.LocalDate,
                input.LocalDateTime,
                input.LocalTime,
                input.Long,
                new Object1Nullable(new Object2Nullable(new Object3Nullable(input.Object.Field1A.Field1B.Field1C))),
                input.Short,
                input.String,
                input.TimeSpan,
                input.Unknown,
                input.Url,
                input.Uuid);
        }
    }

    public class Mutation
    {
        public User CreateUser(UserInput user)
        {
            return new User(user.Id);
        }

        public User UpdateUser(UserInput user)
        {
            return CreateUser(user);
        }

        public DeeplyNested UpdateDeeplyNestedObject(DeeplyNested input) => input;
    }

    public class DeeplyNested
    {
        public required string UserId { get; set; }

        public required string Field { get; set; }

        public required DeeplyNested2 Object { get; set; }
    }

    public class DeeplyNested2
    {
        public required string OtherField { get; set; }

        [DefaultValue("DefaultValue")]
        public required string Field2 { get; set; }
    }

    public class UserInput
    {
        [ID]
        public int Id { get; init; }

        public required string Name { get; init; }

        [GraphQLDescription("The user's email")]
        public required string Email { get; init; }
    }

    public sealed class User(int id)
    {
        [ID]
        public int Id { get; init; } = id;

        [GraphQLDescription("The name of the user")]
        public string Name { get; set; } = "User " + id;

        [GraphQLDeprecated("Deprecated for some reason")]
        public string? Email { get; set; } = id + "@example.com";

        public Address Address { get; set; } = new Address(id + " Street");

        public Preferences? Preferences { get; init; }
    }

    public sealed record Address(string Street);

    public sealed record Preferences(string Color);

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

    [OneOf]
    public sealed record OneOf(
        [property: GraphQLDescription("field1 description")] int? Field1,
        [property: GraphQLDescription("field2 description")] string? Field2);

    public sealed record Object1NonNullable(
        [property: GraphQLDescription("field1A description")] Object2NonNullable Field1A);
    public sealed record Object2NonNullable(
        [property: GraphQLDescription("field1B description")] Object3NonNullable Field1B);
    public sealed record Object3NonNullable(
        [property: GraphQLDescription("field1C description")] TimeOnly Field1C);

    public sealed record ComplexObject(
        [property: GraphQLType<AnyType>] object? Any,
        bool? Boolean,
        byte? Byte,
        [property: GraphQLType<ByteArrayType>] byte[]? ByteArray,
        [property: GraphQLType<DateType>] DateOnly? Date,
        DateTimeOffset? DateTime,
        decimal? Decimal,
        TestEnum? Enum,
        float? Float,
        [property: GraphQLType<IdType>] string? Id,
        int? Int,
        JsonElement? Json,
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
        Uri? Url,
        Guid? Uuid);

    public sealed record ComplexObjectInput(
        [property: GraphQLType<NonNullType<AnyType>>] object Any,
        bool Boolean,
        byte Byte,
        [property: GraphQLType<NonNullType<ByteArrayType>>] byte[] ByteArray,
        [property: GraphQLType<NonNullType<DateType>>] DateOnly Date,
        DateTimeOffset DateTime,
        decimal Decimal,
        TestEnum Enum,
        float Float,
        [property: GraphQLType<NonNullType<IdType>>] string Id,
        int Int,
        JsonElement Json,
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
        Uri Url,
        Guid Uuid);

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
}
