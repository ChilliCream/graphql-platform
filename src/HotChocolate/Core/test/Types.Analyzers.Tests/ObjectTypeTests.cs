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

    [Fact]
    public async Task XmlDocumentation_With_InheritdocFromBaseClass()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate.Types;

            namespace TestNamespace;

            [ObjectType<Query>]
            public static partial class QueryType
            {
            }

            public abstract class BaseQuery
            {
                /// <summary>
                /// Gets the user by their unique identifier.
                /// </summary>
                public virtual string GetUser() => "User";
            }

            public class Query : BaseQuery
            {
                /// <inheritdoc />
                public override string GetUser() => "Query User";
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task XmlDocumentation_With_InheritdocFromInterface()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate.Types;

            namespace TestNamespace;

            [ObjectType<Query>]
            public static partial class QueryType
            {
            }

            public interface IQuery
            {
                /// <summary>
                /// Retrieves the current user from the context.
                /// </summary>
                string GetCurrentUser();
            }

            public class Query : IQuery
            {
                /// <inheritdoc />
                public string GetCurrentUser() => "Current User";
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task XmlDocumentation_With_InheritdocCref()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate.Types;

            namespace TestNamespace;

            [ObjectType<Query>]
            public static partial class QueryType
            {
            }

            public interface IUserResolver
            {
                /// <summary>
                /// Resolves user information by ID.
                /// </summary>
                string ResolveUser();
            }

            public class Query
            {
                /// <inheritdoc cref="IUserResolver.ResolveUser"/>
                public string GetUserInfo() => "User Info";
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task CustomAttribute_On_Parameter_MatchesSnapshot()
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

            public sealed class FooAttribute : Attribute;

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
                    [Foo] int version,
                    CancellationToken cancellationToken)
                    => default;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GraphQLType_On_Parameter_MatchesSnapshot()
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
                    [GraphQLType<StringType>] int version,
                    CancellationToken cancellationToken)
                    => default;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GraphQLType_String_On_Parameter_MatchesSnapshot()
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
                    [GraphQLType("[String]")] int version,
                    CancellationToken cancellationToken)
                    => default;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GraphQLType_NonGeneric_On_Parameter_MatchesSnapshot()
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
                    [GraphQLType(typeof(StringType))] int version,
                    CancellationToken cancellationToken)
                    => default;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GraphQLType_On_Resolver_MatchesSnapshot()
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
                [GraphQLType<StringType>]
                [BindMember(nameof(Book.AuthorId))]
                public static Task<Author?> GetAuthorAsync(
                    [Parent] Book book,
                    [GraphQLType<StringType>] int version,
                    CancellationToken cancellationToken)
                    => default;
            }
            """).MatchMarkdownAsync();
    }
}
