namespace HotChocolate.Types;

public class LookupReturnsNonNullableTypeAnalyzerTests
{
    [Fact]
    public async Task Method_NonNullableReturn_RaisesWarning()
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
                public static User GetUserById(int id) => default!;
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
    public async Task Method_TaskNonNullableReturn_RaisesWarning()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            #nullable enable
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                [Lookup]
                public static Task<User> GetUserByIdAsync(int id) => default!;
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
    public async Task Method_ValueTaskNonNullableReturn_RaisesWarning()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            #nullable enable
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                [Lookup]
                public static ValueTask<User> GetUserByIdAsync(int id) => default!;
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
    public async Task Property_NonNullableReturn_RaisesWarning()
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
                public static User CurrentUser => default!;
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
    public async Task Method_NullableReturn_NoWarning()
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
    public async Task Method_TaskNullableReturn_NoWarning()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            #nullable enable
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                [Lookup]
                public static Task<User?> GetUserByIdAsync(int id) => default!;
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
    public async Task Method_NoLookupAttribute_NoWarning()
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
                public static User GetUserById(int id) => default!;
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
    public async Task Property_NullableReturn_NoWarning()
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
                public static User? CurrentUser => default;
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
