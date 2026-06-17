namespace HotChocolate.Types;

public class ConnectionTests
{
    [Fact]
    public async Task Build_Schema_Should_Reuse_Single_Connection_When_Same_Type_Paged_Twice_In_One_Assembly()
    {
        // arrange
        const string source =
            """
            using System.Linq;
            using HotChocolate.Types;

            namespace Demo.SingleAssembly;

            public sealed class Author
            {
                public int Id { get; set; }

                public string Name { get; set; } = string.Empty;
            }

            [QueryType]
            public static partial class Query
            {
                [UsePaging(InferConnectionNameFromField = false)]
                public static IQueryable<Author> GetAuthors() => default!;

                [UsePaging(InferConnectionNameFromField = false)]
                public static IQueryable<Author> GetMoreAuthors() => default!;
            }
            """;

        // act
        var schema = await GeneratorTestServer.CreateSchemaAsync(
            source,
            disableDefaultSecurity: false);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Build_Schema_Should_Reuse_Single_Connection_When_Same_Type_Paged_Across_Two_Assemblies()
    {
        // arrange
        const string authorAssemblySource =
            """
            namespace Demo.CrossAssembly;

            public sealed class Author
            {
                public int Id { get; set; }

                public string Name { get; set; } = string.Empty;
            }
            """;

        const string assemblyOneSource =
            """
            using System.Linq;
            using HotChocolate.Types;
            using Demo.CrossAssembly;

            namespace Demo.CrossAssembly.One;

            [QueryType]
            public static partial class AssemblyOneQuery
            {
                [UsePaging(InferConnectionNameFromField = false)]
                public static IQueryable<Author> GetAuthors() => default!;
            }
            """;

        const string assemblyTwoSource =
            """
            using System.Linq;
            using HotChocolate.Types;
            using Demo.CrossAssembly;

            namespace Demo.CrossAssembly.Two;

            [QueryType]
            public static partial class AssemblyTwoQuery
            {
                [UsePaging(InferConnectionNameFromField = false)]
                public static IQueryable<Author> GetMoreAuthors() => default!;
            }
            """;

        var assemblies = new GeneratorAssembly[]
        {
            new(
                "Demo.ConnectionAssemblyAuthor",
                [authorAssemblySource],
                References: [],
                Register: false),
            new(
                "Demo.ConnectionAssemblyOne",
                [assemblyOneSource],
                References: ["Demo.ConnectionAssemblyAuthor"]),
            new(
                "Demo.ConnectionAssemblyTwo",
                [assemblyTwoSource],
                References: ["Demo.ConnectionAssemblyAuthor"])
        };

        // act
        var schema = await GeneratorTestServer.CreateSchemaAsync(
            assemblies,
            disableDefaultSecurity: false);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Build_Schema_Should_Reuse_Single_Connection_When_Static_And_NonStatic_Query_Page_Same_Type_In_One_Assembly()
    {
        // arrange
        // A reflection-based (non-static) query type and a source-generated (static partial)
        // query type page the same Author in the same assembly, so both fields should resolve
        // to one shared "AuthorConnection".
        const string source =
            """
            using System.Linq;
            using HotChocolate.Types;

            namespace Demo.MixedSingleAssembly;

            public sealed class Author
            {
                public int Id { get; set; }

                public string Name { get; set; } = string.Empty;
            }

            [QueryType]
            public class RuntimeQuery
            {
                [UsePaging(InferConnectionNameFromField = false)]
                public IQueryable<Author> GetAuthors() => default!;
            }

            [QueryType]
            public static partial class GeneratedQuery
            {
                [UsePaging(InferConnectionNameFromField = false)]
                public static IQueryable<Author> GetMoreAuthors() => default!;
            }
            """;

        // act
        var schema = await GeneratorTestServer.CreateSchemaAsync(
            source,
            disableDefaultSecurity: false);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Build_Schema_Should_Reuse_Single_Connection_When_Static_And_NonStatic_Query_Page_Same_Type_Across_Assemblies()
    {
        // arrange
        // The shared Author lives in its own assembly. One assembly pages it from a
        // reflection-based (non-static) query type, the other from a source-generated
        // (static partial) query type, so both fields should resolve to one shared
        // "AuthorConnection".
        const string authorAssemblySource =
            """
            namespace Demo.MixedCrossAssembly;

            public sealed class Author
            {
                public int Id { get; set; }

                public string Name { get; set; } = string.Empty;
            }
            """;

        const string runtimeAssemblySource =
            """
            using System.Linq;
            using HotChocolate.Types;
            using Demo.MixedCrossAssembly;

            namespace Demo.MixedCrossAssembly.Runtime;

            [QueryType]
            public class RuntimeQuery
            {
                [UsePaging(InferConnectionNameFromField = false)]
                public IQueryable<Author> GetAuthors() => default!;
            }
            """;

        const string generatedAssemblySource =
            """
            using System.Linq;
            using HotChocolate.Types;
            using Demo.MixedCrossAssembly;

            namespace Demo.MixedCrossAssembly.Generated;

            [QueryType]
            public static partial class GeneratedQuery
            {
                [UsePaging(InferConnectionNameFromField = false)]
                public static IQueryable<Author> GetMoreAuthors() => default!;
            }
            """;

        var assemblies = new GeneratorAssembly[]
        {
            new(
                "Demo.MixedAssemblyAuthor",
                [authorAssemblySource],
                References: [],
                Register: false),
            new(
                "Demo.MixedAssemblyRuntime",
                [runtimeAssemblySource],
                References: ["Demo.MixedAssemblyAuthor"]),
            new(
                "Demo.MixedAssemblyGenerated",
                [generatedAssemblySource],
                References: ["Demo.MixedAssemblyAuthor"])
        };

        // act
        var schema = await GeneratorTestServer.CreateSchemaAsync(
            assemblies,
            disableDefaultSecurity: false);

        // assert
        schema.MatchSnapshot();
    }
}
