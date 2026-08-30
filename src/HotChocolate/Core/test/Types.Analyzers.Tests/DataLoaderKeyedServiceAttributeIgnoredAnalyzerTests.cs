using System.Collections.Immutable;
using HotChocolate.Types.Analyzers;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types;

public class DataLoaderKeyedServiceAttributeIgnoredAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportEachKeyedAttribute_WhenParameterIsNotAService()
    {
        // arrange
        var compilation = TestHelper.CreateCompilation(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut;
            using HotChocolate;
            using Microsoft.Extensions.DependencyInjection;

            class MemoizedInScopeAttribute() : ServiceAttribute("state")
            {
            }

            class Query
            {
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, string>> GetAsync(
                    [Service("key")] IReadOnlyList<int> keys,
                    [FromKeyedServices("cancellation-token")] CancellationToken cancellationToken,
                    [MemoizedInScope][DataLoaderState("state")] string state,
                    [Service("service")] object service)
                    => default!;
            }
            """);

        // act
        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
                new DataLoaderKeyedServiceAttributeIgnoredAnalyzer()))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(["HC0131", "HC0131", "HC0131"], diagnostics.Select(t => t.Id));
    }

    [Fact]
    public async Task Analyze_Should_ReportOnlyKeyedAttributes_WhenKeyedAndKeylessServiceAttributesArePresent()
    {
        // arrange
        var compilation = TestHelper.CreateCompilation(
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;
            using HotChocolate;

            class KeyedAttribute() : ServiceAttribute("key") { }
            class KeylessAttribute : ServiceAttribute { }

            class Query
            {
                [DataLoader] public static Task<IReadOnlyDictionary<int, string>> GetKeyedThenKeylessAsync([Keyed][Keyless] IReadOnlyList<int> keys) => default!;
                [DataLoader] public static Task<IReadOnlyDictionary<int, string>> GetKeylessThenKeyedAsync([Keyless][Keyed] IReadOnlyList<int> keys) => default!;
            }
            """);

        // act
        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(
                new DataLoaderKeyedServiceAttributeIgnoredAnalyzer()))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(["HC0131", "HC0131"], diagnostics.Select(t => t.Id));
    }
}
