namespace HotChocolate.Types;

public class LookupReturnsListTypeAnalyzerTests
{
    [Fact]
    public async Task Method_ListReturn_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            #nullable enable
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using System.Collections.Generic;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                [Lookup]
                public static List<User?> GetUsersById(int id) => default!;
            }

            public class User
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Method_ArrayReturn_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            #nullable enable
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                [Lookup]
                public static User?[] GetUsersById(int id) => default!;
            }

            public class User
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Method_IEnumerableReturn_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            #nullable enable
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using System.Collections.Generic;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                [Lookup]
                public static IEnumerable<User?> GetUsersById(int id) => default!;
            }

            public class User
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Method_TaskListReturn_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            #nullable enable
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                [Lookup]
                public static Task<List<User?>> GetUsersByIdAsync(int id) => default!;
            }

            public class User
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Property_ListReturn_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            #nullable enable
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using System.Collections.Generic;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                [Lookup]
                public static List<User?> AllUsers => default!;
            }

            public class User
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Method_SingleReturn_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            #nullable enable
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                [Lookup]
                public static User? GetUserById(int id) => default;
            }

            public class User
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Method_NoLookupAttribute_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            #nullable enable
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using System.Collections.Generic;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                public static List<User?> GetUsersById(int id) => default!;
            }

            public class User
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }
}
