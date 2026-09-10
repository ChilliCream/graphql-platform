using System.Reflection;
using System.Runtime.Loader;
using HotChocolate.Fusion.Configuration;

namespace HotChocolate.Fusion.Compatibility;

/// <summary>
/// Loads the unchanged, pinned-16.6.4 baseline consumer assembly (see
/// Baseline/artifacts/PROVENANCE.md) through a custom <see cref="AssemblyLoadContext"/> that
/// redirects its HotChocolate.* references to the locally built, current product assemblies
/// instead of the 16.6.4 copies it was compiled against, then invokes it by reflection. This is
/// the "unchanged consumer binary executed against the new product assemblies" proof from the
/// design spec's release-proof list, distinct from the recompiled-source proof in
/// SourceCompatibilityTests. The baseline project itself is never rebuilt by this test.
/// </summary>
public sealed class BinaryBaselineCompatibilityTests
{
    [Fact]
    public async Task BaselineConsumer_Should_ProduceExpectedResults_When_RunAgainstCurrentProductAssemblies()
    {
        // arrange
        var currentProductDirectory = System.IO.Path.GetDirectoryName(typeof(IFusionRouterBuilder).Assembly.Location)!;
        var baselineDllPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Baseline", BaselineAssemblyFileName);
        Assert.True(File.Exists(baselineDllPath), $"Baseline artifact not found at '{baselineDllPath}'.");

        var loadContext = new RedirectingLoadContext(currentProductDirectory);

        // act
        var baselineAssembly = loadContext.LoadFromAssemblyPath(baselineDllPath);

        Assert.NotEqual(
            typeof(IFusionRouterBuilder).Assembly.Location,
            baselineAssembly.Location);

        var entryPointType = baselineAssembly.GetType(
            "HotChocolate.Fusion.Compatibility.BaselineConsumer.BaselineEntryPoint",
            throwOnError: true)!;
        var runMethod = entryPointType.GetMethod("RunAsync", BindingFlags.Public | BindingFlags.Static)!;
        var resultTask = (Task<string>)runMethod.Invoke(obj: null, parameters: null)!;
        var result = await resultTask;

        var loadedFusionExecutionAssembly = loadContext.Assemblies
            .Single(a => a.GetName().Name == "HotChocolate.Fusion.Execution");

        loadContext.Unload();

        var fields = result.Split('|').OrderBy(field => field, StringComparer.Ordinal).ToArray();

        // assert: the baseline's own HotChocolate.Fusion.Execution reference resolved to the
        // current build output, not to a 16.6.4 copy sitting anywhere near the baseline DLL.
        Assert.Equal(
            typeof(IFusionRouterBuilder).Assembly.Location,
            loadedFusionExecutionAssembly.Location);
        fields.MatchInlineSnapshot(
            """
            [
              "AddCacheControl.OriginalReceiverIdentity=True",
              "AddGraphQLGateway.DefaultName=_Default",
              "AddGraphQLGateway.DefaultServicesIdentity=True",
              "AddGraphQLGateway.NamedName=baseline-named",
              "AddGraphQLGatewayServer.Name=baseline-server",
              "CustomBuilder.OriginalReceiverIdentity=True",
              "Host.AddGraphQLGateway.Name=baseline-host",
              "Host.AddGraphQLGateway.ServicesIdentity=True",
              "IRequestExecutorProvider.SchemaNames=baseline-server",
              "ThirdPartyExtension.OriginalReceiverIdentity=True",
              "UseQueryCache.OriginalReceiverIdentity=True"
            ]
            """);
    }

    private const string BaselineAssemblyFileName = "HotChocolate.Fusion.Compatibility.BaselineConsumer.dll";

    private sealed class RedirectingLoadContext(string currentProductDirectory)
        : AssemblyLoadContext(isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name is { } name && name.StartsWith("HotChocolate.", StringComparison.Ordinal))
            {
                var candidatePath = System.IO.Path.Combine(currentProductDirectory, name + ".dll");
                if (File.Exists(candidatePath))
                {
                    return LoadFromAssemblyPath(candidatePath);
                }
            }

            // Falls through to AssemblyLoadContext.Default, which already carries the
            // Microsoft.Extensions.* assemblies loaded by this test host.
            return null;
        }
    }
}
