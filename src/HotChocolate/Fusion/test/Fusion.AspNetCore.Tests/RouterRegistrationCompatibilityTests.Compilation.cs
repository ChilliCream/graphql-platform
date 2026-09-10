using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HotChocolate.Fusion;

public partial class RouterRegistrationCompatibilityTests
{
    [Fact]
    public void Compile_Should_KeepAllRouterOverloadsWarningFree_When_ConsumerHasNoFriendAccess()
    {
        // arrange
        var compilation = CreateCompilation(
            """
            using System;
            using HotChocolate.AspNetCore;
            using HotChocolate.AspNetCore.Formatters;
            using HotChocolate.AspNetCore.Subscriptions.Protocols;
            using HotChocolate.Fusion.Configuration;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;

            public static class Consumer
            {
                public static IFusionRouterBuilder Configure<TH, TS, TF>(IServiceCollection services)
                    where TH : IHttpRequestInterceptor, new()
                    where TS : class, ISocketSessionInterceptor, new()
                    where TF : class, IHttpResponseFormatter, new()
                    => services.AddGraphQLRouter(name: "named", maxAllowedRequestSize: 512, disableDefaultSecurity: true)
                        .AddHttpRequestInterceptor<TH>()
                        .AddHttpRequestInterceptor(factory: _ => new TH())
                        .AddSocketSessionInterceptor<TS>()
                        .AddSocketSessionInterceptor(factory: _ => new TS())
                        .AddHttpResponseFormatter()
                        .AddHttpResponseFormatter(indented: true, incrementalDeliveryFormat: IncrementalDeliveryFormat.Version_0_1)
                        .AddHttpResponseFormatter(options: new HttpResponseFormatterOptions())
                        .AddHttpResponseFormatter(options: new HttpResponseFormatterOptions(), incrementalDeliveryFormat: IncrementalDeliveryFormat.Version_0_1)
                        .AddHttpResponseFormatter<TF>()
                        .AddHttpResponseFormatter(factory: _ => new TF())
                        .ModifyServerOptions(configure: o => o.EnableGetRequests = false)
                        .ModifyOptions(o => o.OperationDocumentCacheSize = 64)
                        .AddFileSystemConfiguration("gateway.far");

                public static IFusionRouterBuilder ConfigureStatically<TH, TS, TF>(IFusionRouterBuilder builder)
                    where TH : IHttpRequestInterceptor, new()
                    where TS : class, ISocketSessionInterceptor, new()
                    where TF : class, IHttpResponseFormatter, new()
                {
                    builder = AspNetCoreFusionRouterBuilderExtensions.AddHttpRequestInterceptor<TH>(builder);
                    builder = AspNetCoreFusionRouterBuilderExtensions.AddHttpRequestInterceptor(builder, factory: _ => new TH());
                    builder = AspNetCoreFusionRouterBuilderExtensions.AddSocketSessionInterceptor<TS>(builder);
                    builder = AspNetCoreFusionRouterBuilderExtensions.AddSocketSessionInterceptor(builder, factory: _ => new TS());
                    builder = AspNetCoreFusionRouterBuilderExtensions.AddHttpResponseFormatter(builder);
                    builder = AspNetCoreFusionRouterBuilderExtensions.AddHttpResponseFormatter(builder, options: new HttpResponseFormatterOptions());
                    builder = AspNetCoreFusionRouterBuilderExtensions.AddHttpResponseFormatter<TF>(builder);
                    builder = AspNetCoreFusionRouterBuilderExtensions.AddHttpResponseFormatter(builder, factory: _ => new TF());
                    return AspNetCoreFusionRouterBuilderExtensions.ModifyServerOptions(builder, configure: _ => { });
                }

                public static IFusionRouterBuilder RegisterHost(IHostApplicationBuilder builder)
                    => builder.AddGraphQLRouter();
                public static IFusionRouterBuilder RegisterHostNamed(IHostApplicationBuilder builder)
                    => builder.AddGraphQLRouter(name: "named", maxAllowedRequestSize: 512, disableDefaultSecurity: true);
                public static IFusionRouterBuilder RegisterHostStatically(IHostApplicationBuilder builder)
                    => FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLRouter(builder);
                public static IFusionRouterBuilder RegisterHostNamedStatically(IHostApplicationBuilder builder)
                    => FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLRouter(builder, name: "named", maxAllowedRequestSize: 512, disableDefaultSecurity: true);
                public static IFusionRouterBuilder RegisterServices(IServiceCollection services)
                    => services.AddGraphQLRouter();
                public static IFusionRouterBuilder RegisterServicesStatically(IServiceCollection services)
                    => FusionServerServiceCollectionExtensions.AddGraphQLRouter(services);
                public static IFusionRouterBuilder RegisterServicesNamedStatically(IServiceCollection services)
                    => FusionServerServiceCollectionExtensions.AddGraphQLRouter(services, name: "named", maxAllowedRequestSize: 512, disableDefaultSecurity: true);
            }
            """,
            warningsAsErrors: true);

        // act
        using var assembly = new MemoryStream();
        var result = compilation.Emit(assembly, cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal([], result.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning));
        Assert.True(result.Success);
    }

