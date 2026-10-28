namespace HotChocolate.Types;

public class ObjectTypeTests
{
    [Fact]
    public async Task GenerateSource_BatchDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public sealed class Book
            {
                public int Id { get; set; }
                public string Title { get; set; }
                public int AuthorId { get; set; }
            }

            [ObjectType<Book>]
            internal static partial class BookNode
            {
                [BindMember(nameof(Book.AuthorId))]
                public static Task<Author?> GetAuthorAsync(
                    [Parent] Book book,
                    CancellationToken cancellationToken)
                    => default;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task CustomAttribute_On_NodeResolver_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Reflection;
            using HotChocolate.Types;
            using HotChocolate.Types.Descriptors;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            public sealed class FooAttribute(string name) : ObjectFieldDescriptorAttribute
            {
                public string Name { get; } = name;

                protected override void OnConfigure(
                    IDescriptorContext context,
                    IObjectFieldDescriptor descriptor,
                    MemberInfo? member)
                    => descriptor.Directive(Name);
            }

            public sealed record Product(int Id, string Name);

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                [NodeResolver]
                [Foo("bar")]
                public static Product GetProduct(int id) => new(id, "abc");
            }
            """).MatchMarkdownAsync();
    }
}

