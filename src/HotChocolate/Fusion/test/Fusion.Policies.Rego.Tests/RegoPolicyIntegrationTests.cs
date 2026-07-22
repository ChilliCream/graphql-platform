using System.IO.Hashing;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Policies.Rego;

public sealed class RegoPolicyIntegrationTests
{
    private const string PolicyName = "CanReadProduct";
    private const string Requirements = "{ id }";
    private const string SchemaText = "type Query { product: Product } type Product { id: ID }";

    private const string GrantByDataRego =
        "package CanReadProduct\n"
        + "import rego.v1\n"
        + "allow := [object.get(data.permissions.product_readers, entity.id, false) | "
        + "some entity in input.entities]\n";

    private const string GrantAllRego =
        "package CanReadProduct\n"
        + "import rego.v1\n"
        + "allow := [true | some entity in input.entities]\n";

    private static readonly Version s_version = new("1.0.0");

    [Fact]
    public async Task Policy_Should_TakeEffectAndHotReload_When_PackageContentChanges()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var configurations = new List<FusionConfiguration>();

        try
        {
            var v1 = await BuildConfigurationAsync(
                GrantByDataRego,
                """{ "permissions": { "product_readers": { "1": true } } }""",
                ct);
            configurations.Add(v1);

            await using var configProvider = new MutableFusionConfigurationProvider(v1);
            await using var dataProvider = new ArchiveRegoPolicyDataProvider(configProvider);
            await using var policyProvider = new RegoPolicyProvider(configProvider, [dataProvider], new NoopDiagnostics());
            using var registry = new PolicyCollection([policyProvider]);
            registry.Connect();

            // act
            // The policy from the package grants product "1" and denies product "2".
            var v1Product1Denied = await IsDeniedAsync(registry, "1", ct);
            var v1Product2Denied = await IsDeniedAsync(registry, "2", ct);

            // policy-only change (rego grants everyone, data and schema unchanged).
            var v2 = await BuildConfigurationAsync(
                GrantAllRego,
                """{ "permissions": { "product_readers": { "1": true } } }""",
                ct);
            configurations.Add(v2);
            var before = registry.Get(PolicyName);
            configProvider.Publish(v2);
            var dedupKeyUnchanged = DedupKey(v1) == DedupKey(v2);
            var reloadReplacedInstance = !ReferenceEquals(before, registry.Get(PolicyName));
            var v2Product2Denied = await IsDeniedAsync(registry, "2", ct);

            // data-only change (data now grants product "2", rego unchanged).
            var v3 = await BuildConfigurationAsync(
                GrantByDataRego,
                """{ "permissions": { "product_readers": { "2": true } } }""",
                ct);
            configurations.Add(v3);
            configProvider.Publish(v3);
            var v3Product2Denied = await IsDeniedAsync(registry, "2", ct);
            var v3Product1Denied = await IsDeniedAsync(registry, "1", ct);

            // assert
            // The policy takes effect, a policy-only reload swaps the instance without an executor
            // rebuild (dedup key unchanged), and a data-only reload changes the decision in place.
            $"""
            v1: product1Denied={v1Product1Denied}, product2Denied={v1Product2Denied}
            policyReload: dedupKeyUnchanged={dedupKeyUnchanged}, replacedInstance={reloadReplacedInstance}, product2Denied={v2Product2Denied}
            dataReload: product2Denied={v3Product2Denied}, product1Denied={v3Product1Denied}
            """.MatchInlineSnapshot(
                """
                v1: product1Denied=False, product2Denied=True
                policyReload: dedupKeyUnchanged=True, replacedInstance=True, product2Denied=False
                dataReload: product2Denied=False, product1Denied=True
                """);
        }
        finally
        {
            foreach (var configuration in configurations)
            {
                configuration.Dispose();
            }
        }
    }

    [Fact]
    public async Task ReadAsync_Should_ReturnNull_When_OnlyFormatExceedsRuntimeMax()
    {
        // arrange
        // The archive carries only a policy format newer than this runtime supports, so nothing is read.
        var ct = TestContext.Current.CancellationToken;
        await using var stream = new MemoryStream();
        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetRegoPolicyAsync(
                PolicyName,
                Encoding.UTF8.GetBytes(GrantAllRego),
                Encoding.UTF8.GetBytes(Requirements),
                new Version(2, 0, 0),
                ct);
            await archive.CommitAsync(ct);
        }

        stream.Position = 0;

        // act
        using var readArchive = FusionArchive.Open(stream, FusionArchiveMode.Read, leaveOpen: true);
        var snapshot = await PackagePolicyContentReader.ReadAsync(
            readArchive,
            new Version(1, 0, 0),
            ct);

        // assert
        Assert.Null(snapshot);
    }

    [Fact]
    public async Task ReadAsync_Should_SelectHighestFormat_WithinRuntimeMax()
    {
        // arrange
        // The archive carries both a supported and a newer format; only the supported one is read.
        var ct = TestContext.Current.CancellationToken;
        await using var stream = new MemoryStream();
        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetRegoPolicyAsync(
                PolicyName,
                Encoding.UTF8.GetBytes(GrantAllRego),
                Encoding.UTF8.GetBytes(Requirements),
                new Version(1, 0, 0),
                ct);
            await archive.SetRegoPolicyAsync(
                PolicyName,
                Encoding.UTF8.GetBytes(GrantAllRego),
                Encoding.UTF8.GetBytes(Requirements),
                new Version(2, 0, 0),
                ct);
            await archive.CommitAsync(ct);
        }

        stream.Position = 0;

        // act
        using var readArchive = FusionArchive.Open(stream, FusionArchiveMode.Read, leaveOpen: true);
        using var snapshot = await PackagePolicyContentReader.ReadAsync(
            readArchive,
            new Version(1, 0, 0),
            ct);

        // assert
        Assert.NotNull(snapshot);
        Assert.Equal(new Version(1, 0, 0), snapshot.FormatVersion);
    }

    private static async Task<bool> IsDeniedAsync(
        PolicyCollection registry,
        string productId,
        CancellationToken cancellationToken)
    {
        var policy = registry.Get(PolicyName);
        using var entity = RegoPolicyTestEntities.CreateEntity(productId, "code", "extra");
        var context = new RegoPolicyTestEntities.TestPolicyContext();

        await policy.EvaluateAsync(
            context,
            new[] { entity.Data },
            cancellationToken);

        return context.DeniedIndices.Contains(0);
    }

    private static async Task<FusionConfiguration> BuildConfigurationAsync(
        string rego,
        string data,
        CancellationToken cancellationToken)
    {
        var stream = new MemoryStream();

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(
                new ArchiveMetadata
                {
                    SupportedGatewayFormats = [new Version("1.0.0")],
                    SourceSchemas = []
                },
                cancellationToken);
            await archive.SetRegoPolicyAsync(
                PolicyName,
                Encoding.UTF8.GetBytes(rego),
                Encoding.UTF8.GetBytes(Requirements),
                s_version,
                cancellationToken);
            await archive.SetRegoDataAsync(
                string.Empty,
                Encoding.UTF8.GetBytes(data),
                s_version,
                cancellationToken);
            await archive.CommitAsync(cancellationToken);
        }

        stream.Position = 0;
        PolicyContentSnapshot? snapshot;

        using (var archive = FusionArchive.Open(stream, FusionArchiveMode.Read, leaveOpen: true))
        {
            snapshot = await PackagePolicyContentReader.ReadAsync(
                archive,
                WellKnownVersions.LatestRegoPolicyFormatVersion,
                cancellationToken);
        }

        await stream.DisposeAsync();
        Assert.NotNull(snapshot);

        var schema = Utf8GraphQLParser.Parse(SchemaText);
        var settings = new JsonDocumentOwner(JsonDocument.Parse("{}"), new EmptyMemoryOwner());
        return new FusionConfiguration(schema, settings) { Policies = snapshot };
    }

    private static (ulong Schema, ulong Settings) DedupKey(FusionConfiguration configuration)
        => (XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(configuration.Schema.ToString())),
            XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(configuration.Settings.Document.RootElement.GetRawText())));

    private sealed class EmptyMemoryOwner : System.Buffers.IMemoryOwner<byte>
    {
        public Memory<byte> Memory => default;

        public void Dispose()
        {
        }
    }

    private sealed class NoopDiagnostics : FusionExecutionDiagnosticEventListener;
}