    [Fact]
    public void Compile_Should_KeepLegacySignaturesWithGuidance_When_ConsumerUsesStaticCalls()
    {
        // arrange
        var compilation = CreateCompilation(
            """
            using HotChocolate.Fusion.Configuration;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;

            public static class Consumer
            {
                public static IFusionGatewayBuilder Configure(IServiceCollection services)
                    => AspNetCoreFusionGatewayBuilderExtensions.ModifyServerOptions(
                        FusionServerServiceCollectionExtensions.AddGraphQLGatewayServer(
                            services, name: "named", maxAllowedRequestSize: 512, disableDefaultSecurity: true),
                        configure: _ => { });

                public static IFusionGatewayBuilder ConfigureHost(IHostApplicationBuilder builder)
                    => FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLGateway(
                        builder, name: "named", maxAllowedRequestSize: 512, disableDefaultSecurity: true);
            }
            """,
            warningsAsErrors: false);

        // act
        using var assembly = new MemoryStream();
        var result = compilation.Emit(assembly, cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.Success);
        result.Diagnostics
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .Select(d => $"{d.Severity} {d.Id}: {d.GetMessage()}")
            .ToArray()
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Extensions_Should_PreserveCompleteSignatures_When_AddingRouterOverloads()
    {
        // arrange
        var compilation = CreateCompilation("", warningsAsErrors: false);
        var legacy = compilation.GetTypeByMetadataName(
            "Microsoft.Extensions.DependencyInjection.AspNetCoreFusionGatewayBuilderExtensions")!;
        var router = compilation.GetTypeByMetadataName(
            "Microsoft.Extensions.DependencyInjection.AspNetCoreFusionRouterBuilderExtensions")!;
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
        var methods = legacy.GetMembers().OfType<IMethodSymbol>().ToArray();
        var signatures = methods.Select(m => m.ToDisplayString(format)).Order().ToArray();

        // act
        // Each legacy signature is paired with its Obsolete message so the message text is
        // verified as part of the snapshot instead of a separate per-method assertion.
        var annotatedSignatures = methods
            .Select(m =>
            {
                var obsolete = m.GetAttributes().Single(a => a.AttributeClass?.Name == "ObsoleteAttribute");
                var message = (string?)obsolete.ConstructorArguments.Single().Value;
                return (Signature: m.ToDisplayString(format), Message: message);
            })
            .OrderBy(entry => entry.Signature, StringComparer.Ordinal)
            .Select(entry => $"{entry.Signature} [Obsolete: {entry.Message}]")
            .ToArray();

        // assert
        Assert.Equal(9, methods.Length);
        Assert.Equal(
            signatures.Select(s => s
                .Replace("AspNetCoreFusionGatewayBuilderExtensions", "AspNetCoreFusionRouterBuilderExtensions", StringComparison.Ordinal)
                .Replace("IFusionGatewayBuilder", "IFusionRouterBuilder", StringComparison.Ordinal)),
            router.GetMembers().OfType<IMethodSymbol>().Select(m => m.ToDisplayString(format)).Order());
        annotatedSignatures.MatchMarkdownSnapshot();
    }

    private static CSharpCompilation CreateCompilation(string source, bool warningsAsErrors)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(System.IO.Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));

        return CSharpCompilation.Create(
            "RouterAspNetCoreConsumer",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                generalDiagnosticOption: warningsAsErrors ? ReportDiagnostic.Error : ReportDiagnostic.Default,
                nullableContextOptions: NullableContextOptions.Enable));
    }
}
