using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Packaging;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class LegacyArchiveTests : FusionTestBase
{
    [Fact]
    public async Task Gateway_Should_ExecuteQuery_When_ArchiveContainsLegacyInterfaceObjectArgument()
    {
        // arrange
        var schema = ComposeSchemaDocument("type Query { field: String }").ToString(indented: true);
        var legacySchema = schema.Replace(
            "@fusion__schema_metadata(name: \"a\"",
            "@fusion__schema_metadata(allowNonResolvableInterfaceObjects: true, name: \"a\"",
            StringComparison.Ordinal);
        Assert.NotEqual(schema, legacySchema);
        var archivePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.far");
        var ct = TestContext.Current.CancellationToken;

        try
        {
            using (var archive = FusionArchive.Create(archivePath))
            using (var settings = JsonDocument.Parse("{}"))
            {
                await archive.SetArchiveMetadataAsync(
                    new ArchiveMetadata
                    {
                        SupportedGatewayFormats = [WellKnownVersions.LatestGatewayFormatVersion],
                        SourceSchemas = ["a"]
                    },
                    ct);
                await archive.SetGatewayConfigurationAsync(
                    legacySchema,
                    settings,
                    WellKnownVersions.LatestGatewayFormatVersion,
                    ct);
                await archive.CommitAsync(ct);
            }

            var services = new ServiceCollection();
            services.AddHttpClient();
            services.AddGraphQLGateway().AddFileSystemConfiguration(archivePath);
            await using var provider = services.BuildServiceProvider();
            var executor = await provider.GetRequestExecutorAsync(cancellationToken: ct);

            // act
            await using var result = await executor.ExecuteAsync("{ __typename }", ct);

            // assert
            result.MatchInlineSnapshot(
                """
                {
                  "data": {
                    "__typename": "Query"
                  }
                }
                """);
        }
        finally
        {
            File.Delete(archivePath);
        }
    }
}
