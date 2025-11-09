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
    public async Task Shareable_On_Class()
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

            [Shareable]
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
    public async Task Shareable_On_Class_Scoped()
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

            [Shareable(scoped: true)]
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
    public async Task Shareable_On_Field()
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
                [Shareable]
                [BindMember(nameof(Book.AuthorId))]
                public static Task<Author?> GetAuthorAsync(
                    [Parent] Book book,
                    CancellationToken cancellationToken)
                    => default;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Argument_With_DefaultValue()
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

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                public static string GetFoo(string bar = "baz")
                    => bar;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task XmlDocumentation_With_MultilineDescription()
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

            [QueryType]
            internal static partial class Query
            {
                /// <summary>
                /// This is a multiline description.
                /// It spans multiple lines.
                /// And should be properly normalized.
                /// </summary>
                public static string GetUser()
                    => "User";
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task XmlDocumentation_With_SpecialCharacters()
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

            [QueryType]
            internal static partial class Query
            {
                /// <summary>
                /// Use "quotes" and `backticks` here.
                /// Path: C:\Windows\System32
                /// </summary>
                public static string GetPath()
                    => "Path";
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task XmlDocumentation_With_ParameterDescription()
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

            [QueryType]
            internal static partial class Query
            {
                /// <summary>
                /// Gets a user by ID.
                /// Returns null if not found.
                /// </summary>
                /// <param name="userId">The user's unique identifier with "quotes"</param>
                /// <param name="includeDeleted">Include deleted users (default: false)</param>
                public static string GetUserById(int userId, bool includeDeleted = false)
                    => $"User {userId}";
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task XmlDocumentation_With_ComplexScenario()
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

            [QueryType]
            internal static partial class Query
            {
                /// <summary>
                /// Executes a complex query with:
                /// - Multiple lines
                /// - Special chars: "quotes", 'apostrophes', `backticks`
                /// - Paths: C:\Program Files\App
                /// - Tab	separated	values
                /// </summary>
                /// <param name="query">SQL query like: SELECT * FROM "Users" WHERE name = 'John'</param>
                /// <param name="timeout">Timeout in ms (default: 30000)</param>
                public static string ExecuteQuery(string query, int timeout = 30000)
                    => query;
            }
            """).MatchMarkdownAsync();
    }
}
