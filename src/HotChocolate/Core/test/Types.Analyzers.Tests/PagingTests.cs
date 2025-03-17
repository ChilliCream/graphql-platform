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
            """).MatchMarkdownAsync();
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
            """).MatchMarkdownAsync();
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
            """).MatchMarkdownAsync();
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
            """).MatchMarkdownAsync();
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
            """).MatchMarkdownAsync();
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
            """).MatchMarkdownAsync();
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
            """).MatchMarkdownAsync();
    }
}
