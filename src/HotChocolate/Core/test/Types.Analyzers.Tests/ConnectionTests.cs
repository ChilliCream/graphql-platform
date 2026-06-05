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
}
