using System.Reflection;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Fusion.Compatibility;

/// <summary>
/// Records, from a non-friend consumer, the covered legacy/router compatibility surface and the
/// approved direct breaks excluded from that promise. See README.md for the compatibility
/// promise this proves and the exclusions it verifies against the current assemblies.
/// </summary>
public sealed class CompatibilitySurfaceTests
{
    [Fact]
    public void CompatibilitySurface_Should_MatchRecordedComparison_When_ReflectedFromNonFriendAssembly()
    {
        // arrange
#pragma warning disable CS0618 // Reflecting over the obsolete legacy family types themselves.
        var families = new (string Name, Type Legacy, Type Router)[]
        {
            ("Core", typeof(CoreFusionGatewayBuilderExtensions), typeof(CoreFusionRouterBuilderExtensions)),
            ("Caching", typeof(FusionCachingGatewayBuilderExtensions), typeof(FusionCachingRouterBuilderExtensions)),
            ("Diagnostics", typeof(DiagnosticsFusionGatewayBuilderExtensions), typeof(DiagnosticsFusionRouterBuilderExtensions)),
            ("InMemory", typeof(InMemoryFusionGatewayBuilderExtensions), typeof(InMemoryFusionRouterBuilderExtensions)),
            ("AspNetCore", typeof(AspNetCoreFusionGatewayBuilderExtensions), typeof(AspNetCoreFusionRouterBuilderExtensions))
        };
#pragma warning restore CS0618

        // act
        var coveredFamilies = families.Select(DescribeFamily).ToArray();
        var coveredEntryPoints = new[]
        {
            DescribeEntryPoint(
                typeof(HotChocolateFusionServiceCollectionExtensions),
                "AddGraphQLGateway",
                typeof(IServiceCollection)),
            DescribeEntryPoint(
                typeof(FusionServerServiceCollectionExtensions),
                "AddGraphQLGatewayServer",
                typeof(IServiceCollection)),
            DescribeEntryPoint(
                typeof(FusionServerAspNetCoreHostingBuilderExtensions),
                "AddGraphQLGateway",
                typeof(IHostApplicationBuilder))
        };

        var packagingAssembly = typeof(RouterConfiguration).Assembly;
        var configurationAssembly = typeof(IFusionRouterBuilder).Assembly;
        var buildMethods = typeof(HotChocolateFusionServiceCollectionExtensions)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.Name is "BuildRouterAsync" or "BuildGatewayAsync")
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .Select(m => new
            {
                m.Name,
                m.IsAssembly,
                m.IsPublic,
                Obsolete = m.GetCustomAttribute<ObsoleteAttribute>()?.Message
            })
            .ToArray();

        var excluded = new
        {
            Packaging = new
            {
                GatewayConfigurationTypeRemoved =
                    packagingAssembly.GetType("HotChocolate.Fusion.Packaging.GatewayConfiguration") is null,
                RouterConfigurationExists = typeof(RouterConfiguration) is not null,
                TryGetRouterConfigurationAsyncExists = typeof(FusionArchive)
                    .GetMethod("TryGetRouterConfigurationAsync", BindingFlags.Public | BindingFlags.Instance)
                    is not null,
                GetSupportedRouterFormatsAsyncExists = typeof(FusionArchive)
                    .GetMethod("GetSupportedRouterFormatsAsync", BindingFlags.Public | BindingFlags.Instance)
                    is not null
            },
            Setup = new
            {
                FusionRouterSetupExists = typeof(FusionRouterSetup) is not null,
                FusionGatewaySetupTypeRemoved =
                    configurationAssembly.GetType("HotChocolate.Fusion.Configuration.FusionGatewaySetup") is null
            },
            ExecutionTypes = new
            {
                IsRouterFieldExists = typeof(FusionOutputFieldDefinition).GetProperty("IsRouterField") is not null,
                IsGatewayFieldRemoved = typeof(FusionOutputFieldDefinition).GetProperty("IsGatewayField") is null
            },
            BuildHooks = buildMethods
        };

        // assert
        new { Covered = new { Families = coveredFamilies, EntryPoints = coveredEntryPoints }, Excluded = excluded }
            .MatchMarkdownSnapshot();
    }

    private static object DescribeFamily((string Name, Type Legacy, Type Router) family)
    {
        var legacyMethods = GetExtensionMethods(family.Legacy);
        var routerMethods = GetExtensionMethods(family.Router);
        var routerSignatures = routerMethods
            .Select(NormalizeSignature)
            .ToHashSet(StringComparer.Ordinal);

        var signatures = legacyMethods
            .Select(m =>
            {
                var signature = NormalizeSignature(m);
                return $"{signature}: hasRouterTwin={routerSignatures.Contains(signature)}";
            })
            .ToArray();

        return new
        {
            family.Name,
            LegacyMethodCount = legacyMethods.Length,
            RouterMethodCount = routerMethods.Length,
            AllLegacySignaturesHaveRouterTwin = legacyMethods.All(m => routerSignatures.Contains(NormalizeSignature(m))),
            Signatures = signatures
        };
    }

    private static MethodInfo[] GetExtensionMethods(Type declaringType)
        => declaringType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => !m.IsSpecialName)
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ToArray();

    private static string NormalizeSignature(MethodInfo method)
    {
        var parameterTypesAfterBuilder = method.GetParameters()
            .Skip(1)
            .Select(p => p.ParameterType.Name);
        var genericArity = method.IsGenericMethodDefinition ? method.GetGenericArguments().Length : 0;
        return $"{method.Name}<{genericArity}>({string.Join(", ", parameterTypesAfterBuilder)})";
    }

    private static string DescribeEntryPoint(Type declaringType, string methodName, Type firstParameterType)
    {
        var method = declaringType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == methodName && m.GetParameters()[0].ParameterType == firstParameterType);
        var obsolete = method.GetCustomAttribute<ObsoleteAttribute>();
        return $"{declaringType.Name}.{methodName}({firstParameterType.Name}): Obsolete=\"{obsolete?.Message}\"";
    }
}
