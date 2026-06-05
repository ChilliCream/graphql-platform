using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Composite;

public static class SourceSchemaKeyInferenceTests
{
    [Fact]
    public static async Task InferKey_Should_AddKeyToObject_When_LookupReturnsObject()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ObjectQuery>()
                .ModifyOptions(o => o.InferKeysFromLookups = true)
                .BuildSchemaAsync();

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: ObjectQuery
            }

            type ObjectQuery {
              productById(id: Int!): Product @lookup
            }

            type Product @key(fields: "id") {
              id: Int!
              name: String!
            }

            scalar FieldSelectionSet

            """
            The @key directive is used to designate an entity's unique key,
            which identifies how to uniquely reference an instance of
            an entity across different source schemas.
            """
            directive @key("The field selection set syntax." fields: FieldSelectionSet!) repeatable on
              | OBJECT
              | INTERFACE

            """
            The @lookup directive is used within a source schema to specify output fields
            that can be used by the distributed GraphQL executor to resolve an entity by
            a stable key.
            """
            directive @lookup on FIELD_DEFINITION
            """");
    }

    [Fact]
    public static async Task InferKey_Should_AddKeyToInterfaceAndImplementers_When_LookupReturnsInterface()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<InterfaceQuery>()
                .AddType<Article>()
                .AddType<Video>()
                .ModifyOptions(o => o.InferKeysFromLookups = true)
                .BuildSchemaAsync();

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: InterfaceQuery
            }

            type InterfaceQuery {
              contentById(id: Int!): Content @lookup
            }

            type Article implements Content @key(fields: "id") {
              id: Int!
              body: String!
            }

            type Video implements Content @key(fields: "id") {
              id: Int!
              duration: Int!
            }

            interface Content @key(fields: "id") {
              id: Int!
            }

            scalar FieldSelectionSet

            """
            The @key directive is used to designate an entity's unique key,
            which identifies how to uniquely reference an instance of
            an entity across different source schemas.
            """
            directive @key("The field selection set syntax." fields: FieldSelectionSet!) repeatable on
              | OBJECT
              | INTERFACE

            """
            The @lookup directive is used within a source schema to specify output fields
            that can be used by the distributed GraphQL executor to resolve an entity by
            a stable key.
            """
            directive @lookup on FIELD_DEFINITION
            """");
    }

    [Fact]
    public static async Task InferKey_Should_AddKeyToMembersNotUnion_When_LookupReturnsUnion()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<UnionQuery>()
                .AddType<Cat>()
                .AddType<Dog>()
                .ModifyOptions(o => o.InferKeysFromLookups = true)
                .BuildSchemaAsync();

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: UnionQuery
            }

            type UnionQuery {
              petById(id: Int!): Pet @lookup
            }

            type Cat @key(fields: "id") {
              id: Int!
              meows: Boolean!
            }

            type Dog @key(fields: "id") {
              id: Int!
              barks: Boolean!
            }

            union Pet = Cat | Dog

            scalar FieldSelectionSet

            """
            The @key directive is used to designate an entity's unique key,
            which identifies how to uniquely reference an instance of
            an entity across different source schemas.
            """
            directive @key("The field selection set syntax." fields: FieldSelectionSet!) repeatable on
              | OBJECT
              | INTERFACE

            """
            The @lookup directive is used within a source schema to specify output fields
            that can be used by the distributed GraphQL executor to resolve an entity by
            a stable key.
            """
            directive @lookup on FIELD_DEFINITION
            """");
    }

    [Fact]
    public static async Task InferKey_Should_NotDuplicate_When_DeveloperDeclaredKey()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<DeclaredKeyQuery>()
                .ModifyOptions(o => o.InferKeysFromLookups = true)
                .BuildSchemaAsync();

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: DeclaredKeyQuery
            }

            type DeclaredKeyQuery {
              userById(id: Int!): User @lookup
            }

            type User @key(fields: "id") {
              id: Int!
              name: String!
            }

            scalar FieldSelectionSet

            """
            The @key directive is used to designate an entity's unique key,
            which identifies how to uniquely reference an instance of
            an entity across different source schemas.
            """
            directive @key("The field selection set syntax." fields: FieldSelectionSet!) repeatable on
              | OBJECT
              | INTERFACE

            """
            The @lookup directive is used within a source schema to specify output fields
            that can be used by the distributed GraphQL executor to resolve an entity by
            a stable key.
            """
            directive @lookup on FIELD_DEFINITION
            """");
    }

    [Fact]
    public static async Task InferKey_Should_AddTwoKeys_When_ChoiceIsArgument()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ChoiceQuery>()
                .ModifyOptions(o => o.InferKeysFromLookups = true)
                .BuildSchemaAsync();

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: ChoiceQuery
            }

            type ChoiceQuery {
              accountBy(by: AccountByInput! @is(field: "{\n  id\n} | {\n  email\n}")): Account
                @lookup
            }

            type Account @key(fields: "id") @key(fields: "email") {
              id: Int!
              email: String!
            }

            input AccountByInput @oneOf {
              id: Int
              email: String
            }

            scalar FieldSelectionMap

            scalar FieldSelectionSet

            """
            The @is directive is utilized on lookup fields to describe how the arguments
            can be mapped from the entity type that the lookup field resolves.
            """
            directive @is("The field selection map syntax." field: FieldSelectionMap!) on
              | ARGUMENT_DEFINITION

            """
            The @key directive is used to designate an entity's unique key,
            which identifies how to uniquely reference an instance of
            an entity across different source schemas.
            """
            directive @key("The field selection set syntax." fields: FieldSelectionSet!) repeatable on
              | OBJECT
              | INTERFACE

            """
            The @lookup directive is used within a source schema to specify output fields
            that can be used by the distributed GraphQL executor to resolve an entity by
            a stable key.
            """
            directive @lookup on FIELD_DEFINITION
            """");
    }

    [Fact]
    public static async Task InferKey_Should_AddThreeDistinctKeys_When_ChoiceIsHasNestedPath()
    {
        // arrange & act
        // composite-schemas spec example #4214e
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<NestedChoiceQuery>()
                .ModifyOptions(o => o.InferKeysFromLookups = true)
                .BuildSchemaAsync();

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: NestedChoiceQuery
            }

            type NestedChoiceQuery {
              person(
                by: PersonByInput! @is(field: "{\n  id\n} | {\n  addressId: address.id\n} | {\n  name\n}")
              ): Person @lookup
            }

            type Address {
              id: Int!
            }

            type Person
              @key(fields: "id")
              @key(fields: "address { id }")
              @key(fields: "name") {
              id: Int!
              name: String!
              address: Address!
            }

            input PersonByInput @oneOf {
              id: Int
              addressId: Int
              name: String
            }

            scalar FieldSelectionMap

            scalar FieldSelectionSet

            """
            The @is directive is utilized on lookup fields to describe how the arguments
            can be mapped from the entity type that the lookup field resolves.
            """
            directive @is("The field selection map syntax." field: FieldSelectionMap!) on
              | ARGUMENT_DEFINITION

            """
            The @key directive is used to designate an entity's unique key,
            which identifies how to uniquely reference an instance of
            an entity across different source schemas.
            """
            directive @key("The field selection set syntax." fields: FieldSelectionSet!) repeatable on
              | OBJECT
              | INTERFACE

            """
            The @lookup directive is used within a source schema to specify output fields
            that can be used by the distributed GraphQL executor to resolve an entity by
            a stable key.
            """
            directive @lookup on FIELD_DEFINITION
            """");
    }

    [Fact]
    public static async Task InferKey_Should_AddNestedKey_When_IsPathIsNested()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<NestedPathQuery>()
                .ModifyOptions(o => o.InferKeysFromLookups = true)
                .BuildSchemaAsync();

        // assert
        var person = schema.Types.GetType<IObjectTypeDefinition>("Person");
        var keys = person.Directives.Where(d => d.Name == "key").ToArray();
        Assert.Equal(@"@key(fields: ""address { id }"")", Assert.Single(keys).ToString());
    }

    [Fact]
    public static async Task InferKey_Should_MergeArgumentsInDeclarationOrder_When_LookupHasMultipleArguments()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<MultiArgQuery>()
                .ModifyOptions(o => o.InferKeysFromLookups = true)
                .BuildSchemaAsync();

        // assert
        var product = schema.Types.GetType<IObjectTypeDefinition>("Product");
        var keys = product.Directives.Where(d => d.Name == "key").ToArray();
        Assert.Equal(@"@key(fields: ""id categoryId"")", Assert.Single(keys).ToString());
    }

    [Fact]
    public static async Task InferKey_Should_NotDuplicate_When_DeveloperDeclaredKeyInReverseOrder()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ReverseKeyQuery>()
                .ModifyOptions(o => o.InferKeysFromLookups = true)
                .BuildSchemaAsync();

        // assert
        var part = schema.Types.GetType<IObjectTypeDefinition>("Part");
        var keys = part.Directives.Where(d => d.Name == "key").ToArray();
        Assert.Equal(@"@key(fields: ""sku id"")", Assert.Single(keys).ToString());
    }

    [Fact]
    public static async Task InferKey_Should_AddTwoDistinctKeys_When_MultipleLookupsReturnSameType()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<TwoLookupsQuery>()
                .ModifyOptions(o => o.InferKeysFromLookups = true)
                .BuildSchemaAsync();

        // assert
        var book = schema.Types.GetType<IObjectTypeDefinition>("Book");
        var keys = book.Directives
            .Where(d => d.Name == "key")
            .Select(d => d.ToString())
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
        string[] expected = [@"@key(fields: ""id"")", @"@key(fields: ""isbn"")"];
        Assert.Equal(expected, keys);
    }

    [Fact]
    public static async Task InferKey_Should_AddKey_When_SourceSchemaDefaultsEnabled()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ObjectQuery>()
                .AddSourceSchemaDefaults()
                .BuildSchemaAsync();

        // assert
        var product = schema.Types.GetType<IObjectTypeDefinition>("Product");
        var keys = product.Directives.Where(d => d.Name == "key").ToArray();
        Assert.Equal(@"@key(fields: ""id"")", Assert.Single(keys).ToString());
    }

    [Fact]
    public static async Task InferKey_Should_NotInferKey_When_DefaultOptionsAndNoSourceSchemaDefaults()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ObjectQuery>()
                .BuildSchemaAsync();

        // assert
        var product = schema.Types.GetType<IObjectTypeDefinition>("Product");
        Assert.DoesNotContain(product.Directives, d => d.Name == "key");
    }

    [Fact]
    public static async Task InferKey_Should_AddCompositeKey_When_LookupHasMultipleArguments_WithNullableArg()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<NullableMultiArgQuery>()
                .ModifyOptions(o => o.InferKeysFromLookups = true)
                .BuildSchemaAsync();

        // assert
        var product = schema.Types.GetType<IObjectTypeDefinition>("Product");
        var keys = product.Directives.Where(d => d.Name == "key").ToArray();
        Assert.Equal(@"@key(fields: ""a b"")", Assert.Single(keys).ToString());
    }

    [Fact]
    public static async Task InferKey_Should_MergeNameAndIsMappedArguments_When_LookupHasMixedArguments()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<MixedIsQuery>()
                .ModifyOptions(o => o.InferKeysFromLookups = true)
                .BuildSchemaAsync();

        // assert
        var product = schema.Types.GetType<IObjectTypeDefinition>("Product");
        var keys = product.Directives.Where(d => d.Name == "key").ToArray();
        Assert.Equal(@"@key(fields: ""a c"")", Assert.Single(keys).ToString());
    }

    [Fact]
    public static async Task InferKey_Should_AddCartesianKeys_When_PlainArgCombinedWithChoiceIs()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<CartesianQuery>()
                .ModifyOptions(o => o.InferKeysFromLookups = true)
                .BuildSchemaAsync();

        // assert
        var account = schema.Types.GetType<IObjectTypeDefinition>("Account");
        var keys = account.Directives
            .Where(d => d.Name == "key")
            .Select(d => d.ToString())
            .ToArray();
        string[] expected = [@"@key(fields: ""tenant id"")", @"@key(fields: ""tenant email"")"];
        Assert.Equal(expected, keys);
    }

    [Fact]
    public static async Task InferKey_Should_NotAddKey_When_OptedOut()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ObjectQuery>()
                .ModifyOptions(o => o.InferKeysFromLookups = false)
                .BuildSchemaAsync();

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: ObjectQuery
            }

            type ObjectQuery {
              productById(id: Int!): Product @lookup
            }

            type Product {
              id: Int!
              name: String!
            }

            """
            The @lookup directive is used within a source schema to specify output fields
            that can be used by the distributed GraphQL executor to resolve an entity by
            a stable key.
            """
            directive @lookup on FIELD_DEFINITION
            """");
    }

    public class ObjectQuery
    {
        [Lookup]
        public Product? GetProductById(int id)
            => new(id, "Abc");
    }

    public record Product(int Id, string Name);

    public class InterfaceQuery
    {
        [Lookup]
        public Content? GetContentById(int id)
            => null;
    }

    [InterfaceType]
    public interface Content
    {
        int Id { get; }
    }

    public record Article(int Id, string Body) : Content;

    public record Video(int Id, int Duration) : Content;

    public class UnionQuery
    {
        [Lookup]
        public IPet? GetPetById(int id)
            => null;
    }

    [UnionType("Pet")]
    public interface IPet;

    public record Cat(int Id, bool Meows) : IPet;

    public record Dog(int Id, bool Barks) : IPet;

    public class DeclaredKeyQuery
    {
        [Lookup]
        public User? GetUserById(int id)
            => new(id, "Abc");
    }

    [EntityKey("id")]
    public record User(int Id, string Name);

    public class ChoiceQuery
    {
        [Lookup]
        public Account? GetAccountBy([Is("{ id } | { email }")] AccountByInput by)
            => new(by.Id ?? 1, by.Email ?? "abc");
    }

    [OneOf]
    public record AccountByInput(int? Id, string? Email);

    public record Account(int Id, string Email);

    public class NestedChoiceQuery
    {
        [Lookup]
        public Person? GetPerson(
            [Is("{ id } | { addressId: address.id } | { name }")] PersonByInput by)
            => null;
    }

    [OneOf]
    public record PersonByInput(int? Id, int? AddressId, string? Name);

    public record Person(int Id, string Name, Address Address);

    public record Address(int Id);

    public class NestedPathQuery
    {
        [Lookup]
        public Person? GetPersonByAddressId([Is("address.id")] int addressId)
            => null;
    }

    public class MultiArgQuery
    {
        [Lookup]
        public Product? GetProductByIdAndCategoryId(int id, int categoryId)
            => new(id, "Abc");
    }

    public class ReverseKeyQuery
    {
        [Lookup]
        public Part? GetPartBySkuAndId(string sku, int id)
            => null;
    }

    [EntityKey("sku id")]
    public record Part(int Id, string Sku);

    public class TwoLookupsQuery
    {
        [Lookup]
        public Book? GetBookById(int id)
            => null;

        [Lookup]
        public Book? GetBookByIsbn(string isbn)
            => null;
    }

    public record Book(int Id, string Isbn);

    public class NullableMultiArgQuery
    {
        [Lookup]
        public CompositeProduct? GetProductByName(string a, string? b)
            => null;
    }

    public class MixedIsQuery
    {
        [Lookup]
        public CompositeProduct? GetProductBy(string a, [Is("c")] string b)
            => null;
    }

    [GraphQLName("Product")]
    public record CompositeProduct(string A, string B, string C);

    public class CartesianQuery
    {
        [Lookup]
        public TenantAccount? GetAccountBy(
            string tenant,
            [Is("{ id } | { email }")] AccountByInput by)
            => null;
    }

    [GraphQLName("Account")]
    public record TenantAccount(int Id, string Email, string Tenant);
}
