using Basic.Reference.Assemblies;
using HotChocolate.Fusion.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Fusion.Compatibility;

/// <summary>
/// Proves the two source-level compatibility promises from the design spec by compiling real
/// consumer source against the current (locally built) product assemblies with Roslyn, exactly
/// the way an external, non-friend consumer's own build would see it:
/// old gateway-named configuration source compiles with only warning-level obsolete diagnostics,
/// and new router-named fluent chains compile cleanly even under warnings-as-errors.
/// </summary>
public sealed class SourceCompatibilityTests
{
    private static readonly MetadataReference[] s_references = BuildReferences();

    [Fact]
    public void LegacyGatewayConsumerSource_Should_CompileWithOnlyObsoleteWarnings_When_CompiledAgainstCurrentAssemblies()
    {
        // arrange
        var compilation = CreateCompilation("LegacyConsumer", LegacySource, treatWarningsAsErrors: false);

        // act
        var diagnostics = compilation.GetDiagnostics(TestContext.Current.CancellationToken);
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        // CS8019 (unnecessary using) is an incidental style nitpick of the inline snippet, not a
        // compatibility signal; every other non-error diagnostic must be the obsolete warning.
        var unexpectedNonErrorIds = diagnostics
            .Where(d => d.Severity != DiagnosticSeverity.Error && d.Id is not ("CS0618" or "CS8019"))
            .Select(d => d.Id)
            .Distinct()
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        var obsoleteCount = diagnostics.Count(d => d.Id == "CS0618");

        // assert
        Assert.Empty(errors);
        Assert.Empty(unexpectedNonErrorIds);
        Assert.True(
            obsoleteCount >= 8,
            $"expected at least 8 CS0618 diagnostics from the legacy gateway call sites, got {obsoleteCount}.");
    }

    [Fact]
    public void RouterConsumerSource_Should_CompileWithoutDiagnostics_When_TreatingWarningsAsErrors()
    {
        // arrange
        var compilation = CreateCompilation("RouterConsumer", RouterSource, treatWarningsAsErrors: true);

        // act
        var diagnostics = compilation.GetDiagnostics(TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(diagnostics);
    }

    private static CSharpCompilation CreateCompilation(
        string assemblyName,
        string source,
        bool treatWarningsAsErrors)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithGeneralDiagnosticOption(treatWarningsAsErrors ? ReportDiagnostic.Error : ReportDiagnostic.Default)
            .WithNullableContextOptions(NullableContextOptions.Enable);

        return CSharpCompilation.Create(assemblyName, [syntaxTree], s_references, options);
    }

    private static MetadataReference[] BuildReferences()
    {
        // Force-load every assembly the two consumer sources below can reach, regardless of
        // test execution order, then reference the full set of loaded assemblies (this also
        // pulls in their transitive dependencies, such as HotChocolate.Language) so the
        // synthetic compilation sees exactly what a real consumer project referencing these
        // same packages would compile against.
        _ = typeof(IFusionRouterBuilder).Assembly; // HotChocolate.Fusion.Execution
        _ = typeof(FusionServerServiceCollectionExtensions).Assembly; // HotChocolate.Fusion.AspNetCore
        _ = typeof(FusionCachingRouterBuilderExtensions).Assembly; // HotChocolate.Fusion.Caching
        _ = typeof(IServiceCollection).Assembly; // Microsoft.Extensions.DependencyInjection.Abstractions
        _ = typeof(IHostApplicationBuilder).Assembly; // Microsoft.Extensions.Hosting.Abstractions

        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location));

        MetadataReference[] bcl =
        [
#if NET8_0
            .. Net80.References.All,
#elif NET9_0
            .. Net90.References.All,
#elif NET10_0
            .. Net100.References.All,
#elif NET11_0
            .. Net110.References.All,
#endif
            .. loaded
        ];

        return bcl;
    }

    private const string LegacySource =
        """
        using HotChocolate.Fusion.Caching;
        using HotChocolate.Fusion.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Hosting;

        namespace LegacyConsumer;

        public static class Consumer
        {
            // Named, default and non-default arguments across the three registration entry points.
            public static void Configure(IServiceCollection services, IHostApplicationBuilder hostBuilder)
            {
                var defaultBuilder = services.AddGraphQLGateway();
                var namedBuilder = services.AddGraphQLGateway(name: "named-schema");
                var serverBuilder = services.AddGraphQLGatewayServer(
                    disableDefaultSecurity: true,
                    name: "server-schema",
                    maxAllowedRequestSize: 2_000_000);

                // Old extension declaring-class static call (not extension-method syntax).
                var afterCacheControl = FusionCachingGatewayBuilderExtensions.AddCacheControl(serverBuilder);
                FusionCachingGatewayBuilderExtensions.UseQueryCache(afterCacheControl, after: null);

                // A custom IFusionGatewayBuilder-only implementation flowing through a
                // third-party-shaped extension written against the old interface.
                var custom = new CustomBuilder("custom-schema", services);
                var afterCustomCacheControl = FusionCachingGatewayBuilderExtensions.AddCacheControl(custom);
                ThirdParty.Configure(afterCustomCacheControl);

                // IHostApplicationBuilder registration entry point.
                hostBuilder.AddGraphQLGateway(
                    name: "host-schema",
                    maxAllowedRequestSize: 111_222,
                    disableDefaultSecurity: false);
            }

            private sealed class CustomBuilder : IFusionGatewayBuilder
            {
                public CustomBuilder(string name, IServiceCollection services)
                {
                    Name = name;
                    Services = services;
                }

                public string Name { get; }

                public IServiceCollection Services { get; }
            }
        }

        public static class ThirdParty
        {
            public static IFusionGatewayBuilder Configure(this IFusionGatewayBuilder builder) => builder;
        }
        """;

    private const string RouterSource =
        """
        using HotChocolate.Fusion.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Hosting;

        namespace RouterConsumer;

        public static class Consumer
        {
            public static void Configure(IServiceCollection services, IHostApplicationBuilder hostBuilder)
            {
                IFusionRouterBuilder core = services.AddGraphQLRouterCore();
                IFusionRouterBuilder named = services.AddGraphQLRouterCore(name: "named-schema");
                IFusionRouterBuilder server = services.AddGraphQLRouter(
                    disableDefaultSecurity: true,
                    name: "server-schema",
                    maxAllowedRequestSize: 2_000_000);

                IFusionRouterBuilder afterCacheControl = server.AddCacheControl();
                IFusionRouterBuilder afterQueryCache = afterCacheControl.UseQueryCache();

                IFusionRouterBuilder hostRouter = hostBuilder.AddGraphQLRouter(
                    name: "host-schema",
                    maxAllowedRequestSize: 111_222,
                    disableDefaultSecurity: false);
            }
        }
        """;
}
