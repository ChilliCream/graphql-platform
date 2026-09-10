using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Configuration;

public class RouterBuilderCompatibilityTests : FusionTestBase
{
    [Fact]
    public void Compile_Should_KeepRouterChainsWarningFree_When_UsingCoreExtensions()
    {
        var compilation = CreateCompilation(
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate.Execution;
            using HotChocolate.Fusion.Configuration;
            using HotChocolate.Fusion.Diagnostics;
            using HotChocolate.Fusion.Execution;
            using HotChocolate.Fusion.Execution.Clients;
            using HotChocolate.Language;
            using HotChocolate.Validation;
            using Microsoft.Extensions.DependencyInjection;

            public static class Consumer
            {
                public static IFusionRouterBuilder Configure<TParser, TFilter, TListener, TWarmup, TVisitor, TRule>(
                    IServiceCollection services,
                    IFusionConfigurationProvider provider,
                    HttpSourceSchemaClientConfiguration client,
                    TWarmup warmup)
                    where TParser : class, INodeIdParser
                    where TFilter : class, IErrorFilter, new()
                    where TListener : class, new()
                    where TWarmup : class, IRequestExecutorWarmupTask, new()
                    where TVisitor : DocumentValidatorVisitor, new()
                    where TRule : class, IDocumentValidatorRule, new()
                    => services.AddGraphQLRouterCore(name: "named")
                        .ConfigureSchemaFeatures((_, _) => { })
                        .ConfigureSchemaServices((_, _) => { })
                        .AddConfigurationProvider(_ => provider)
                        .AddFileSystemConfiguration(fileName: "gateway.far")
                        .AddInMemoryConfiguration(Utf8GraphQLParser.Parse("type Query { field: String }"))
                        .AddOperationPlannerInterceptor(_ => null!)
                        .ModifyOptions(o => o.OperationDocumentCacheSize = 64)
                        .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                        .ModifyPlannerOptions(_ => { })
                        .ModifyParserOptions(o => o.MaxAllowedTokens = 100)
                        .AddApplicationService<object>()
                        .AddNodeIdParser<TParser>()
                        .AddMD5DocumentHashProvider()
                        .AddSha1DocumentHashProvider()
                        .AddSha256DocumentHashProvider()
                        .AddErrorFilter(error => error)
                        .AddErrorFilter<TFilter>()
                        .AddErrorFilter(_ => new TFilter())
                        .AddDiagnosticEventListener<TListener>()
                        .AddDiagnosticEventListener(_ => new TListener())
                        .AddWarmupTask((_, _) => Task.CompletedTask)
                        .AddWarmupTask(warmup)
                        .AddWarmupTask<TWarmup>()
                        .AddWarmupTask(_ => new TWarmup())
                        .AddValidationVisitor<TVisitor>()
                        .AddValidationVisitor((_, _) => new TVisitor(), isCacheable: false)
                        .AddValidationRule<TRule>()
                        .AddValidationRule((_, _) => new TRule())
                        .AddMaxExecutionDepthRule(10)
                        .DisableIntrospection()
                        .DisableIntrospection((_, _) => false)
                        .SetMaxAllowedValidationErrors(10)
                        .SetMaxAllowedLocationsPerValidationError(2)
                        .SetIntrospectionAllowedDepth(3, 4)
                        .SetMaxAllowedFieldMergeComparisons(100)
                        .AddMaxAllowedFieldCycleDepthRule()
                        .RemoveMaxAllowedFieldCycleDepthRule()
                        .ConfigureValidation((_, _) => { })
                        .AddHttpClientConfiguration("a", new Uri("http://localhost/a"))
                        .AddHttpClientConfiguration("b", "client", new Uri("http://localhost/b"))
                        .AddHttpClientConfiguration(client)
                        .AddHttpClientConfiguration(_ => client)
                        .UseRequest(next => next, key: "one")
                        .UseRequest((_, next) => next, key: "two", before: "one")
                        .UseRequest(new RequestMiddlewareConfiguration((_, next) => next, "three"), after: "one")
                        .UseDocumentCache()
                        .UseDocumentParser()
                        .UseDocumentValidation()
                        .UseExceptions()
                        .UseTimeout()
                        .UseInstrumentation()
                        .UseOperationPlanCache()
                        .UseOperationPlan()
                        .UseConcurrencyGate()
                        .UseOperationExecution()
                        .UseOperationVariableCoercion()
                        .UseSkipWarmupExecution()
                        .UseReadPersistedOperation()
                        .UseAutomaticPersistedOperationNotFound()
                        .UseWritePersistedOperation()
                        .UsePersistedOperationNotFound()
                        .UseOnlyPersistedOperationAllowed()
                        .UseDefaultPipeline()
                        .UsePersistedOperationPipeline()
                        .UseAutomaticPersistedOperationPipeline();

                public static ValueTask<IRequestExecutor> Build(IFusionRouterBuilder builder)
                    => builder.BuildRequestExecutorAsync(schemaName: "named", cancellationToken: CancellationToken.None);

                public static IFusionRouterBuilder ConfigureStatically(IFusionRouterBuilder builder)
                    => CoreFusionRouterBuilderExtensions.ModifyOptions(builder, _ => { });
            }
            """);

        using var assembly = new MemoryStream();
        var result = compilation.Emit(assembly, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal([], result.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning));
        Assert.True(result.Success);
    }

    [Fact]
    public void Compile_Should_ReportWarningLevelGuidance_When_UsingLegacyEntryPoints()
    {
        var compilation = CreateCompilation(
            """
            using HotChocolate.Fusion.Configuration;
            using Microsoft.Extensions.DependencyInjection;

            public static class Consumer
            {
                public static IFusionGatewayBuilder Configure(IServiceCollection services)
                    => CoreFusionGatewayBuilderExtensions.ModifyOptions(
                        services.AddGraphQLGateway(name: "named"), _ => { });
            }
            """);

        using var assembly = new MemoryStream();
        var result = compilation.Emit(assembly, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        result.Diagnostics
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .Select(d => $"{d.Severity} {d.Id}: {d.GetMessage()}")
            .ToArray()
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void CoreExtensions_Should_PreserveSignatures_When_AddingRouterOverloads()
    {
        var compilation = CreateCompilation("");
        var legacy = compilation.GetTypeByMetadataName(
            "Microsoft.Extensions.DependencyInjection.CoreFusionGatewayBuilderExtensions")!;
        var router = compilation.GetTypeByMetadataName(
            "Microsoft.Extensions.DependencyInjection.CoreFusionRouterBuilderExtensions")!;
        var format = SymbolDisplayFormat.CSharpErrorMessageFormat
            .WithGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeParameters
                | SymbolDisplayGenericsOptions.IncludeTypeConstraints)
            .WithMemberOptions(SymbolDisplayMemberOptions.IncludeAccessibility
                | SymbolDisplayMemberOptions.IncludeModifiers
                | SymbolDisplayMemberOptions.IncludeType
                | SymbolDisplayMemberOptions.IncludeContainingType
                | SymbolDisplayMemberOptions.IncludeParameters)
            .WithParameterOptions(SymbolDisplayParameterOptions.IncludeExtensionThis
                | SymbolDisplayParameterOptions.IncludeType
                | SymbolDisplayParameterOptions.IncludeName
                | SymbolDisplayParameterOptions.IncludeDefaultValue)
            .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        var legacyMethods = legacy.GetMembers().OfType<IMethodSymbol>().ToArray();
        var routerMethods = router.GetMembers().OfType<IMethodSymbol>().ToArray();
        var signatures = legacyMethods.Select(m => m.ToDisplayString(format)).Order().ToArray();

        Assert.Equal(
            signatures.Select(s => s
                .Replace("CoreFusionGatewayBuilderExtensions", "CoreFusionRouterBuilderExtensions", StringComparison.Ordinal)
                .Replace("IFusionGatewayBuilder", "IFusionRouterBuilder", StringComparison.Ordinal)),
            routerMethods.Select(m => m.ToDisplayString(format)).Order());
        Assert.All(legacyMethods, method =>
        {
            var obsolete = Assert.Single(method.GetAttributes(), a => a.AttributeClass?.Name == "ObsoleteAttribute");
            Assert.Equal($"Use {method.Name} on IFusionRouterBuilder instead.", obsolete.ConstructorArguments[0].Value);
            Assert.Single(obsolete.ConstructorArguments);
        });
        Assert.All(routerMethods, method => Assert.Empty(method.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "ObsoleteAttribute")));
        signatures.MatchMarkdownSnapshot();
    }

    [Fact]
    public void BuildHelpers_Should_RemainInternal_When_InspectingTheRegistrationSurface()
    {
        var methods = typeof(HotChocolateFusionServiceCollectionExtensions)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.Name is "BuildRouterAsync" or "BuildGatewayAsync")
            .OrderBy(m => m.Name)
            .Select(m => new
            {
                m.Name,
                m.IsAssembly,
                Obsolete = m.GetCustomAttribute<ObsoleteAttribute>()?.Message
            })
            .ToArray();

        methods.MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Configure_Should_PreserveOrderAndSchemaIsolation_When_MixingBuilderSurfaces(bool customLegacyBuilder)
    {
        var services = new ServiceCollection();
        var callbacks = new List<string>();
        var warmups = new List<string>();
        services.AddHttpClient();
        services.AddSingleton(new ApplicationMarker("application"));
        var schema = ComposeSchemaDocument("type Query { field: String }");
        var router = services.AddGraphQLRouterCore("one");

        IFusionRouterBuilder chain = router
            .AddInMemoryConfiguration(schema)
            .ModifyOptions(o =>
            {
                callbacks.Add("router");
                o.OperationDocumentCacheSize = 64;
            })
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ModifyParserOptions(o => o.MaxAllowedTokens = 100)
            .AddSha256DocumentHashProvider()
            .AddApplicationService<ApplicationMarker>()
            .ConfigureSchemaServices((sp, sc) => sc.AddSingleton(new SchemaMarker(sp.GetRequiredService<ApplicationMarker>().Value)))
            .ConfigureSchemaFeatures((_, features) => features.Set(new FeatureMarker("router")))
            .AddWarmupTask((executor, _) =>
            {
                warmups.Add($"{executor.Schema.Name}:router");
                return Task.CompletedTask;
            })
            .UseDefaultPipeline();
        Assert.Same(router, chain);

#pragma warning disable CS0618 // Intentional old signatures and a third-party legacy-only builder.
        IFusionGatewayBuilder legacy = customLegacyBuilder
            ? new LegacyOnlyBuilder(router.Name, services)
            : router;
        Assert.Equal(!customLegacyBuilder, legacy is IFusionRouterBuilder);
        Assert.Same(legacy, CoreFusionGatewayBuilderExtensions.ModifyOptions(legacy, o =>
        {
            callbacks.Add("legacy");
            o.OperationDocumentCacheSize *= 2;
        }));
        Assert.Same(legacy, legacy
            .ConfigureSchemaFeatures((_, features) => features.Set(new FeatureMarker("legacy")))
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton(new SchemaMarker("legacy")))
            .AddWarmupTask((executor, _) =>
            {
                warmups.Add($"{executor.Schema.Name}:legacy");
                return Task.CompletedTask;
            }));

        // A third-party extension can return the base interface and continue through legacy methods.
        Assert.Same(router, router.AddLegacyConfiguration(callbacks).ModifyOptions(o =>
        {
            callbacks.Add("legacy-continuation");
            o.OperationDocumentCacheSize += 16;
        }));

        services.AddGraphQLGateway("two")
            .AddInMemoryConfiguration(schema)
            .ModifyOptions(o => o.OperationDocumentCacheSize = 32)
            .AddWarmupTask((executor, _) =>
            {
                warmups.Add($"{executor.Schema.Name}:legacy");
                return Task.CompletedTask;
            });
#pragma warning restore CS0618

        await using var provider = services.BuildServiceProvider();
        var executorProvider = provider.GetRequiredService<IRequestExecutorProvider>();
        var one = await executorProvider.GetExecutorAsync("one", TestContext.Current.CancellationToken);
        var two = await executorProvider.GetExecutorAsync("two", TestContext.Current.CancellationToken);
        var setup = provider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>();

        new
        {
            executorProvider.SchemaNames,
            One = new
            {
                one.Schema.Name,
                CacheSize = one.Schema.Features.GetRequired<FusionOptions>().OperationDocumentCacheSize,
                Feature = one.Schema.Features.GetRequired<FeatureMarker>().Value,
                ParserTokens = one.Schema.Features.GetRequired<ParserOptions>().MaxAllowedTokens,
                one.Schema.Features.GetRequired<FusionRequestOptions>().IncludeExceptionDetails,
                Services = one.Schema.Services.GetServices<SchemaMarker>().Select(m => m.Value).ToArray(),
                ApplicationService = ReferenceEquals(
                    provider.GetRequiredService<ApplicationMarker>(),
                    one.Schema.Services.GetRequiredService<ApplicationMarker>()),
                OptionsCallbacks = setup.Get("one").OptionsModifiers.Count
            },
            Two = new
            {
                two.Schema.Name,
                CacheSize = two.Schema.Features.GetRequired<FusionOptions>().OperationDocumentCacheSize,
                OptionsCallbacks = setup.Get("two").OptionsModifiers.Count
            },
            Callbacks = callbacks,
            Warmups = warmups
        }.MatchMarkdownSnapshot();

        await using var result = await one.ExecuteAsync(
            "{ __typename }", TestContext.Current.CancellationToken);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__typename": "Query"
              }
            }
            """);
    }

