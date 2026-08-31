using System.Collections.Immutable;
using HotChocolate.Types.Analyzers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace HotChocolate.Types;

public class DataLoaderKeyedServiceKeyNotDeterminableAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportMetadataDerivedServiceAttribute_WhenParameterIsAService()
    {
        // arrange
        var reference = TestHelper.CreateReference(
            """
            using HotChocolate;

            public class MetadataServiceAttribute() : ServiceAttribute("metadata")
            {
            }
            """,
            "MetadataAttributes");
        var compilation = (CSharpCompilation)TestHelper.CreateCompilation(
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            class Query
            {
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, string>> GetAsync(
                    [MetadataService] IReadOnlyList<int> keys,
                    [MetadataService] object service)
                    => default!;
            }
            """).AddReferences(reference);

        // act
        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
                new DataLoaderKeyedServiceKeyNotDeterminableAnalyzer()))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        // assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("HC0133", diagnostic.Id);
        Assert.Equal(new LinePosition(9, 9), diagnostic.Location.GetLineSpan().StartLinePosition);
    }

    [Fact]
    public async Task Analyze_Should_ReportOnlyUndeterminableSourceDerivedServiceAttributes()
    {
        // arrange
        var compilation = TestHelper.CreateCompilation(
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;
            using HotChocolate;

            class ReadableAttribute() : ServiceAttribute("readable")
            {
            }

            class UndeterminableAttribute(string key) : ServiceAttribute(key + "-suffix")
            {
            }

            class Query
            {
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, string>> GetAsync(
                    IReadOnlyList<int> keys,
                    [Readable] object readable,
                    [Service("direct")] object direct,
                    [Undeterminable("key")] object undeterminable)
                    => default!;
            }
            """);

        // act
        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
                new DataLoaderKeyedServiceKeyNotDeterminableAnalyzer()))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        // assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("HC0133", diagnostic.Id);
        Assert.Equal(new LinePosition(20, 9), diagnostic.Location.GetLineSpan().StartLinePosition);
    }
}
