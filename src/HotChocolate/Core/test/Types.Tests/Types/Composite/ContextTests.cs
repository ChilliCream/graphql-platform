using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Composite;

public static class ContextTests
{
    [Fact]
    public static async Task Context_Should_PrintDirective_When_AttributeIsOnClass()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query1>();

        // act
        var schema = await builder.BuildSchemaAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query1
            }

            type Query1 {
              user: User1!
            }

            type User1 @context(name: "userContext") {
              id: Int!
            }

            """
            The @context directive declares that instances of the annotated type provide the named
            context, which other source schemas can reference with $name in a @require field
            selection map.
            """
            directive @context("The name of the context." name: String!) repeatable on
              | OBJECT
              | INTERFACE
              | UNION
            """");
    }

    [Fact]
    public static async Task Context_Should_PrintTwoDirectives_When_TwoAttributesAreApplied()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query2>();

        // act
        var schema = await builder.BuildSchemaAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query2
            }

            type Query2 {
              user: User2!
            }

            type User2 @context(name: "userContext") @context(name: "tenantContext") {
              id: Int!
            }

            """
            The @context directive declares that instances of the annotated type provide the named
            context, which other source schemas can reference with $name in a @require field
            selection map.
            """
            directive @context("The name of the context." name: String!) repeatable on
              | OBJECT
              | INTERFACE
              | UNION
            """");
    }

    [Fact]
    public static async Task Context_Should_PrintDirective_When_AttributeIsOnInterface()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query3>()
            .AddType<IAccount3>()
            .AddObjectType<Account3>();

        // act
        var schema = await builder.BuildSchemaAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query3
            }

            type Query3 {
              account: IAccount3!
            }

            type Account3 implements IAccount3 {
              id: Int!
            }

            interface IAccount3 @context(name: "accountContext") {
              id: Int!
            }

            """
            The @context directive declares that instances of the annotated type provide the named
            context, which other source schemas can reference with $name in a @require field
            selection map.
            """
            directive @context("The name of the context." name: String!) repeatable on
              | OBJECT
              | INTERFACE
              | UNION
            """");
    }

    [Fact]
    public static async Task Context_Should_PrintDirective_When_ObjectDescriptorExtensionIsUsed()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query4>()
            .AddObjectType<User4>(d => d.Context("userContext"));

        // act
        var schema = await builder.BuildSchemaAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query4
            }

            type Query4 {
              user: User4!
            }

            type User4 @context(name: "userContext") {
              id: Int!
            }

            """
            The @context directive declares that instances of the annotated type provide the named
            context, which other source schemas can reference with $name in a @require field
            selection map.
            """
            directive @context("The name of the context." name: String!) repeatable on
              | OBJECT
              | INTERFACE
              | UNION
            """");
    }

    [Fact]
    public static async Task Context_Should_PrintDirective_When_UntypedObjectDescriptorExtensionIsUsed()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("user").Type("User").Resolve(_ => new object());
            })
            .AddObjectType(d =>
            {
                d.Name("User");
                d.Context("userContext");
                d.Field("id").Type<NonNullType<IdType>>().Resolve("1");
            });

        // act
        var schema = await builder.BuildSchemaAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query
            }

            type Query {
              user: User
            }

            type User @context(name: "userContext") {
              id: ID!
            }

            """
            The @context directive declares that instances of the annotated type provide the named
            context, which other source schemas can reference with $name in a @require field
            selection map.
            """
            directive @context("The name of the context." name: String!) repeatable on
              | OBJECT
              | INTERFACE
              | UNION
            """");
    }

    [Fact]
    public static async Task Context_Should_PrintDirective_When_InterfaceDescriptorExtensionIsUsed()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query5>()
            .AddInterfaceType<IAccount5>(d => d.Context("accountContext"))
            .AddObjectType<Account5>();

        // act
        var schema = await builder.BuildSchemaAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query5
            }

            type Query5 {
              account: IAccount5!
            }

            type Account5 implements IAccount5 {
              id: Int!
            }

            interface IAccount5 @context(name: "accountContext") {
              id: Int!
            }

            """
            The @context directive declares that instances of the annotated type provide the named
            context, which other source schemas can reference with $name in a @require field
            selection map.
            """
            directive @context("The name of the context." name: String!) repeatable on
              | OBJECT
              | INTERFACE
              | UNION
            """");
    }

    [Fact]
    public static async Task Context_Should_PrintDirective_When_UnionDescriptorExtensionIsUsed()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query6>()
            .AddUnionType<IAccount6>(d =>
            {
                d.Context("accountContext");
                d.Type<UserType6>();
                d.Type<OrganizationType6>();
            });

        // act
        var schema = await builder.BuildSchemaAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query6
            }

            type Query6 {
              account: IAccount6!
            }

            type Organization6 {
              id: Int!
            }

            type User6 {
              id: Int!
            }

            union IAccount6 @context(name: "accountContext") = User6 | Organization6

            """
            The @context directive declares that instances of the annotated type provide the named
            context, which other source schemas can reference with $name in a @require field
            selection map.
            """
            directive @context("The name of the context." name: String!) repeatable on
              | OBJECT
              | INTERFACE
              | UNION
            """");
    }

    [Fact]
    public static async Task Context_Should_PrintDirective_When_UnionAttributeIsUsed()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query7>()
            .AddUnionType<IAccount7>()
            .AddObjectType<User7>();

        // act
        var schema = await builder.BuildSchemaAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query7
            }

            type Query7 {
              account: IAccount7!
            }

            type User7 {
              id: Int!
            }

            union IAccount7 @context(name: "accountContext") = User7

            """
            The @context directive declares that instances of the annotated type provide the named
            context, which other source schemas can reference with $name in a @require field
            selection map.
            """
            directive @context("The name of the context." name: String!) repeatable on
              | OBJECT
              | INTERFACE
              | UNION
            """");
    }

    [Fact]
    public static async Task Require_Should_PrintContextPath_When_ArgumentRequiresContext()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query8>();

        // act
        var schema = await builder.BuildSchemaAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query8
            }

            type Query8 {
              book: Book8!
            }

            type Book8 {
              price(isoCode: String! @require(field: "$userContext.userCurrency.isoCode")): String!
              id: Int!
            }

            scalar FieldSelectionMap

            """
            The @require directive is used to express data requirements with other source schemas.
            Arguments annotated with the @require directive are removed from the composite schema
            and the value for these will be resolved by the distributed executor.


            directive @require(field: FieldSelectionMap!) on ARGUMENT_DEFINITION
            """
            directive @require("The field selection map syntax." field: FieldSelectionMap!) on
              | ARGUMENT_DEFINITION
            """");
    }

    [Fact]
    public static void Context_Should_Throw_When_NameIsNullOrEmpty()
    {
        // arrange, act, assert
        Assert.Throws<ArgumentNullException>(() => new ContextAttribute(null!));
        Assert.Throws<ArgumentException>(() => new ContextAttribute(""));
        Assert.Throws<ArgumentNullException>(() => new Context(null!));
        Assert.Throws<ArgumentException>(() => new Context(""));
    }

    [Fact]
    public static async Task Context_Should_Throw_When_DescriptorExtensionNameIsNullOrEmpty()
    {
        // arrange
        IObjectTypeDescriptor<User4>? objectDescriptor = null;
        IInterfaceTypeDescriptor<IAccount5>? interfaceDescriptor = null;
        IUnionTypeDescriptor? unionDescriptor = null;

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query4>()
            .AddObjectType<User4>(d => objectDescriptor = d)
            .AddInterfaceType<IAccount5>(d => interfaceDescriptor = d)
            .AddUnionType<IAccount6>(d =>
            {
                unionDescriptor = d;
                d.Type<UserType6>();
                d.Type<OrganizationType6>();
            })
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act, assert
        Assert.Throws<ArgumentException>(() => objectDescriptor!.Context(""));
        Assert.Throws<ArgumentNullException>(() => interfaceDescriptor!.Context(null!));
        Assert.Throws<ArgumentException>(() => unionDescriptor!.Context(""));
    }

    public class Query1
    {
        public User1 User => new(1);
    }

    [Context("userContext")]
    public record User1(int Id);

    public class Query2
    {
        public User2 User => new(1);
    }

    [Context("userContext")]
    [Context("tenantContext")]
    public record User2(int Id);

    public class Query3
    {
        public IAccount3 Account => new Account3(1);
    }

    [InterfaceType]
    [Context("accountContext")]
    public interface IAccount3
    {
        int Id { get; }
    }

    public record Account3(int Id) : IAccount3;

    public class Query4
    {
        public User4 User => new(1);
    }

    public record User4(int Id);

    public class Query5
    {
        public IAccount5 Account => new Account5(1);
    }

    public interface IAccount5
    {
        int Id { get; }
    }

    public record Account5(int Id) : IAccount5;

    public class Query6
    {
        public IAccount6 Account => new User6(1);
    }

    public interface IAccount6;

    public record User6(int Id) : IAccount6;

    public record Organization6(int Id) : IAccount6;

    public class UserType6 : ObjectType<User6>;

    public class OrganizationType6 : ObjectType<Organization6>;

    public class Query7
    {
        public IAccount7 Account => new User7(1);
    }

    [UnionType]
    [Context("accountContext")]
    public interface IAccount7;

    public record User7(int Id) : IAccount7;

    public class Query8
    {
        public Book8 Book => new(1);
    }

    public record Book8(int Id)
    {
        public string Price([Require("$userContext.userCurrency.isoCode")] string isoCode)
            => isoCode;
    }
}
