using System.IO.Compression;
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
                ChilliCream.Nitro.Fusion.FusionSourceSchemaContent
                ChilliCream.Nitro.Fusion.FusionSourceSchemaDownload
                ChilliCream.Nitro.Fusion.FusionSourceSchemaUpload
                ChilliCream.Nitro.Fusion.FusionSourceSchemaVersion
                ChilliCream.Nitro.Fusion.FusionTarget
                ChilliCream.Nitro.Fusion.IFusionDeploymentWorkflow
                ChilliCream.Nitro.Fusion.NitroFusionServiceCollectionExtensions
                """);
    }

    [Fact]
    public void PublishAsync_Should_AcceptFusionArchiveFromMemory_When_Inspected()
    {
        typeof(IFusionDeploymentWorkflow)
            .GetMethod(nameof(IFusionDeploymentWorkflow.PublishAsync))!
            .ToString()
            .MatchInlineSnapshot(
                """
                System.Threading.Tasks.Task PublishAsync(ChilliCream.Nitro.Fusion.FusionPublicationRequest, System.ReadOnlyMemory`1[System.Byte], System.Threading.CancellationToken)
                """);
    }

    [Fact]
    public void Dispose_Should_ClearOwnedArchiveAndRejectAccess_When_Called()
    {
        byte[] archive = [1, 2, 3];
        var download = new FusionSourceSchemaDownload(
            "products",
            "20260730",
            archive,
            new string('A', 64));

        Assert.Equal(new byte[] { 1, 2, 3 }, download.Archive.ToArray());

        download.Dispose();

        Assert.Equal(new byte[3], archive);
        Assert.Throws<ObjectDisposedException>(() => download.Archive);

        archive[0] = 9;
        download.Dispose();
        Assert.Equal(new byte[] { 9, 0, 0 }, archive);
    }

    [Fact]
    public async Task ComputeSha256Async_Should_ReturnSameDigest_When_ContentIsNormalized()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var firstPath = Path.Combine(directory, "first.fss");
            var secondPath = Path.Combine(directory, "second.fss");
            await CreateArchiveAsync(
                firstPath,
                "products",
                "type Query { product: String }",
                """{"name":"products","transports":{"http":{"url":"https://example.com"}}}""");
            await CreateArchiveAsync(
                secondPath,
                "products",
                """
                type Query {
                  product: String
                }
                """,
                """{"transports":{"http":{"url":"https://example.com"}},"name":"products"}""");

            var first = await FusionSourceSchemaContent.ComputeSha256Async(
                firstPath,
                "products",
                TestContext.Current.CancellationToken);
            var second = await FusionSourceSchemaContent.ComputeSha256Async(
                secondPath,
                "products",
                TestContext.Current.CancellationToken);

            Assert.Equal(first, second);
            first.MatchInlineSnapshot(
                """
                3B7C4FBDBEC6ECB8C072794A67BD1CB8B4DCFB5153B6D284E662A0D3CB49EC08
                """);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadSourceSchemaAsync_Should_ReturnArchiveAndDigest_When_VersionExists()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var remotePath = Path.Combine(directory, "remote.fss");
            await CreateArchiveAsync(
                remotePath,
                "products",
                """
                type Query {
                  product: String
                }
                """,
                """{"transports":{"http":{"url":"https://example.com"}},"name":"products"}""");
            var remoteArchive = await File.ReadAllBytesAsync(
                remotePath,
                TestContext.Current.CancellationToken);
            var transport = new FakeTransport
            {
                RemoteArchive = remoteArchive
            };
            var workflow = CreateWorkflow(transport);

            using var result = await workflow.DownloadSourceSchemaAsync(
                CreateTarget(),
                new FusionSourceSchemaVersion("products", "20260730"),
                TestContext.Current.CancellationToken);

            Assert.NotNull(result);
            Assert.Equal("products", result.Name);
            Assert.Equal("20260730", result.Version);
            Assert.Equal(
                "3B7C4FBDBEC6ECB8C072794A67BD1CB8B4DCFB5153B6D284E662A0D3CB49EC08",
                result.ContentSha256);
            Assert.Equal(remoteArchive, result.Archive.ToArray());
            Assert.Equal("products", transport.LastDownloadName);
            Assert.Equal("20260730", transport.LastDownloadVersion);

            var ownedArchive = Assert.Single(transport.ReturnedArchives);
            result.Dispose();
            Assert.Equal(new byte[ownedArchive.Length], ownedArchive);
            Assert.Throws<ObjectDisposedException>(() => result.Archive);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadSourceSchemaAsync_Should_ReturnNull_When_VersionDoesNotExist()
    {
        var transport = new FakeTransport();
        var workflow = CreateWorkflow(transport);

        var result = await workflow.DownloadSourceSchemaAsync(
            CreateTarget(),
            new FusionSourceSchemaVersion("products", "missing"),
            TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.Equal(1, transport.DownloadCount);
        Assert.Equal("products", transport.LastDownloadName);
        Assert.Equal("missing", transport.LastDownloadVersion);
    }

    [Fact]
    public async Task DownloadSourceSchemaAsync_Should_RejectArchive_When_NameDiffers()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var remotePath = Path.Combine(directory, "remote.fss");
            await CreateArchiveAsync(
                remotePath,
                "inventory",
                "type Query { product: String }",
                """{"name":"inventory"}""");
            var transport = new FakeTransport
            {
                RemoteArchive = await File.ReadAllBytesAsync(
                    remotePath,
                    TestContext.Current.CancellationToken)
            };
            var workflow = CreateWorkflow(transport);

            var exception = await Assert.ThrowsAsync<FusionDeploymentException>(
                () => workflow.DownloadSourceSchemaAsync(
                    CreateTarget(),
                    new FusionSourceSchemaVersion("products", "20260730"),
                    TestContext.Current.CancellationToken));

            Assert.Equal(
                "The source schema settings name must exactly match 'products'.",
                exception.Message);
            var returnedArchive = Assert.Single(transport.ReturnedArchives);
            Assert.Equal(new byte[returnedArchive.Length], returnedArchive);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadSourceSchemaAsync_Should_RejectArchive_When_DecompressedEntryIsTooLarge()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var remotePath = Path.Combine(directory, "remote.fss");
            await CreateOversizedSettingsArchiveAsync(remotePath);
            var transport = new FakeTransport
            {
                RemoteArchive = await File.ReadAllBytesAsync(
                    remotePath,
                    TestContext.Current.CancellationToken)
            };
            var workflow = CreateWorkflow(transport);

            var exception = await Assert.ThrowsAsync<FusionDeploymentException>(
                () => workflow.DownloadSourceSchemaAsync(
                    CreateTarget(),
                    new FusionSourceSchemaVersion("products", "20260730"),
                    TestContext.Current.CancellationToken));

            Assert.Equal(
                "The Fusion source schema archive is invalid.",
                exception.Message);
            Assert.IsType<InvalidOperationException>(exception.InnerException);
            var returnedArchive = Assert.Single(transport.ReturnedArchives);
            Assert.Equal(new byte[returnedArchive.Length], returnedArchive);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
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
            var returnedArchive = Assert.Single(transport.ReturnedArchives);
            Assert.Equal(new byte[returnedArchive.Length], returnedArchive);
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
            var returnedArchive = Assert.Single(transport.ReturnedArchives);
            Assert.Equal(new byte[returnedArchive.Length], returnedArchive);
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
        var fusionArchive = Encoding.UTF8.GetBytes("fusion archive");
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
            fusionArchive,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            ["begin", "watch", "claim", "validate", "commit"],
            transport.Calls);
        Assert.Equal(fusionArchive, transport.ValidatedArchive);
        Assert.Equal(fusionArchive, transport.CommittedArchive);
    }

    [Fact]
    public async Task PublishAsync_Should_ReleaseAndFail_When_ValidationFailsWithoutForce()
    {
        var fusionArchive = Encoding.UTF8.GetBytes("fusion archive");
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
                fusionArchive,
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "Nitro rejected the Fusion configuration validation. "
            + "Breaking schema change.",
            exception.Message);
        Assert.Equal(
            ["begin", "watch", "claim", "validate", "release"],
            transport.Calls);
    }

    [Fact]
    public async Task PublishAsync_Should_Commit_When_ValidationFailsWithForce()
    {
        var fusionArchive = Encoding.UTF8.GetBytes("fusion archive");
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
            fusionArchive,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            ["begin", "watch", "claim", "validate", "commit"],
            transport.Calls);
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

    private static async Task CreateOversizedSettingsArchiveAsync(string path)
    {
        await using var stream = File.Create(path);
#if NET10_0_OR_GREATER
        await using var archive = new ZipArchive(
#else
        using var archive = new ZipArchive(
#endif
            stream,
            ZipArchiveMode.Create,
            leaveOpen: true);
        await using (var schema = archive.CreateEntry("schema.graphqls").Open())
        {
            await schema.WriteAsync(
                Encoding.UTF8.GetBytes("type Query { product: String }"),
                TestContext.Current.CancellationToken);
        }

        await using (var settings =
            archive.CreateEntry("schema-settings.json").Open())
        {
            await settings.WriteAsync(
                Encoding.UTF8.GetBytes(
                    $$"""{"name":"products","padding":"{{new string('a', 512_000)}}"}"""),
                TestContext.Current.CancellationToken);
        }
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

        public string? LastDownloadName { get; private set; }

        public string? LastDownloadVersion { get; private set; }

        public List<byte[]> ReturnedArchives { get; } = [];

        public List<string> Calls { get; } = [];

        public byte[]? ValidatedArchive { get; private set; }

        public byte[]? CommittedArchive { get; private set; }

        public Task<byte[]?> DownloadSourceSchemaAsync(
            string name,
            string version,
            CancellationToken cancellationToken)
        {
            DownloadCount++;
            LastDownloadName = name;
            LastDownloadVersion = version;
            var archive = DownloadCount > 1 && RemoteArchiveAfterUpload is not null
                ? RemoteArchiveAfterUpload
                : RemoteArchive;
            if (archive is null)
            {
                return Task.FromResult<byte[]?>(null);
            }

            var ownedArchive = archive.ToArray();
            ReturnedArchives.Add(ownedArchive);
            return Task.FromResult<byte[]?>(ownedArchive);
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
            ReadOnlyMemory<byte> archive,
            CancellationToken cancellationToken)
        {
            ValidatedArchive = archive.ToArray();
            return Success("validate");
        }

        public Task<FusionRemoteCommandResult> CommitPublishAsync(
            string requestId,
            ReadOnlyMemory<byte> archive,
            CancellationToken cancellationToken)
        {
            CommittedArchive = archive.ToArray();
            return Success("commit");
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private Task<FusionRemoteCommandResult> Success(string call)
        {
            Calls.Add(call);
            return Task.FromResult(FusionRemoteCommandResult.Success);
        }
    }
}
