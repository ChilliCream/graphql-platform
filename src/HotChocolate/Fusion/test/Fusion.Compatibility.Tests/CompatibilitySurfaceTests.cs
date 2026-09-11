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
            ("AspNetCore", typeof(AspNetCoreFusionGatewayBuilderExtensions), typeof(AspNetCoreFusionRouterBuilderExtensions)),
            // Each broker family lives in a single static class, with the legacy and router
            // overloads distinguished only by the builder type of their first parameter.
            ("NATS", typeof(NatsEventStreamBrokerServiceCollectionExtensions), typeof(NatsEventStreamBrokerServiceCollectionExtensions)),
            ("Kafka", typeof(KafkaEventStreamBrokerServiceCollectionExtensions), typeof(KafkaEventStreamBrokerServiceCollectionExtensions)),
            ("Redis", typeof(RedisEventStreamBrokerServiceCollectionExtensions), typeof(RedisEventStreamBrokerServiceCollectionExtensions)),
            ("AmazonSqs", typeof(AmazonSqsEventStreamBrokerServiceCollectionExtensions), typeof(AmazonSqsEventStreamBrokerServiceCollectionExtensions)),
            ("AzureEventHubs", typeof(AzureEventHubsEventStreamBrokerServiceCollectionExtensions), typeof(AzureEventHubsEventStreamBrokerServiceCollectionExtensions)),
            ("Mcp", typeof(FusionGatewayBuilderExtensions), typeof(FusionRouterBuilderExtensions)),
            ("OpenApi", typeof(OpenApiFusionGatewayBuilderExtensions), typeof(OpenApiFusionRouterBuilderExtensions))
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
                TryGetRouterConfigurationAsyncExists = typeof(FusionArchive)
                    .GetMethod("TryGetRouterConfigurationAsync", BindingFlags.Public | BindingFlags.Instance)
                    is not null,
                TryGetGatewayConfigurationAsyncRemoved = typeof(FusionArchive)
                    .GetMethod("TryGetGatewayConfigurationAsync", BindingFlags.Public | BindingFlags.Instance)
                    is null,
                GetSupportedRouterFormatsAsyncExists = typeof(FusionArchive)
                    .GetMethod("GetSupportedRouterFormatsAsync", BindingFlags.Public | BindingFlags.Instance)
                    is not null,
                GetSupportedGatewayFormatsAsyncRemoved = typeof(FusionArchive)
                    .GetMethod("GetSupportedGatewayFormatsAsync", BindingFlags.Public | BindingFlags.Instance)
                    is null,
                SupportedGatewayFormatsRemoved = typeof(ArchiveMetadata)
                    .GetProperty("SupportedGatewayFormats", BindingFlags.Public | BindingFlags.Instance)
                    is null
            },
            Setup = new
            {
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
        // Legacy and router overloads are told apart by the builder type of their first
        // parameter, not by declaring type: broker families keep both on one static class.
#pragma warning disable CS0618 // Reflecting over the obsolete IFusionGatewayBuilder type itself.
        var legacyMethods = GetExtensionMethods(family.Legacy, typeof(IFusionGatewayBuilder));
#pragma warning restore CS0618
        var routerMethods = GetExtensionMethods(family.Router, typeof(IFusionRouterBuilder));
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
            AllLegacyObsoleteOnGatewayBuilder = legacyMethods.Length > 0
                && legacyMethods.All(m => m.GetCustomAttribute<ObsoleteAttribute>() is not null),
            AllRouterCleanOnRouterBuilder = routerMethods.Length > 0
                && routerMethods.All(m => m.GetCustomAttribute<ObsoleteAttribute>() is null),
            Signatures = signatures
        };
    }

    private static MethodInfo[] GetExtensionMethods(Type declaringType, Type firstParameterType)
        => declaringType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => !m.IsSpecialName && HasFirstParameter(m, firstParameterType))
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ToArray();

    private static bool HasFirstParameter(MethodInfo method, Type parameterType)
    {
        var parameters = method.GetParameters();
        return parameters.Length > 0 && parameters[0].ParameterType == parameterType;
    }

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
