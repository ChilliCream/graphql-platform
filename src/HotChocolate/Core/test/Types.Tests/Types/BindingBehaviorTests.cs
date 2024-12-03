using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class BindingBehaviorTests
{
    [Fact]
    public async Task BindingBehavior_Explicit_All_Attributes()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType()
                .AddType<Query1>()
                .ModifyOptions(o => o.DefaultBindingBehavior = BindingBehavior.Explicit)
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Book1 {
              title: String
              category: BookCategory1!
            }

            type Query {
              books: Book1
            }

            enum BookCategory1 {
              A
              B
              C
            }
            """);
    }

    [QueryType]
    public class Query1
    {
        public Book1 GetBooks() => new("Abc", BookCategory1.B);
    }

    [ObjectType]
    public record Book1(string Title, BookCategory1 Category);

    [EnumType]
    public enum BookCategory1
    {
        A,
        B,
        C,
    }

    [Fact]
    public async Task BindingBehavior_Explicit_Enum_Missing_Attribute()
    {
        async Task Error() =>
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType()
                .AddType<Query2>()
                .ModifyOptions(o => o.DefaultBindingBehavior = BindingBehavior.Explicit)
                .BuildSchemaAsync();

        var error = await Assert.ThrowsAsync<SchemaException>(Error);

        error.Message.MatchInlineSnapshot(
            """
            For more details look at the `Errors` property.

            1. The enum type `BookCategory2` has no values. (HotChocolate.Types.EnumType<HotChocolate.Types.BindingBehaviorTests.BookCategory2>)

            """);
    }

    [QueryType]
    public class Query2
    {
        public Book2 GetBooks() => new("Abc", BookCategory2.B);
    }

    [ObjectType]
    public record Book2(string Title, BookCategory2 Category);

    public enum BookCategory2
    {
        A,
        B,
        C,
    }

    [Fact]
    public async Task BindingBehavior_Explicit_Enum_Bound_With_Fluent()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType()
                .AddType<Query3>()
                .AddType<BookCategory3Type>()
                .ModifyOptions(o => o.DefaultBindingBehavior = BindingBehavior.Explicit)
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Book3 {
              title: String
              category: BookCategory3!
            }

            type Query {
              books: Book3
            }

            enum BookCategory3 {
              A
            }
            """);
    }

    [QueryType]
    public class Query3
    {
        public Book3 GetBooks() => new("Abc", BookCategory3.B);
    }

    [ObjectType]
    public record Book3(string Title, BookCategory3 Category);

    public enum BookCategory3
    {
        A,
        B,
        C,
    }

    public class BookCategory3Type : EnumType<BookCategory3>
    {
        protected override void Configure(IEnumTypeDescriptor<BookCategory3> descriptor)
        {
            descriptor.Value(BookCategory3.A);
        }
    }
}
