namespace HotChocolate.Types;

public class ShareableScopedOnMemberAnalyzerTests
{
    [Fact]
    public async Task Method_WithScopedTrue_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Book>]
            internal static partial class BookNode
            {
                [Shareable(scoped: true)]
                public static Task<Author?> GetAuthorAsync(
                    [Parent] Book book,
                    CancellationToken cancellationToken)
                    => default;
            }

            public class Book
            {
                public int Id { get; set; }
                public string Title { get; set; }
                public int AuthorId { get; set; }
            }

            public class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Method_WithScopedFalse_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Book>]
            internal static partial class BookNode
            {
                [Shareable(scoped: false)]
                public static Task<Author?> GetAuthorAsync(
                    [Parent] Book book,
                    CancellationToken cancellationToken)
                    => default;
            }

            public class Book
            {
                public int Id { get; set; }
                public string Title { get; set; }
                public int AuthorId { get; set; }
            }

            public class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Method_WithoutScoped_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Book>]
            internal static partial class BookNode
            {
                [Shareable]
                public static Task<Author?> GetAuthorAsync(
                    [Parent] Book book,
                    CancellationToken cancellationToken)
                    => default;
            }

            public class Book
            {
                public int Id { get; set; }
                public string Title { get; set; }
                public int AuthorId { get; set; }
            }

            public class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Property_WithScopedTrue_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;

            namespace TestNamespace;

            [ObjectType<Book>]
            internal static partial class BookNode
            {
                [Shareable(scoped: true)]
                public static string Title => "Test";
            }

            public class Book
            {
                public int Id { get; set; }
                public string Title { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Property_WithoutScoped_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;

            namespace TestNamespace;

            [ObjectType<Book>]
            internal static partial class BookNode
            {
                [Shareable]
                public static string Title => "Test";
            }

            public class Book
            {
                public int Id { get; set; }
                public string Title { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Class_WithScopedTrue_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;

            namespace TestNamespace;

            [Shareable(scoped: true)]
            [ObjectType<Book>]
            internal static partial class BookNode
            {
                public static string GetTitle() => "Test";
            }

            public class Book
            {
                public int Id { get; set; }
                public string Title { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Method_NoShareableAttribute_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Book>]
            internal static partial class BookNode
            {
                public static Task<Author?> GetAuthorAsync(
                    [Parent] Book book,
                    CancellationToken cancellationToken)
                    => default;
            }

            public class Book
            {
                public int Id { get; set; }
                public string Title { get; set; }
                public int AuthorId { get; set; }
            }

            public class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }
}
