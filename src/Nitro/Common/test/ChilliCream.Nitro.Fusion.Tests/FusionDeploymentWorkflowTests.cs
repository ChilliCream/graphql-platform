using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.Fusion.Transport;
using HotChocolate.Fusion.SourceSchema.Packaging;

namespace ChilliCream.Nitro.Fusion;

public sealed class FusionDeploymentWorkflowTests
{
    [Fact]
    public void Assembly_Should_ExposeOnlyWorkflowContract_When_Inspected()
    {
        var exportedTypes = typeof(IFusionDeploymentWorkflow)
            .Assembly
            .GetExportedTypes()
            .Select(type => type.FullName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        string.Join(Environment.NewLine, exportedTypes)
            .MatchInlineSnapshot(
                """
                ChilliCream.Nitro.Fusion.FusionDeploymentException
                ChilliCream.Nitro.Fusion.FusionIdentityCollisionException
                ChilliCream.Nitro.Fusion.FusionIndeterminateStateException
                ChilliCream.Nitro.Fusion.FusionPublicationRequest
                ChilliCream.Nitro.Fusion.FusionSourceSchemaUpload
                ChilliCream.Nitro.Fusion.FusionSourceSchemaVersion
                ChilliCream.Nitro.Fusion.FusionTarget
                ChilliCream.Nitro.Fusion.IFusionDeploymentWorkflow
                ChilliCream.Nitro.Fusion.NitroFusionServiceCollectionExtensions
                """);
    }

    [Fact]
    public async Task ReconcileSourceSchemaAsync_Should_NoOp_When_NormalizedContentMatches()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var localPath = Path.Combine(directory, "local.fss");
            var remotePath = Path.Combine(directory, "remote.fss");
            await CreateArchiveAsync(
                localPath,
                "products",
                "type Query { product: String }",
                """{"name":"products","transports":{"http":{"url":"https://example.com"}}}""");
            await CreateArchiveAsync(
                remotePath,
                "products",
                """
                type Query {
                  product: String
                }
                """,
                """{"transports":{"http":{"url":"https://example.com"}},"name":"products"}""");
            var transport = new FakeTransport
            {
                RemoteArchive = await File.ReadAllBytesAsync(
                    remotePath,
                    TestContext.Current.CancellationToken)
            };
            var workflow = CreateWorkflow(transport);

            await workflow.ReconcileSourceSchemaAsync(
                CreateTarget(),
                await CreateUploadAsync(localPath),
                TestContext.Current.CancellationToken);

            Assert.Equal(0, transport.UploadCount);
            Assert.Equal(1, transport.DownloadCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileSourceSchemaAsync_Should_ThrowCollision_When_ContentDiffers()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var localPath = Path.Combine(directory, "local.fss");
            var remotePath = Path.Combine(directory, "remote.fss");
            await CreateArchiveAsync(
                localPath,
                "products",
                "type Query { product: String }",
                """{"name":"products"}""");
            await CreateArchiveAsync(
                remotePath,
                "products",
                "type Query { product: Int }",
                """{"name":"products"}""");
            var transport = new FakeTransport
            {
                RemoteArchive = await File.ReadAllBytesAsync(
                    remotePath,
                    TestContext.Current.CancellationToken)
            };
            var workflow = CreateWorkflow(transport);
            var upload = await CreateUploadAsync(localPath);

            var exception = await Assert.ThrowsAsync<FusionIdentityCollisionException>(
                () => workflow.ReconcileSourceSchemaAsync(
                    CreateTarget(),
                    upload,
                    TestContext.Current.CancellationToken));

            Assert.Equal(
                "Source schema 'products' version '20260730' already exists "
                + "with different normalized schema, settings, or extensions.",
                exception.Message);
            Assert.Equal(0, transport.UploadCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileSourceSchemaAsync_Should_VerifyReadBack_When_UploadIsUncertain()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var archivePath = Path.Combine(directory, "products.fss");
            await CreateArchiveAsync(
                archivePath,
                "products",
                "type Query { product: String }",
                """{"name":"products"}""");
            var archive = await File.ReadAllBytesAsync(
                archivePath,
                TestContext.Current.CancellationToken);
            var transport = new FakeTransport
            {
                UploadException = new IOException("Connection reset."),
                RemoteArchiveAfterUpload = archive
            };
            var workflow = CreateWorkflow(transport);

            await workflow.ReconcileSourceSchemaAsync(
                CreateTarget(),
                await CreateUploadAsync(archivePath),
                TestContext.Current.CancellationToken);

            Assert.Equal(1, transport.UploadCount);
            Assert.Equal(2, transport.DownloadCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task PublishAsync_Should_Commit_When_ValidationSucceeds()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var fusionArchivePath = Path.Combine(directory, "gateway.far");
            await File.WriteAllTextAsync(
                fusionArchivePath,
                "fusion archive",
                TestContext.Current.CancellationToken);
            var transport = new FakeTransport
            {
                Events =
                [
                    new(FusionRemoteEventKind.Ready, []),
                    new(FusionRemoteEventKind.ValidationSucceeded, []),
                    new(FusionRemoteEventKind.PublishingSucceeded, [])
                ]
            };
            var workflow = CreateWorkflow(transport);

            await workflow.PublishAsync(
                CreatePublicationRequest(force: false),
                fusionArchivePath,
                TestContext.Current.CancellationToken);

            Assert.Equal(
                ["begin", "watch", "claim", "validate", "commit"],
                transport.Calls);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task PublishAsync_Should_ReleaseAndFail_When_ValidationFailsWithoutForce()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var fusionArchivePath = Path.Combine(directory, "gateway.far");
            await File.WriteAllTextAsync(
                fusionArchivePath,
                "fusion archive",
                TestContext.Current.CancellationToken);
            var transport = new FakeTransport
            {
                Events =
                [
                    new(FusionRemoteEventKind.Ready, []),
                    new(
                        FusionRemoteEventKind.ValidationFailed,
                        ["Breaking schema change."])
                ]
            };
            var workflow = CreateWorkflow(transport);

            var exception = await Assert.ThrowsAsync<FusionDeploymentException>(
                () => workflow.PublishAsync(
                    CreatePublicationRequest(force: false),
                    fusionArchivePath,
                    TestContext.Current.CancellationToken));

            Assert.Equal(
                "Nitro rejected the Fusion configuration validation. "
                + "Breaking schema change.",
                exception.Message);
            Assert.Equal(
                ["begin", "watch", "claim", "validate", "release"],
                transport.Calls);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task PublishAsync_Should_Commit_When_ValidationFailsWithForce()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var fusionArchivePath = Path.Combine(directory, "gateway.far");
            await File.WriteAllTextAsync(
                fusionArchivePath,
                "fusion archive",
                TestContext.Current.CancellationToken);
            var transport = new FakeTransport
            {
                Events =
                [
                    new(
                        FusionRemoteEventKind.Ready,
                        []),
                    new(
                        FusionRemoteEventKind.ValidationFailed,
                        ["Breaking schema change."]),
                    new(
                        FusionRemoteEventKind.PublishingSucceeded,
                        [])
                ]
            };
            var workflow = CreateWorkflow(transport);

            await workflow.PublishAsync(
                CreatePublicationRequest(force: true),
                fusionArchivePath,
                TestContext.Current.CancellationToken);

            Assert.Equal(
                ["begin", "watch", "claim", "validate", "commit"],
                transport.Calls);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static FusionDeploymentWorkflow CreateWorkflow(
        FakeTransport transport)
        => new(new FakeTransportFactory(transport));

    private static FusionTarget CreateTarget()
        => new(
            new Uri("https://api.chillicream.com"),
            "api-id",
            "secret");

    private static FusionPublicationRequest CreatePublicationRequest(bool force)
        => new(
            CreateTarget(),
            "production",
            "20260730",
            [new FusionSourceSchemaVersion("products", "20260730")],
            WaitForApproval: false,
            Force: force,
            OperationTimeout: TimeSpan.FromMinutes(1),
            ApprovalTimeout: TimeSpan.FromMinutes(1));

    private static async Task<FusionSourceSchemaUpload> CreateUploadAsync(
        string archivePath)
    {
        await using var stream = File.OpenRead(archivePath);
        var sha256 = Convert.ToHexString(
            await SHA256.HashDataAsync(
                stream,
                TestContext.Current.CancellationToken));
        return new FusionSourceSchemaUpload(
            "products",
            "20260730",
            archivePath,
            sha256);
    }

    private static async Task CreateArchiveAsync(
        string path,
        string name,
        string schema,
        string settings)
    {
        using var archive = FusionSourceSchemaArchive.Create(path);
        await archive.SetArchiveMetadataAsync(
            new ArchiveMetadata(),
            TestContext.Current.CancellationToken);
        await archive.SetSchemaAsync(
            Encoding.UTF8.GetBytes(schema),
            TestContext.Current.CancellationToken);
        using var settingsDocument = JsonDocument.Parse(settings);
        await archive.SetSettingsAsync(
            settingsDocument,
            TestContext.Current.CancellationToken);
        await archive.CommitAsync(TestContext.Current.CancellationToken);
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "nitro-fusion-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FakeTransportFactory(FakeTransport transport)
        : IFusionDeploymentTransportFactory
    {
        public ValueTask<IFusionDeploymentTransport> OpenAsync(
            FusionTarget target,
            CancellationToken cancellationToken)
            => ValueTask.FromResult<IFusionDeploymentTransport>(transport);
    }

    private sealed class FakeTransport : IFusionDeploymentTransport
    {
        public byte[]? RemoteArchive { get; set; }

        public byte[]? RemoteArchiveAfterUpload { get; set; }

        public Exception? UploadException { get; set; }

        public IReadOnlyList<FusionRemoteEvent> Events { get; init; } = [];

        public int DownloadCount { get; private set; }

        public int UploadCount { get; private set; }

        public List<string> Calls { get; } = [];

        public Task<byte[]?> DownloadSourceSchemaAsync(
            string name,
            string version,
            CancellationToken cancellationToken)
        {
            DownloadCount++;
            return Task.FromResult(
                DownloadCount > 1 && RemoteArchiveAfterUpload is not null
                    ? RemoteArchiveAfterUpload
                    : RemoteArchive);
        }

        public Task<FusionRemoteCommandResult> UploadSourceSchemaAsync(
            string version,
            string archivePath,
            CancellationToken cancellationToken)
        {
            UploadCount++;
            if (UploadException is not null)
            {
                throw UploadException;
            }

            return Task.FromResult(FusionRemoteCommandResult.Success);
        }

        public Task<FusionRemoteBeginResult> BeginPublishAsync(
            FusionPublicationRequest request,
            CancellationToken cancellationToken)
        {
            Calls.Add("begin");
            return Task.FromResult(
                new FusionRemoteBeginResult("request-id", []));
        }

        public async IAsyncEnumerable<FusionRemoteEvent> WatchPublishAsync(
            string requestId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Calls.Add("watch");
            foreach (var @event in Events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return @event;
                await Task.Yield();
            }
        }

        public Task<FusionRemoteCommandResult> ClaimPublishAsync(
            string requestId,
            CancellationToken cancellationToken)
            => Success("claim");

        public Task<FusionRemoteCommandResult> ReleasePublishAsync(
            string requestId,
            CancellationToken cancellationToken)
            => Success("release");

        public Task<FusionRemoteCommandResult> ValidatePublishAsync(
            string requestId,
            string archivePath,
            CancellationToken cancellationToken)
            => Success("validate");

        public Task<FusionRemoteCommandResult> CommitPublishAsync(
            string requestId,
            string archivePath,
            CancellationToken cancellationToken)
            => Success("commit");

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private Task<FusionRemoteCommandResult> Success(string call)
        {
            Calls.Add(call);
            return Task.FromResult(FusionRemoteCommandResult.Success);
        }
    }
}
