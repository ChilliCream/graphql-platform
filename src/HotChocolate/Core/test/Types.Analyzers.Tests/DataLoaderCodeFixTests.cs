using System.Collections.Immutable;
using HotChocolate.Types.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace HotChocolate.Types;

public class DataLoaderCodeFixTests
{
    [Fact]
    public async Task KeyParameterCodeFix_Should_UseContractKeyType_When_BatchAndCacheKeysAreInvalid()
    {
        // arrange
        const string batchSource = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut;

            internal static class TestClass
            {
                [DataLoader<IBatchDataLoader<int, string>>]
                internal static Task<IReadOnlyDictionary<int, string>> GetBatchAsync(
                    string keys,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """;
        const string cacheSource = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            internal static class TestClass
            {
                [DataLoader<ICacheDataLoader<int, string>>]
                internal static Task<string> GetCacheAsync(IReadOnlyList<int> key) => default!;
            }
            """;

        // act
        var fixedSources = new[]
        {
            await ApplyCodeFixAsync(
                batchSource,
                new DataLoaderKeyParameterAnalyzer(),
                new DataLoaderKeyParameterCodeFixProvider(),
                "Adjust signature to match <T>"),
            await ApplyCodeFixAsync(
                cacheSource,
                new DataLoaderKeyParameterAnalyzer(),
                new DataLoaderKeyParameterCodeFixProvider(),
                "Adjust signature to match <T>")
        };

        // assert
        fixedSources[0].MatchInlineSnapshot("""
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut;

            internal static class TestClass
            {
                [DataLoader<IBatchDataLoader<int, string>>]
                internal static Task<IReadOnlyDictionary<int, string>> GetBatchAsync(
                    global::System.Collections.Generic.IReadOnlyList<int> keys,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """);
        fixedSources[1].MatchInlineSnapshot("""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            internal static class TestClass
            {
                [DataLoader<ICacheDataLoader<int, string>>]
                internal static Task<string> GetCacheAsync(int key) => default!;
            }
            """);
    }

    [Fact]
    public async Task ReturnTypeCodeFix_Should_PreserveAsyncWrapper_When_BatchAndCacheReturnsAreInvalid()
    {
        // arrange
        const string batchSource = """
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using GreenDonut;

            internal static class TestClass
            {
                [DataLoader<IBatchDataLoader<int, string>>]
                internal static ValueTask<ILookup<int, int>> GetBatchAsync(IReadOnlyList<int> keys)
                    => default;
            }
            """;
        const string cacheSource = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            internal static class TestClass
            {
                [DataLoader<ICacheDataLoader<int, string>>]
                internal static Task<int> GetCacheAsync(int key) => default!;
            }
            """;

        // act
        var fixedSources = new[]
        {
            await ApplyCodeFixAsync(
                batchSource,
                new DataLoaderReturnTypeAnalyzer(),
                new DataLoaderReturnTypeCodeFixProvider(),
                "Adjust signature to match <T>"),
            await ApplyCodeFixAsync(
                cacheSource,
                new DataLoaderReturnTypeAnalyzer(),
                new DataLoaderReturnTypeCodeFixProvider(),
                "Adjust signature to match <T>")
        };

        // assert
        fixedSources[0].MatchInlineSnapshot("""
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using GreenDonut;

            internal static class TestClass
            {
                [DataLoader<IBatchDataLoader<int, string>>]
                internal static global::System.Threading.Tasks.ValueTask<global::System.Collections.Generic.IReadOnlyDictionary<int, string>> GetBatchAsync(IReadOnlyList<int> keys)
                    => default;
            }
            """);
        fixedSources[1].MatchInlineSnapshot("""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            internal static class TestClass
            {
                [DataLoader<ICacheDataLoader<int, string>>]
                internal static global::System.Threading.Tasks.Task<string> GetCacheAsync(int key) => default!;
            }
            """);
    }

    [Fact]
    public async Task MultipleAttributesCodeFix_Should_KeepLastGenericAttribute_When_DataLoaderAttributesAreDuplicated()
    {
        // arrange
        const string source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            internal interface F1 : IBatchDataLoader<int, string> { }
            internal interface F2 : IBatchDataLoader<int, string> { }
            internal interface F3 : IBatchDataLoader<int, string> { }

            internal static class TestClass
            {
                [DataLoader, DataLoader<F1>]
                internal static Task<IReadOnlyDictionary<int, string>> GetOldThenGenericAsync(
                    IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<F2>, DataLoader]
                internal static Task<IReadOnlyDictionary<int, string>> GetGenericThenOldAsync(
                    IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<F1>][DataLoader<F2>][DataLoader<F3>]
                internal static Task<IReadOnlyDictionary<int, string>> GetMultipleGenericAsync(
                    IReadOnlyList<int> keys)
                    => default!;
            }
            """;

        // act
        var fixedSource = await ApplyCodeFixesAsync(
            source,
            new DataLoaderMultipleAttributesAnalyzer(),
            new DataLoaderMultipleAttributesCodeFixProvider(),
            "Remove duplicate DataLoader attribute");

        // assert
        fixedSource.MatchInlineSnapshot("""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            internal interface F1 : IBatchDataLoader<int, string> { }
            internal interface F2 : IBatchDataLoader<int, string> { }
            internal interface F3 : IBatchDataLoader<int, string> { }

            internal static class TestClass
            {
                [DataLoader<F1>]
                internal static Task<IReadOnlyDictionary<int, string>> GetOldThenGenericAsync(
                    IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<F2>]
                internal static Task<IReadOnlyDictionary<int, string>> GetGenericThenOldAsync(
                    IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<F3>]
                internal static Task<IReadOnlyDictionary<int, string>> GetMultipleGenericAsync(
                    IReadOnlyList<int> keys)
                    => default!;
            }
            """);
    }

    private static async Task<string> ApplyCodeFixAsync(
        string source,
        DiagnosticAnalyzer analyzer,
        CodeFixProvider codeFixProvider,
        string title)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, source);
        var fixedDocument = await ApplyCodeFixDocumentAsync(document, analyzer, codeFixProvider, title);
        return (await fixedDocument.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
    }

    private static async Task<string> ApplyCodeFixesAsync(
        string source,
        DiagnosticAnalyzer analyzer,
        CodeFixProvider codeFixProvider,
        string title)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, source);

