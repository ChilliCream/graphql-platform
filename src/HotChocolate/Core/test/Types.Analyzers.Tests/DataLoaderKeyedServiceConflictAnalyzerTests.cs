using System.Collections.Immutable;
using HotChocolate.Types.Analyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace HotChocolate.Types;

public class DataLoaderKeyedServiceConflictAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportConflict_WhenBothServiceAttributesArePresent()
    {
        // arrange
        var compilation = TestHelper.CreateCompilation(
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;
            using HotChocolate;
            using Microsoft.Extensions.DependencyInjection;

            class Query
            {
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, string>> GetAsync(
                    IReadOnlyList<int> keys,
                    [Service]
                    [FromKeyedServices("key")]
                    object service)
                    => default!;
            }
            """);

        // act
        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
                new DataLoaderKeyedServiceConflictAnalyzer()))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        // assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("HC0130", diagnostic.Id);
        Assert.Equal(
            new LinePositionSpan(new LinePosition(11, 8), new LinePosition(12, 34)),
            diagnostic.Location.GetLineSpan().Span);
    }

    [Fact]
    public async Task Analyze_Should_ReportConflict_WhenDerivedServiceAttributeAndFromKeyedServicesArePresent()
    {
        // arrange
        var compilation = TestHelper.CreateCompilation(
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;
            using HotChocolate;
            using Microsoft.Extensions.DependencyInjection;

            class MemoizedInScopeAttribute() : ServiceAttribute("memoized")
            {
            }

            class Query
            {
                [DataLoader<IBatchDataLoader<int, string>>]
                public static Task<IReadOnlyDictionary<int, string>> GetAsync(
                    IReadOnlyList<int> keys,
                    [MemoizedInScope]
                    [FromKeyedServices("key")]
                    object service)
                    => default!;
            }
            """);

        // act
        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
                new DataLoaderKeyedServiceConflictAnalyzer()))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        // assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("HC0130", diagnostic.Id);
    }

    [Fact]
    public async Task Analyze_Should_NotReportConflict_WhenOnlyOneServiceAttributeIsPresent()
    {
        // arrange
        var compilation = TestHelper.CreateCompilation(
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;
            using HotChocolate;
            using Microsoft.Extensions.DependencyInjection;

            class Query
            {
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, string>> GetWithServiceAsync(
                    IReadOnlyList<int> keys,
                    [Service("key")] object service)
                    => default!;

                [DataLoader]
                public static Task<IReadOnlyDictionary<int, string>> GetWithFromKeyedServicesAsync(
                    IReadOnlyList<int> keys,
                    [FromKeyedServices("key")] object service)
                    => default!;
            }
            """);

        // act
        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
                new DataLoaderKeyedServiceConflictAnalyzer()))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(diagnostics);
    }
}
