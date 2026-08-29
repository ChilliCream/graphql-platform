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
        var fixedSource = await ApplyFixAllAsync(
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
                [ DataLoader<F1>]
                internal static Task<IReadOnlyDictionary<int, string>> GetOldThenGenericAsync(
                    IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<F2> ]
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

    [Fact]
    public async Task MultipleAttributesCodeFix_Should_PreserveCommentsAndUnrelatedAttributes_When_MixedListsContainDuplicates()
    {
        // arrange
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            internal interface F1 : IBatchDataLoader<int, string> { }
            internal interface F2 : IBatchDataLoader<int, string> { }

            internal static class TestClass
            {
                [method: Obsolete]
                [DataLoader<F1>, /* first generic */ DataLoader, /* old attribute */ DataLoader<F2>]
                internal static Task<IReadOnlyDictionary<int, string>> GetAsync(IReadOnlyList<int> keys)
                    => default!;
            }
            """;

        // act
        var fixedSource = await ApplyCodeFixAsync(
            source,
            new DataLoaderMultipleAttributesAnalyzer(),
            new DataLoaderMultipleAttributesCodeFixProvider(),
            "Remove duplicate DataLoader attribute");

        // assert
        fixedSource.MatchInlineSnapshot("""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            internal interface F1 : IBatchDataLoader<int, string> { }
            internal interface F2 : IBatchDataLoader<int, string> { }

            internal static class TestClass
            {
                [method: Obsolete]
                [ /* first generic */  /* old attribute */ DataLoader<F2>]
                internal static Task<IReadOnlyDictionary<int, string>> GetAsync(IReadOnlyList<int> keys)
                    => default!;
            }
            """);
    }

    [Fact]
    public async Task KeyedServiceAttributeIgnoredCodeFix_Should_RemoveOnlyTheKeyedAttribute()
    {
        // arrange
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;
            using HotChocolate;

            internal static class TestClass
            {
                [DataLoader]
                internal static Task<IReadOnlyDictionary<int, string>> GetAsync(
                    [Service("key")][Obsolete]
                    IReadOnlyList<int> keys)
                    => default!;
            }
            """;

        // act
        var fixedSource = await ApplyCodeFixAsync(
            source,
            new DataLoaderKeyedServiceAttributeIgnoredAnalyzer(),
            new DataLoaderKeyedServiceAttributeIgnoredCodeFixProvider(),
            "Remove keyed service attribute");

        // assert
        fixedSource.MatchInlineSnapshot("""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;
            using HotChocolate;

            internal static class TestClass
            {
                [DataLoader]
                internal static Task<IReadOnlyDictionary<int, string>> GetAsync(
                    [Obsolete]
                    IReadOnlyList<int> keys)
                    => default!;
            }
            """);
    }

    [Fact]
    public async Task KeyedServiceAttributeIgnoredCodeFix_Should_RemoveOnlyKeyedAttributes_WhenKeyedAndKeylessServiceAttributesArePresent()
    {
        // arrange
        const string source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;
            using HotChocolate;

            class KeyedAttribute() : ServiceAttribute("key") { }
            class KeylessAttribute : ServiceAttribute { }

            internal static class TestClass
            {
                [DataLoader] public static Task<IReadOnlyDictionary<int, string>> GetKeyedThenKeylessAsync([Keyed][Keyless] IReadOnlyList<int> keys) => default!;
                [DataLoader] public static Task<IReadOnlyDictionary<int, string>> GetKeylessThenKeyedAsync([Keyless][Keyed] IReadOnlyList<int> keys) => default!;
            }
            """;

        // act
        var fixedSource = await ApplyFixAllAsync(
            source,
            new DataLoaderKeyedServiceAttributeIgnoredAnalyzer(),
            new DataLoaderKeyedServiceAttributeIgnoredCodeFixProvider(),
            "Remove keyed service attribute");

        // assert
        fixedSource.MatchInlineSnapshot("""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;
            using HotChocolate;

            class KeyedAttribute() : ServiceAttribute("key") { }
            class KeylessAttribute : ServiceAttribute { }

            internal static class TestClass
            {
                [DataLoader] public static Task<IReadOnlyDictionary<int, string>> GetKeyedThenKeylessAsync([Keyless] IReadOnlyList<int> keys) => default!;
                [DataLoader] public static Task<IReadOnlyDictionary<int, string>> GetKeylessThenKeyedAsync([Keyless] IReadOnlyList<int> keys) => default!;
            }
            """);
    }

    [Fact]
    public async Task KeyedServiceAttributeIgnoredCodeFix_Should_PreserveCommentAndConditionalTrivia_When_AttributeListIsStandalone()
    {
        // arrange
        const string source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;
            using HotChocolate;

            internal static class TestClass
            {
                [DataLoader]
                internal static Task<IReadOnlyDictionary<int, string>> GetAsync(
            // The keyed service is ignored.
            #if DEBUG
            #endif
            [Service("key")]
                    IReadOnlyList<int> keys)
                    => default!;
            }
            """;

        // act
        var fixedSource = await ApplyCodeFixAsync(
            source,
            new DataLoaderKeyedServiceAttributeIgnoredAnalyzer(),
            new DataLoaderKeyedServiceAttributeIgnoredCodeFixProvider(),
            "Remove keyed service attribute");

        // assert
        fixedSource.MatchInlineSnapshot("""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;
            using HotChocolate;

            internal static class TestClass
            {
                [DataLoader]
                internal static Task<IReadOnlyDictionary<int, string>> GetAsync(
            // The keyed service is ignored.
            #if DEBUG
            #endif

                    IReadOnlyList<int> keys)
                    => default!;
            }
            """);
    }

    [Fact]
    public async Task KeyedServiceAttributeIgnoredCodeFix_Should_PreserveSurroundingComments_When_AttributeListContainsUnrelatedAttribute()
    {
        // arrange
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;
            using HotChocolate;

            internal static class TestClass
            {
                [DataLoader]
                internal static Task<IReadOnlyDictionary<int, string>> GetAsync(
                    [Obsolete, /* before keyed service */ Service("key") /* after keyed service */]
                    IReadOnlyList<int> keys)
                    => default!;
            }
            """;

        // act
        var fixedSource = await ApplyCodeFixAsync(
            source,
            new DataLoaderKeyedServiceAttributeIgnoredAnalyzer(),
            new DataLoaderKeyedServiceAttributeIgnoredCodeFixProvider(),
            "Remove keyed service attribute");

        // assert
        fixedSource.MatchInlineSnapshot("""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;
            using HotChocolate;

            internal static class TestClass
            {
                [DataLoader]
                internal static Task<IReadOnlyDictionary<int, string>> GetAsync(
                    [Obsolete /* before keyed service */  /* after keyed service */]
                    IReadOnlyList<int> keys)
                    => default!;
            }
            """);
    }

    [Fact]
    public async Task KeyedServiceOnConstructorParameterCodeFix_Should_ReplaceDirectServiceAndPreserveTrivia()
    {
        // arrange
        const string source = """
            using GreenDonut;
            using HotChocolate;
            using Microsoft.Extensions.DependencyInjection;

            class Loader : IDataLoader
            {
                public Loader(
            // The keyed service is required by this loader.
            #if DEBUG
            #endif
                    [Service(/* key */ "key")]
                    object service)
                {
                }
            }
            """;

        // act
        var fixedSource = await ApplyCodeFixAsync(
            source,
            new DataLoaderKeyedServiceOnConstructorParameterAnalyzer(),
            new DataLoaderKeyedServiceOnConstructorParameterCodeFixProvider(),
            "Use FromKeyedServices");

        // assert
        fixedSource.MatchInlineSnapshot("""
            using GreenDonut;
            using HotChocolate;
            using Microsoft.Extensions.DependencyInjection;

            class Loader : IDataLoader
            {
                public Loader(
            // The keyed service is required by this loader.
            #if DEBUG
            #endif
                    [FromKeyedServices(/* key */ "key")]
                    object service)
                {
                }
            }
            """);
    }

    [Fact]
    public async Task KeyedServiceOnConstructorParameterCodeFix_Should_NotRegisterForDerivedAttribute()
    {
        // arrange
        const string source = """
            using GreenDonut;
            using HotChocolate;

            class MemoizedInScopeAttribute() : ServiceAttribute("key")
            {
            }

            class Loader : IDataLoader
            {
                public Loader([MemoizedInScope] object service)
                {
                }
            }
            """;

        // act
        var actions = await GetCodeFixesAsync(
            source,
            new DataLoaderKeyedServiceOnConstructorParameterAnalyzer(),
            new DataLoaderKeyedServiceOnConstructorParameterCodeFixProvider());

        // assert
        Assert.Empty(actions);
    }

    [Fact]
    public async Task SignatureCodeFixes_Should_UseLastContract_When_GenericAttributesConflict()
    {
        // arrange
        const string keyParameterSource = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            internal interface F1 : IBatchDataLoader<int, string> { }
            internal interface F2 : ICacheDataLoader<Guid, decimal> { }

            internal static class TestClass
            {
                [DataLoader<F1>][DataLoader<F2>]
                internal static Task<decimal> GetAsync(string key) => default!;
            }
            """;
        const string returnTypeSource = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            internal interface F1 : IBatchDataLoader<int, string> { }
            internal interface F2 : ICacheDataLoader<Guid, decimal> { }

            internal static class TestClass
            {
                [DataLoader<F1>][DataLoader<F2>]
                internal static ValueTask<int> GetAsync(Guid key) => default;
            }
            """;

        // act
        var fixedSources = new[]
        {
            await ApplyCodeFixAsync(
                keyParameterSource,
                new DataLoaderKeyParameterAnalyzer(),
                new DataLoaderKeyParameterCodeFixProvider(),
                "Adjust signature to match <T>"),
            await ApplyCodeFixAsync(
                returnTypeSource,
                new DataLoaderReturnTypeAnalyzer(),
                new DataLoaderReturnTypeCodeFixProvider(),
                "Adjust signature to match <T>")
        };

        // assert
        fixedSources[0].MatchInlineSnapshot("""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            internal interface F1 : IBatchDataLoader<int, string> { }
            internal interface F2 : ICacheDataLoader<Guid, decimal> { }

            internal static class TestClass
            {
                [DataLoader<F1>][DataLoader<F2>]
                internal static Task<decimal> GetAsync(global::System.Guid key) => default!;
            }
            """);
        fixedSources[1].MatchInlineSnapshot("""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            internal interface F1 : IBatchDataLoader<int, string> { }
            internal interface F2 : ICacheDataLoader<Guid, decimal> { }

            internal static class TestClass
            {
                [DataLoader<F1>][DataLoader<F2>]
                internal static global::System.Threading.Tasks.ValueTask<decimal> GetAsync(Guid key) => default;
            }
            """);
    }

    [Fact]
    public async Task SignatureCodeFixes_Should_NotRegisterAction_When_LastGenericAttributeIsUnresolved()
    {
        // arrange
        const string keyParameterSource = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            internal interface F1 : IBatchDataLoader<int, string> { }

            internal static class TestClass
            {
                [DataLoader<F1>][DataLoader<Missing>]
                internal static Task<IReadOnlyDictionary<int, string>> GetAsync(string key) => default!;
            }
            """;
        const string returnTypeSource = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            internal interface F1 : IBatchDataLoader<int, string> { }

            internal static class TestClass
            {
                [DataLoader<F1>][DataLoader<Missing>]
                internal static Task<int> GetAsync(IReadOnlyList<int> keys) => default!;
            }
            """;

        // act
        var keyParameterActions = await GetCodeFixesAsync(
            keyParameterSource,
            new DataLoaderKeyParameterAnalyzer(),
            new DataLoaderKeyParameterCodeFixProvider());
        var returnTypeActions = await GetCodeFixesAsync(
            returnTypeSource,
            new DataLoaderReturnTypeAnalyzer(),
            new DataLoaderReturnTypeCodeFixProvider());

        // assert
        Assert.Empty(keyParameterActions);
        Assert.Empty(returnTypeActions);
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

    private static async Task<string> ApplyFixAllAsync(
        string source,
        DiagnosticAnalyzer analyzer,
        CodeFixProvider codeFixProvider,
        string title)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, source);
        var compilation = await document.Project.GetCompilationAsync(TestContext.Current.CancellationToken);
        var diagnostics = await compilation!
            .WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
        var context = new FixAllContext(
            document,
            codeFixProvider,
            FixAllScope.Document,
            title,
            codeFixProvider.FixableDiagnosticIds,
            new TestDiagnosticProvider(diagnostics),
            TestContext.Current.CancellationToken);
        var fixAllProvider = codeFixProvider.GetFixAllProvider()
            ?? throw new InvalidOperationException("The code fix provider does not support Fix All.");
        var action = await fixAllProvider.GetFixAsync(context);
        var operation = Assert.IsType<ApplyChangesOperation>(
            Assert.Single(await action!.GetOperationsAsync(TestContext.Current.CancellationToken)));
        var fixedDocument = operation.ChangedSolution.GetDocument(document.Id)!;
        var fixedCompilation = await fixedDocument.Project.GetCompilationAsync(TestContext.Current.CancellationToken);
        var residualDiagnostics = await fixedCompilation!
            .WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        Assert.Empty(residualDiagnostics);
        return (await fixedDocument.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
    }

    private static async Task<IReadOnlyList<CodeAction>> GetCodeFixesAsync(
        string source,
        DiagnosticAnalyzer analyzer,
        CodeFixProvider codeFixProvider)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, source);
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
        return actions;
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

    private sealed class TestDiagnosticProvider(ImmutableArray<Diagnostic> diagnostics)
        : FixAllContext.DiagnosticProvider
    {
        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(
            Document document,
            CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(
            Project project,
            CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<Diagnostic>>(ImmutableArray<Diagnostic>.Empty);

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(
            Project project,
            CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);
    }
}
