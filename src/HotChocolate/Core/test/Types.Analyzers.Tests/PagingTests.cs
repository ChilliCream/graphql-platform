namespace HotChocolate.Types;

public class PagingTests
{
    [Fact]
    public async Task GenerateSource_ConnectionT_MatchesSnapshot()
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

            [QueryType]
            public static partial class BookPage
            {
                public static Task<HotChocolate.Types.Pagination.Connection<Author>> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_CustomConnection_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [QueryType]
            public static partial class AuthorQueries
            {
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
            {
                public override IReadOnlyList<AuthorEdge> Edges => default!;

                public IReadOnlyList<Author> Nodes => default!;

                public override ConnectionPageInfo PageInfo => default!;

                public int TotalCount => 0;
            }

            public class AuthorEdge : IEdge<Author>
            {
                public Author Node => default!;

                object? IEdge.Node => Node;

                public string Cursor => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_CustomConnection_UseConnection_IncludeTotalCount_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [QueryType]
            public static partial class AuthorQueries
            {
                [UseConnection(IncludeTotalCount = true)]
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
            {
                public override IReadOnlyList<AuthorEdge> Edges => default!;

                public IReadOnlyList<Author> Nodes => default!;

                public override ConnectionPageInfo PageInfo => default!;

                public int TotalCount => 0;
            }

            public class AuthorEdge : IEdge<Author>
            {
                public Author Node => default!;

                object? IEdge.Node => Node;

                public string Cursor => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_CustomConnection_UseConnection_ConnectionName_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [QueryType]
            public static partial class AuthorQueries
            {
                [UseConnection(Name = "Authors123")]
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
            {
                public override IReadOnlyList<AuthorEdge> Edges => default!;

                public IReadOnlyList<Author> Nodes => default!;

                public override ConnectionPageInfo PageInfo => default!;

                public int TotalCount => 0;
            }

            public class AuthorEdge : IEdge<Author>
            {
                public Author Node => default!;

                object? IEdge.Node => Node;

                public string Cursor => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_CustomConnection_No_Duplicates_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;

