using HotChocolate.Types.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace HotChocolate.Types;

public class QueryContextConnectionCodeFixTests
{
    [Fact]
    public async Task CodeFix_Should_UseNodeType_When_DerivedStreamPageConnectionHasQueryContextMismatch()
    {
        // arrange
        const string source = """
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                public static Task<ProductConnection> GetProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Brand> query,
                    CancellationToken cancellationToken)
                    => throw new System.InvalidOperationException();
            }

            public class Product
            {
                public int Id { get; set; }
            }

            public class Brand
            {
                public int Id { get; set; }
            }

            public class ProductConnection : StreamPageConnection<Product>
            {
                public ProductConnection(StreamPage<Product> page)
                    : base(page)
                {
                }
            }
            """;

        // act
        var fixedSource = await ApplyCodeFixAsync(source);

        // assert
        fixedSource.MatchInlineSnapshot(
            """
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                public static Task<ProductConnection> GetProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<TestNamespace.Product> query,
                    CancellationToken cancellationToken)
                    => throw new System.InvalidOperationException();
            }

            public class Product
            {
                public int Id { get; set; }
            }

            public class Brand
            {
                public int Id { get; set; }
            }

            public class ProductConnection : StreamPageConnection<Product>
            {
                public ProductConnection(StreamPage<Product> page)
                    : base(page)
                {
                }
            }
            """);
    }

    private static async Task<string> ApplyCodeFixAsync(string source)
    {
        using var workspace = new AdhocWorkspace();
        var compilation = TestHelper.CreateCompilation(source);
        var project = workspace.AddProject(
            ProjectInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Default,
                "Tests",
                "Tests",
                LanguageNames.CSharp,
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                parseOptions: CSharpParseOptions.Default,
                metadataReferences: compilation.References));
        var document = workspace.AddDocument(project.Id, "Test.cs", SourceText.From(source));
        var projectCompilation = await document.Project.GetCompilationAsync(TestContext.Current.CancellationToken);
        var diagnostic = Assert.Single(
            await projectCompilation!
                .WithAnalyzers([new QueryContextConnectionAnalyzer()])
                .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken));
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            TestContext.Current.CancellationToken);

        await new QueryContextConnectionCodeFixProvider().RegisterCodeFixesAsync(context);

        var operation = Assert.IsType<ApplyChangesOperation>(
            Assert.Single(
                await Assert.Single(actions).GetOperationsAsync(TestContext.Current.CancellationToken)));
        var fixedDocument = operation.ChangedSolution.GetDocument(document.Id)!;
        return (await fixedDocument.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
    }
}
