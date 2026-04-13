using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Fusion;
using ChilliCream.Nitro.CommandLine.FusionCompatibility;
using HotChocolate.Fusion;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class LegacyFusionArchiveMigratorTests
{
    [Fact]
    public async Task MergeIntoAsync_Should_MigrateAllSubgraphs_When_NoExplicitNames()
    {
        // arrange
        await using var buffer = await LegacyFusionArchiveFixtures.CreateMultiSubgraphAsync();
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>();

        // act
        await LegacyFusionArchiveMigrator.MergeIntoAsync(
            buffer,
            sourceSchemas,
            Array.Empty<string>(),
            CancellationToken.None);

        // assert
        Assert.Equal(2, sourceSchemas.Count);
        Assert.Contains("users", sourceSchemas.Keys);
        Assert.Contains("reviews", sourceSchemas.Keys);

        foreach (var (_, (_, settingsDoc)) in sourceSchemas)
        {
            var transports = settingsDoc.RootElement.GetProperty("transports");
            var http = transports.GetProperty("http");
            var url = http.GetProperty("url").GetString();
            Assert.False(string.IsNullOrEmpty(url));
        }
    }

    [Fact]
    public async Task MergeIntoAsync_Should_SkipSubgraph_When_InExplicitNames()
    {
        // arrange
        await using var buffer = await LegacyFusionArchiveFixtures.CreateMultiSubgraphAsync();
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>();

        // act
        await LegacyFusionArchiveMigrator.MergeIntoAsync(
            buffer,
            sourceSchemas,
            new[] { "users" },
            CancellationToken.None);

        // assert
        Assert.Single(sourceSchemas);
        Assert.Contains("reviews", sourceSchemas.Keys);
        Assert.DoesNotContain("users", sourceSchemas.Keys);
    }

    [Fact]
    public async Task MergeIntoAsync_Should_ReturnMigratedCompositionSettings_When_V1HasFusionSettings()
    {
        // arrange
        await using var buffer = await LegacyFusionArchiveFixtures.CreateMultiSubgraphAsync();
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>();

        // act
        var migrated = await LegacyFusionArchiveMigrator.MergeIntoAsync(
            buffer,
            sourceSchemas,
            Array.Empty<string>(),
            CancellationToken.None);

        // assert
        Assert.NotNull(migrated);
        Assert.NotNull(migrated!.Preprocessor.ExcludeByTag);
        Assert.Contains("alpha", migrated.Preprocessor.ExcludeByTag!);
        Assert.Contains("beta", migrated.Preprocessor.ExcludeByTag!);
    }

    [Fact]
    public async Task MergeIntoAsync_Should_ReturnNullSettings_When_V1HasNoFusionSettings()
    {
        // arrange
        await using var buffer = await LegacyFusionArchiveFixtures.CreateNoSettingsAsync();
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>();

        // act
        var migrated = await LegacyFusionArchiveMigrator.MergeIntoAsync(
            buffer,
            sourceSchemas,
            Array.Empty<string>(),
            CancellationToken.None);

        // assert
        Assert.Null(migrated);
        Assert.Single(sourceSchemas);
    }

    [Fact]
    public async Task MergeIntoAsync_Should_RewindBufferToPositionZero_AfterCall()
    {
        // arrange
        await using var buffer = await LegacyFusionArchiveFixtures.CreateMultiSubgraphAsync();
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>();

        // act
        await LegacyFusionArchiveMigrator.MergeIntoAsync(
            buffer,
            sourceSchemas,
            Array.Empty<string>(),
            CancellationToken.None);

        // assert
        Assert.Equal(0, buffer.Position);
    }

    [Fact]
    public async Task MergeIntoAsync_Should_ThrowExitException_When_SubgraphHasExtensions()
    {
        // arrange
        await using var buffer = await LegacyFusionArchiveFixtures.CreateWithExtensionsAsync();
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>();

        // act
        var ex = await Assert.ThrowsAsync<ExitException>(() =>
            LegacyFusionArchiveMigrator.MergeIntoAsync(
                buffer,
                sourceSchemas,
                Array.Empty<string>(),
                CancellationToken.None));

        // assert
        Assert.Contains("orders", ex.Message);
    }

    [Fact]
    public async Task MergeIntoAsync_Should_ThrowFusionGraphPackageException_When_ArchiveIsCorrupt()
    {
        // arrange
        await using var buffer = new MemoryStream(new byte[] { 0x00, 0x01, 0x02, 0x03 });
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>();

        // act + assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            LegacyFusionArchiveMigrator.MergeIntoAsync(
                buffer,
                sourceSchemas,
                Array.Empty<string>(),
                CancellationToken.None));
    }

    [Fact]
    public async Task MergeIntoAsync_Should_NotOverwrite_When_ExplicitDictAlreadyHasEntry()
    {
        // arrange
        await using var buffer = await LegacyFusionArchiveFixtures.CreateMultiSubgraphAsync();
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>();
        var existingDoc = JsonDocument.Parse("""{"sentinel":true}""");
        var existingText = new SourceSchemaText("users", "type Query { existing: String }");
        sourceSchemas["users"] = (existingText, existingDoc);

        // act
        await LegacyFusionArchiveMigrator.MergeIntoAsync(
            buffer,
            sourceSchemas,
            new[] { "users" },
            CancellationToken.None);

        // assert
        var (schema, settings) = sourceSchemas["users"];
        Assert.Same(existingDoc, settings);
        Assert.Equal("type Query { existing: String }", schema.SourceText);
    }

    [Fact]
    public async Task MergeIntoAsync_Should_ProduceSourceSchemaSettings_That_DeserializeCleanly()
    {
        // arrange
        await using var buffer = await LegacyFusionArchiveFixtures.CreateMultiSubgraphAsync();
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>();

        // act
        await LegacyFusionArchiveMigrator.MergeIntoAsync(
            buffer,
            sourceSchemas,
            Array.Empty<string>(),
            CancellationToken.None);

        // assert
        foreach (var (_, (_, settingsDoc)) in sourceSchemas)
        {
            var settings = settingsDoc.Deserialize(
                SettingsJsonSerializerContext.Default.SourceSchemaSettings);
            Assert.NotNull(settings);
        }
    }

    [Fact]
    public async Task BufferAsync_Should_ReturnSeekablePositionZero_When_InputIsForwardOnly()
    {
        // arrange
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        await using var forwardOnly = new ForwardOnlyStream(payload);

        // act
        await using var result = await LegacyFusionArchiveMigrator.BufferAsync(
            forwardOnly,
            CancellationToken.None);

        // assert
        Assert.True(result.CanSeek);
        Assert.Equal(0, result.Position);
        Assert.Equal(payload.Length, result.Length);
        Assert.Equal(payload, result.ToArray());
    }

    [Fact]
    public void CompositionSettings_MergeInto_Should_ReturnMigratedValues_When_CallerIsDefault()
    {
        // arrange
        var caller = new CompositionSettings();
        var migrated = new CompositionSettings
        {
            Merger = new CompositionSettings.MergerSettings
            {
                EnableGlobalObjectIdentification = true
            }
        };

        // act
        var result = caller.MergeInto(migrated);

        // assert
        Assert.True(result.Merger.EnableGlobalObjectIdentification);
    }

    private sealed class ForwardOnlyStream : Stream
    {
        private readonly MemoryStream _inner;

        public ForwardOnlyStream(byte[] data)
        {
            _inner = new MemoryStream(data);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
            => _inner.Read(buffer, offset, count);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _inner.ReadAsync(buffer, offset, count, cancellationToken);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _inner.ReadAsync(buffer, cancellationToken);

        public override void Flush() => _inner.Flush();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

internal static class LegacyFusionArchiveFixtures
{
    private const string UsersSchema = """
        type Query {
          user(id: ID!): User
        }
        type User {
          id: ID!
          name: String!
        }
        """;

    private const string ReviewsSchema = """
        type Query {
          review(id: ID!): Review
        }
        type Review {
          id: ID!
          rating: Int!
        }
        """;

    private const string OrdersSchema = """
        type Query {
          order(id: ID!): Order
        }
        type Order {
          id: ID!
          total: Float!
        }
        """;

    public static async Task<MemoryStream> CreateMultiSubgraphAsync()
    {
        var buffer = new MemoryStream();
        await using (var package = FusionGraphPackage.Open(buffer, FileAccess.ReadWrite))
        {
            await package.SetSubgraphConfigurationAsync(
                new SubgraphConfiguration(
                    "users",
                    UsersSchema,
                    Array.Empty<string>(),
                    new[] { new HttpClientConfiguration(new Uri("https://users.example.com/graphql")) },
                    configurationExtensions: null));

            await package.SetSubgraphConfigurationAsync(
                new SubgraphConfiguration(
                    "reviews",
                    ReviewsSchema,
                    Array.Empty<string>(),
                    new[] { new HttpClientConfiguration(new Uri("https://reviews.example.com/graphql")) },
                    configurationExtensions: null));

            using var settings = JsonDocument.Parse("""
                {
                  "tagDirective": { "exclude": ["alpha", "beta"] },
                  "nodeField": { "enabled": true }
                }
                """);
            await package.SetFusionGraphSettingsAsync(settings);
        }

        buffer.Position = 0;
        return buffer;
    }

    public static async Task<MemoryStream> CreateNoSettingsAsync()
    {
        var buffer = new MemoryStream();
        await using (var package = FusionGraphPackage.Open(buffer, FileAccess.ReadWrite))
        {
            await package.SetSubgraphConfigurationAsync(
                new SubgraphConfiguration(
                    "users",
                    UsersSchema,
                    Array.Empty<string>(),
                    new[] { new HttpClientConfiguration(new Uri("https://users.example.com/graphql")) },
                    configurationExtensions: null));
        }

        buffer.Position = 0;
        return buffer;
    }

    public static async Task<MemoryStream> CreateWithExtensionsAsync()
    {
        const string ordersExtension = "extend type Order { createdAt: String }";

        var buffer = new MemoryStream();
        await using (var package = FusionGraphPackage.Open(buffer, FileAccess.ReadWrite))
        {
            await package.SetSubgraphConfigurationAsync(
                new SubgraphConfiguration(
                    "orders",
                    OrdersSchema,
                    ordersExtension,
                    new HttpClientConfiguration(new Uri("https://orders.example.com/graphql")),
                    configurationExtensions: null));
        }

        buffer.Position = 0;
        return buffer;
    }
}