                    public static Task<AuthorConnection> GetAuthors2Async(
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace.Types.Nodes
            {
                [ObjectType<Author>]
                public static partial class AuthorNode
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        [Parent] Author author,
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
                {
                    public override IReadOnlyList<AuthorEdge> Edges => default!;

                    public IReadOnlyList<Author> Nodes => default!;

                    public override ConnectionPageInfo PageInfo => default!;

                    public int TotalCount => 0;
                }

                public class AuthorEdge : IEdge<Author>
                {
                    public Author Node => default!;

                    object? IEdge.Node => Node;

                    public string Cursor => default!;
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_GenericCustomConnection_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<CustomConnection<Author>> GetAuthorsAsync(
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;

                    public static Task<CustomConnection<Author>> GetAuthors2Async(
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace.Types.Nodes
            {
                [ObjectType<Author>]
                public static partial class AuthorNode
                {
                    public static Task<CustomConnection<Author>> GetAuthorsAsync(
                        [Parent] Author author,
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                public class CustomConnection<T> : ConnectionBase<T, CustomEdge<T>, ConnectionPageInfo>
                {
                    public override IReadOnlyList<CustomEdge<T>> Edges => default!;

                    public IReadOnlyList<T> Nodes => default!;

                    public override ConnectionPageInfo PageInfo => default!;

                    public int TotalCount => 0;
                }

                public class CustomEdge<T> : IEdge<T>
                {
                    public T Node => default!;

                    object? IEdge.Node => Node;

                    public string Cursor => default!;
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_GenericCustomConnection_WithConnectionName_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<CustomConnection<Author>> GetAuthorsAsync(
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;

                    [UseConnection(Name = "Authors2")]
                    public static Task<CustomConnection<Author>> GetAuthors2Async(
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace.Types.Nodes
            {
                [ObjectType<Author>]
                public static partial class AuthorNode
                {
                    public static Task<CustomConnection<Author>> GetAuthorsAsync(
                        [Parent] Author author,
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                public class CustomConnection<T> : ConnectionBase<T, CustomEdge<T>, ConnectionPageInfo>
                {
                    public override IReadOnlyList<CustomEdge<T>>? Edges => default!;

                    public IReadOnlyList<T> Nodes => default!;

                    public override ConnectionPageInfo PageInfo => default!;

                    public int TotalCount => 0;
                }

                public class CustomEdge<T> : IEdge<T>
                {
                    public T Node => default!;

                    object? IEdge.Node => Node;

                    public string Cursor => default!;
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_Inherit_From_PageConnection()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                public class AuthorConnection : PageConnection<Author>
                {
                    public AuthorConnection(GreenDonut.Data.Page<Author> page)
                        : base(page)
                    {
                    }

                    public string CustomResolver() => "Foo";
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_Inherit_From_PageConnection_In_Referenced_Assembly()
    {
        // arrange
        var referencedAssembly = TestHelper.CreateReference(
            """
            using GreenDonut.Data;
            using HotChocolate.Types.Pagination;

            namespace TestLibrary;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class AuthorConnection : PageConnection<Author>
            {
                public AuthorConnection(Page<Author> page)
                    : base(page)
                {
                }

                public string CustomResolver() => "Foo";
            }
            """,
            "TestLibrary");

        // act
        var snapshot = TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using TestLibrary;

            namespace TestNamespace.Types.Root;

            [QueryType]
            public static partial class AuthorQueries
            {
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """,
            [referencedAssembly]);

        // assert
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_Inherit_From_ConnectionBase_Reuse_PageEdge()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                public class AuthorConnection : ConnectionBase<Author, PageEdge<Author>, ConnectionPageInfo>
                {
                    public override IReadOnlyList<PageEdge<Author>>? Edges => default!;

                    public IReadOnlyList<Author> Nodes => default!;

                    public override ConnectionPageInfo PageInfo => default!;

                    public int TotalCount => 0;
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_Inherit_From_ConnectionBase_Reuse_PageEdge_Generic()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<CustomConnection<Author>> GetAuthorsAsync(
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                public class CustomConnection<T> : ConnectionBase<T, PageEdge<T>, ConnectionPageInfo>
                {
                    public override IReadOnlyList<PageEdge<T>>? Edges => default!;

                    public IReadOnlyList<T> Nodes => default!;

                    public override ConnectionPageInfo PageInfo => default!;

                    public int TotalCount => 0;
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_Inherit_From_ConnectionBase_Inherit_PageEdge()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
                {
                    public override IReadOnlyList<AuthorEdge>? Edges => default!;

                    public IReadOnlyList<Author> Nodes => default!;

                    public override ConnectionPageInfo PageInfo => default!;

                    public int TotalCount => 0;
                }

                public class AuthorEdge(
                    GreenDonut.Data.Page<Author> page,
                    GreenDonut.Data.PageEntry<Author> entry) : PageEdge<Author>(page, entry)
                {
                    public Author Author => Node;
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_Inherit_From_ConnectionBase_PageEdge_In_Referenced_Assembly()
    {
        // arrange
        var referencedAssembly = TestHelper.CreateReference(
            """
            using System.Collections.Generic;
            using GreenDonut.Data;
            using HotChocolate.Types.Pagination;

            namespace TestLibrary;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
            {
                public override IReadOnlyList<AuthorEdge>? Edges => default!;

                public IReadOnlyList<Author> Nodes => default!;

                public override ConnectionPageInfo PageInfo => default!;

                public int TotalCount => 0;
            }

            public class AuthorEdge(
                Page<Author> page,
                PageEntry<Author> entry) : PageEdge<Author>(page, entry)
            {
                public Author Author => Node;
            }
            """,
            "TestLibrary");

        // act
        var snapshot = TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using TestLibrary;

            namespace TestNamespace.Types.Root;

            [QueryType]
            public static partial class AuthorQueries
            {
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """,
            [referencedAssembly]);

        // assert
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_ConnectionFlags()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        GreenDonut.Data.PagingArguments pagingArgs,
                        ConnectionFlags flags,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
                {
                    public override IReadOnlyList<AuthorEdge>? Edges => default!;

                    public IReadOnlyList<Author> Nodes => default!;

                    public override ConnectionPageInfo PageInfo => default!;

                    public int TotalCount => 0;
                }

                public class AuthorEdge(
                    GreenDonut.Data.Page<Author> page,
                    GreenDonut.Data.PageEntry<Author> entry) : PageEdge<Author>(page, entry)
                {
                    public Author Author => Node;
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Shareable_On_Connection_Class()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [QueryType]
            public static partial class AuthorQueries
            {
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }

            [Shareable]
            public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
            {
                public override IReadOnlyList<AuthorEdge> Edges => default!;

                public IReadOnlyList<Author> Nodes => default!;

                public override ConnectionPageInfo PageInfo => default!;

                public int TotalCount => 0;
            }

            public class AuthorEdge : IEdge<Author>
            {
                public Author Node => default!;

                object? IEdge.Node => Node;

                public string Cursor => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Shareable_On_Connection_Class_Scoped()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [QueryType]
            public static partial class AuthorQueries
            {
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }

            [Shareable(scoped: true)]
            public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
            {
                public override IReadOnlyList<AuthorEdge> Edges => default!;

                public IReadOnlyList<Author> Nodes => default!;

                public override ConnectionPageInfo PageInfo => default!;

                public int TotalCount => 0;
            }

            public class AuthorEdge : IEdge<Author>
            {
                public Author Node => default!;

                object? IEdge.Node => Node;

                public string Cursor => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Shareable_On_Connection_Field()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [QueryType]
            public static partial class AuthorQueries
            {
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
            {
                [Shareable]
                public override IReadOnlyList<AuthorEdge> Edges => default!;

                public IReadOnlyList<Author> Nodes => default!;

                public override ConnectionPageInfo PageInfo => default!;

                public int TotalCount => 0;
            }

            public class AuthorEdge : IEdge<Author>
            {
                public Author Node => default!;

                object? IEdge.Node => Node;

                public string Cursor => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Shareable_On_Edge_Class()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [QueryType]
            public static partial class AuthorQueries
            {
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
            {
                public override IReadOnlyList<AuthorEdge> Edges => default!;

                public IReadOnlyList<Author> Nodes => default!;

                public override ConnectionPageInfo PageInfo => default!;

                public int TotalCount => 0;
            }

            [Shareable]
            public class AuthorEdge : IEdge<Author>
            {
                public Author Node => default!;

                object? IEdge.Node => Node;

                public string Cursor => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Shareable_On_Edge_Class_Scoped()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [QueryType]
            public static partial class AuthorQueries
            {
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
            {
                public override IReadOnlyList<AuthorEdge> Edges => default!;

                public IReadOnlyList<Author> Nodes => default!;

                public override ConnectionPageInfo PageInfo => default!;

                public int TotalCount => 0;
            }

            [Shareable(scoped: true)]
            public class AuthorEdge : IEdge<Author>
            {
                public Author Node => default!;

                object? IEdge.Node => Node;

                public string Cursor => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Shareable_On_Edge_Field()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [QueryType]
            public static partial class AuthorQueries
            {
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
            {
                public override IReadOnlyList<AuthorEdge> Edges => default!;

                public IReadOnlyList<Author> Nodes => default!;

                public override ConnectionPageInfo PageInfo => default!;

                public int TotalCount => 0;
            }

            public class AuthorEdge : IEdge<Author>
            {
                [Shareable]
                public Author Node => default!;

                object? IEdge.Node => Node;

                public string Cursor => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Shareable_On_PageConnection()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                [Shareable]
                public class AuthorConnection : PageConnection<Author>
                {
                    public AuthorConnection(GreenDonut.Data.Page<Author> page)
                        : base(page)
                    {
                    }

                    public string CustomResolver() => "Foo";
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Shareable_On_PageConnection_Scoped()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                [Shareable(scoped: true)]
                public class AuthorConnection : PageConnection<Author>
                {
                    public AuthorConnection(GreenDonut.Data.Page<Author> page)
                        : base(page)
                    {
                    }

                    public string CustomResolver() => "Foo";
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Inaccessible_On_Connection_Class()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [QueryType]
            public static partial class AuthorQueries
            {
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }

            [Inaccessible]
            public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
            {
                public override IReadOnlyList<AuthorEdge> Edges => default!;

                public IReadOnlyList<Author> Nodes => default!;

                public override ConnectionPageInfo PageInfo => default!;

                public int TotalCount => 0;
            }

            public class AuthorEdge : IEdge<Author>
            {
                public Author Node => default!;

                object? IEdge.Node => Node;

                public string Cursor => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Inaccessible_On_Connection_Class_Scoped()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [QueryType]
            public static partial class AuthorQueries
            {
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }

            [Inaccessible(scoped: true)]
            public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
            {
                public override IReadOnlyList<AuthorEdge> Edges => default!;

                public IReadOnlyList<Author> Nodes => default!;

                public override ConnectionPageInfo PageInfo => default!;

                public int TotalCount => 0;
            }

            public class AuthorEdge : IEdge<Author>
            {
                public Author Node => default!;

                object? IEdge.Node => Node;

                public string Cursor => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Inaccessible_On_Connection_Field()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [QueryType]
            public static partial class AuthorQueries
            {
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
            {
                [Inaccessible]
                public override IReadOnlyList<AuthorEdge> Edges => default!;

                public IReadOnlyList<Author> Nodes => default!;

                public override ConnectionPageInfo PageInfo => default!;

                public int TotalCount => 0;
            }

            public class AuthorEdge : IEdge<Author>
            {
                public Author Node => default!;

                object? IEdge.Node => Node;

                public string Cursor => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Inaccessible_On_Edge_Class()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [QueryType]
            public static partial class AuthorQueries
            {
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
            {
                public override IReadOnlyList<AuthorEdge> Edges => default!;

                public IReadOnlyList<Author> Nodes => default!;

                public override ConnectionPageInfo PageInfo => default!;

                public int TotalCount => 0;
            }

            [Inaccessible]
            public class AuthorEdge : IEdge<Author>
            {
                public Author Node => default!;

                object? IEdge.Node => Node;

                public string Cursor => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Inaccessible_On_Edge_Class_Scoped()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [QueryType]
            public static partial class AuthorQueries
            {
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
            {
                public override IReadOnlyList<AuthorEdge> Edges => default!;

                public IReadOnlyList<Author> Nodes => default!;

                public override ConnectionPageInfo PageInfo => default!;

                public int TotalCount => 0;
            }

            [Inaccessible(scoped: true)]
            public class AuthorEdge : IEdge<Author>
            {
                public Author Node => default!;

                object? IEdge.Node => Node;

                public string Cursor => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Inaccessible_On_Edge_Field()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [QueryType]
            public static partial class AuthorQueries
            {
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class AuthorConnection : ConnectionBase<Author, AuthorEdge, ConnectionPageInfo>
            {
                public override IReadOnlyList<AuthorEdge> Edges => default!;

                public IReadOnlyList<Author> Nodes => default!;

                public override ConnectionPageInfo PageInfo => default!;

                public int TotalCount => 0;
            }

            public class AuthorEdge : IEdge<Author>
            {
                [Inaccessible]
                public Author Node => default!;

                object? IEdge.Node => Node;

                public string Cursor => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Inaccessible_On_PageConnection()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                [Inaccessible]
                public class AuthorConnection : PageConnection<Author>
                {
                    public AuthorConnection(GreenDonut.Data.Page<Author> page)
                        : base(page)
                    {
                    }

                    public string CustomResolver() => "Foo";
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Inaccessible_On_PageConnection_Scoped()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        GreenDonut.Data.PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                [Inaccessible(scoped: true)]
                public class AuthorConnection : PageConnection<Author>
                {
                    public AuthorConnection(GreenDonut.Data.Page<Author> page)
                        : base(page)
                    {
                    }

                    public string CustomResolver() => "Foo";
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_StreamPageConnection_Task()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<StreamPageConnection<Author>> GetAuthorsAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_StreamPageConnection_ValueTask()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static ValueTask<StreamPageConnection<Author>> GetAuthorsAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_StreamPageConnection_Direct()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static StreamPageConnection<Author> GetAuthors(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_StreamPageConnection_UseConnection_Options()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    [UseConnection(Name = "Writers", IncludeTotalCount = true)]
                    public static Task<StreamPageConnection<Author>> GetAuthorsAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_Inherit_From_StreamPageConnection()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                public class AuthorConnection : StreamPageConnection<Author>
                {
                    public AuthorConnection(StreamPage<Author> page)
                        : base(page)
                    {
                    }

                    public string CustomResolver() => "Foo";
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_Inherit_From_StreamPageConnection_Direct()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static AuthorConnection GetAuthors(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                public class AuthorConnection : StreamPageConnection<Author>
                {
                    public AuthorConnection(StreamPage<Author> page)
                        : base(page)
                    {
                    }

                    public string CustomResolver() => "Foo";
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_StreamPageConnection_ConnectionFlags_And_QueryContext()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<StreamPageConnection<Author>> GetAuthorsAsync(
                        PagingArguments pagingArgs,
                        QueryContext<Author> query,
                        ConnectionFlags flags,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_StreamPageConnection_No_Duplicates()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;

                    public static ValueTask<AuthorConnection> GetAuthors2Async(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace.Types.Nodes
            {
                [ObjectType<Author>]
                public static partial class AuthorNode
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        [Parent] Author author,
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                public class AuthorConnection : StreamPageConnection<Author>
                {
                    public AuthorConnection(StreamPage<Author> page)
                        : base(page)
                    {
                    }

                    public string CustomResolver() => "Foo";
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_StreamPageConnection_And_PageConnection_With_Same_Node_Name()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<PageConnection<Author>> GetAuthorsAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;

                    public static Task<StreamPageConnection<Author>> GetStreamedAuthorsAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_PageInfo_Is_Shared_Across_Connection_Backings_With_Async_Custom_Members()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }

                public sealed class Book
                {
                    public int Id { get; set; }
                    public string Title { get; set; }
                }

                public sealed class Magazine
                {
                    public int Id { get; set; }
                    public string Title { get; set; }
                }

                public sealed class Journal
                {
                    public int Id { get; set; }
                    public string Title { get; set; }
                }

                public sealed class Newsletter
                {
                    public int Id { get; set; }
                    public string Title { get; set; }
                }

                public class AuthorConnection : PageConnection<Author>
                {
                    public AuthorConnection(Page<Author> page)
                        : base(page)
                    {
                    }

                    public ValueTask<int> GetIssueCountAsync(CancellationToken cancellationToken)
                        => default;

                    public ValueTask<bool> HasSpecialsAsync(CancellationToken cancellationToken)
                        => default;
                }

                public class BookConnection : StreamPageConnection<Book>
                {
                    public BookConnection(StreamPage<Book> page)
                        : base(page)
                    {
                    }

                    public ValueTask<int> GetIssueCountAsync(CancellationToken cancellationToken)
                        => default;

                    public ValueTask<bool> HasSpecialsAsync(CancellationToken cancellationToken)
                        => default;
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class Queries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;

                    public static Task<BookConnection> GetBooksAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;

                    public static Task<PageConnection<Magazine>> GetPagedMagazinesAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;

                    public static Task<StreamPageConnection<Journal>> GetStreamedJournalsAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;

                    [UsePaging]
                    public static IEnumerable<Newsletter> GetClassicNewsletters() => [];
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_StreamPageConnection_With_Different_Node_Types()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace
            {
                public sealed class Book
                {
                    public int Id { get; set; }
                    public string Title { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class Queries
                {
                    public static Task<StreamPageConnection<Author>> GetAuthorsAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;

                    public static Task<StreamPageConnection<Book>> GetBooksAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Shareable_On_StreamPageConnection()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                [Shareable]
                public class AuthorConnection : StreamPageConnection<Author>
                {
                    public AuthorConnection(StreamPage<Author> page)
                        : base(page)
                    {
                    }

                    public string CustomResolver() => "Foo";
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Shareable_On_StreamPageConnection_Scoped()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                [Shareable(scoped: true)]
                public class AuthorConnection : StreamPageConnection<Author>
                {
                    public AuthorConnection(StreamPage<Author> page)
                        : base(page)
                    {
                    }

                    public string CustomResolver() => "Foo";
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Inaccessible_On_StreamPageConnection()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                [Inaccessible]
                public class AuthorConnection : StreamPageConnection<Author>
                {
                    public AuthorConnection(StreamPage<Author> page)
                        : base(page)
                    {
                    }

                    public string CustomResolver() => "Foo";
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Inaccessible_On_StreamPageConnection_Scoped()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                [Inaccessible(scoped: true)]
                public class AuthorConnection : StreamPageConnection<Author>
                {
                    public AuthorConnection(StreamPage<Author> page)
                        : base(page)
                    {
                    }

                    public string CustomResolver() => "Foo";
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_Inherit_From_StreamPageConnection_In_Referenced_Assembly()
    {
        // arrange
        var referencedAssembly = TestHelper.CreateReference(
            """
            using GreenDonut.Data;
            using HotChocolate.Types.Pagination;

            namespace TestLibrary;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class AuthorConnection : StreamPageConnection<Author>
            {
                public AuthorConnection(StreamPage<Author> page)
                    : base(page)
                {
                }

                public string CustomResolver() => "Foo";
            }
            """,
            "TestLibrary");

        // act
        var snapshot = TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using TestLibrary;

            namespace TestNamespace.Types.Root;

            [QueryType]
            public static partial class AuthorQueries
            {
                public static Task<AuthorConnection> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """,
            [referencedAssembly]);

        // assert
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_Inherit_From_StreamPageConnection_Override_Nodes()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            namespace TestNamespace
            {
                public sealed class Author
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }
            }

            namespace TestNamespace.Types.Root
            {
                [QueryType]
                public static partial class AuthorQueries
                {
                    public static Task<AuthorConnection> GetAuthorsAsync(
                        PagingArguments pagingArgs,
                        CancellationToken cancellationToken)
                        => default!;
                }
            }

            namespace TestNamespace
            {
                public class AuthorConnection : StreamPageConnection<Author>
                {
                    public AuthorConnection(StreamPage<Author> page)
                        : base(page)
                    {
                    }

                    public override IAsyncEnumerable<Author>? GetNodesAsync(
                        CancellationToken cancellationToken = default)
                        => base.GetNodesAsync(cancellationToken);
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }
}
