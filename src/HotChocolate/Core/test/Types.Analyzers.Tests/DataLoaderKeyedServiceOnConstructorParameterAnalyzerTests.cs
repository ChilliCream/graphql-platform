using System.Collections.Immutable;
using HotChocolate.Types.Analyzers;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types;

public class DataLoaderKeyedServiceOnConstructorParameterAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportDirectAndSourceDerivedKeyedServices_WhenOnDataLoaderConstructorParameters()
    {
        // arrange
        var compilation = TestHelper.CreateCompilation(
            """
            using GreenDonut;
            using HotChocolate;

            class MemoizedInScopeAttribute() : ServiceAttribute("derived-key")
            {
            }

            class Loader : IDataLoader
            {
                public Loader([Service("direct-key")] object direct, [MemoizedInScope] object derived)
                {
                }
            }
            """);

        // act
        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
                new DataLoaderKeyedServiceOnConstructorParameterAnalyzer()))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(["HC0132", "HC0132"], diagnostics.Select(t => t.Id));
    }

    [Fact]
    public async Task Analyze_Should_ReportDirectEnumKeyedService_WhenOnDataLoaderConstructorParameter()
    {
        // arrange
        var compilation = TestHelper.CreateCompilation(
            """
            using GreenDonut;
            using HotChocolate;

            enum ServiceKey
            {
                First
            }

            class Loader : IDataLoader
            {
                public Loader([Service(ServiceKey.First)] object service)
                {
                }
            }
            """);

        // act
        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
                new DataLoaderKeyedServiceOnConstructorParameterAnalyzer()))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(["HC0132"], diagnostics.Select(t => t.Id));
    }

    [Fact]
    public async Task Analyze_Should_NotReportKeylessAndDependencyInjectionAttributes_WhenOnConstructorParameters()
    {
        // arrange
        var compilation = TestHelper.CreateCompilation(
            """
            using GreenDonut;
            using HotChocolate;
            using Microsoft.Extensions.DependencyInjection;

            class Loader : IDataLoader
            {
                public Loader([Service] object keyless, [FromKeyedServices("key")] object keyed)
                {
                }
            }

            class NotALoader
            {
                public NotALoader([Service("key")] object service)
                {
                }
            }
            """);

        // act
        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
                new DataLoaderKeyedServiceOnConstructorParameterAnalyzer()))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(diagnostics);
    }
}