    [Fact]
    public void Configure_Should_UseOneNamedOptionsPipeline_When_RegisteringThroughBothEntryPoints()
    {
        var services = new ServiceCollection();
        var router = services.AddGraphQLRouterCore().ModifyOptions(o => o.OperationDocumentCacheSize = 64);
#pragma warning disable CS0618 // Intentional mixed registration.
        var legacy = services.AddGraphQLGateway().ModifyOptions(o => o.OperationDocumentCacheSize *= 2);
#pragma warning restore CS0618
        Assert.Same(services, router.Services);
        Assert.Same(services, legacy.Services);

        using var provider = services.BuildServiceProvider();
        var setup = provider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>().Get(router.Name);
        var pipeline = new List<RequestMiddlewareConfiguration>();
        foreach (var configure in setup.PipelineModifiers)
        {
            configure(pipeline);
        }

        new
        {
            router.Name,
            LegacyName = legacy.Name,
            SchemaNames = provider.GetServices<SchemaName>().Select(n => n.Value).ToArray(),
            ManagerCount = services.Count(s => s.ServiceType == typeof(FusionRequestExecutorManager)),
            OptionsCallbacks = setup.OptionsModifiers.Count,
            CacheSize = FusionRequestExecutorManager.CreateOptions(setup).OperationDocumentCacheSize,
            Pipeline = pipeline.Select(m => m.Key).ToArray()
        }.MatchMarkdownSnapshot();
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        // A distinct assembly name deliberately has no InternalsVisibleTo access.
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(System.IO.Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));

        return CSharpCompilation.Create(
            "RouterCoreConsumer",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

#pragma warning disable CS0618 // This builder deliberately implements only the published legacy contract.
    private sealed class LegacyOnlyBuilder(string name, IServiceCollection services) : IFusionGatewayBuilder
    {
        public string Name { get; } = name;

        public IServiceCollection Services { get; } = services;
    }
#pragma warning restore CS0618

    private sealed record ApplicationMarker(string Value);

    private sealed record SchemaMarker(string Value);

    private sealed record FeatureMarker(string Value);
}

#pragma warning disable CS0618 // Models an existing third-party extension library.
internal static class LegacyRouterTestExtensions
{
    public static IFusionGatewayBuilder AddLegacyConfiguration(
        this IFusionGatewayBuilder builder,
        List<string> callbacks)
        => builder.ModifyOptions(_ => callbacks.Add("third-party"));
}
#pragma warning restore CS0618