        while (true)
        {
            var compilation = await document.Project.GetCompilationAsync(TestContext.Current.CancellationToken);
            var diagnostics = await compilation!
                .WithAnalyzers(ImmutableArray.Create(analyzer))
                .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

            if (diagnostics.Length == 0)
            {
                return (await document.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
            }

            document = await ApplyCodeFixDocumentAsync(document, analyzer, codeFixProvider, title);
        }
    }

    private static async Task<Document> ApplyCodeFixDocumentAsync(
        Document document,
        DiagnosticAnalyzer analyzer,
        CodeFixProvider codeFixProvider,
        string title)
    {
        var compilation = await document.Project.GetCompilationAsync(TestContext.Current.CancellationToken);
        var diagnostic = (await compilation!
            .WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken)).First();
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            TestContext.Current.CancellationToken);

        await codeFixProvider.RegisterCodeFixesAsync(context);

        var action = Assert.Single(actions);
        Assert.Equal(title, action.Title);

        var operation = Assert.IsType<ApplyChangesOperation>(
            Assert.Single(await action.GetOperationsAsync(TestContext.Current.CancellationToken)));
        return operation.ChangedSolution.GetDocument(document.Id)!;
    }

    private static Document CreateDocument(AdhocWorkspace workspace, string source)
    {
        var compilation = TestHelper.CreateCompilation(source);
        var project = workspace.AddProject(ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Default,
            "Tests",
            "Tests",
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: CSharpParseOptions.Default,
            metadataReferences: compilation.References));

        return workspace.AddDocument(project.Id, "Test.cs", SourceText.From(source));
    }
}
