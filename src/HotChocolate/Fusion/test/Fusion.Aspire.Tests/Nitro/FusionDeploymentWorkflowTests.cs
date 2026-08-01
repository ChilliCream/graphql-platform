using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.SourceSchema.Packaging;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class FusionDeploymentWorkflowTests
{
    [Fact]
    public void Assembly_Should_ExposeOnlySeedUpdateOptions_When_Inspected()
    {
        // act
        var exportedTypes = typeof(FusionDeploymentWorkflow)
            .Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace is "HotChocolate.Fusion.Aspire.Nitro")
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        // assert
        // NitroSeedUpdateOptions is the configuration surface of AddNitro, everything else that
        // drives the deployment workflow stays internal.
        Assert.Equal(
            ["HotChocolate.Fusion.Aspire.Nitro.NitroSeedUpdateOptions"],
            exportedTypes);
    }

    [Fact]
    public void PublishAsync_Should_AcceptFusionArchiveFromMemory_When_Inspected()
    {
        typeof(FusionDeploymentWorkflow)
            .GetMethod(nameof(FusionDeploymentWorkflow.PublishAsync))!
            .ToString()
            .MatchInlineSnapshot(
                """
                System.Threading.Tasks.Task PublishAsync(HotChocolate.Fusion.Aspire.Nitro.FusionPublicationRequest, System.ReadOnlyMemory`1[System.Byte], System.Threading.CancellationToken)
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
            var firstPath = IOPath.Combine(directory, "first.fss");
            var secondPath = IOPath.Combine(directory, "second.fss");
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
                619211A45A3F75EC35BC88FAF88BA74173FE42C157B7F3020CBC896198CFE224
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
        // arrange
        var directory = CreateTemporaryDirectory();
        try
        {
            var remotePath = IOPath.Combine(directory, "remote.fss");
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
            var nitro = new FakeNitro { RemoteArchive = remoteArchive };
            var workflow = CreateWorkflow(nitro);

            // act
            using var result = await workflow.DownloadSourceSchemaAsync(
                CreateTarget(),
                new FusionSourceSchemaVersion("products", "20260730"),
                TestContext.Current.CancellationToken);

            // assert
            Assert.NotNull(result);
            var ownedArchive = GetUnderlyingArray(result.Archive);
            $"""
            Name: {result.Name}
            Version: {result.Version}
            Content SHA-256: {result.ContentSha256}
            Archive matches: {result.Archive.ToArray().SequenceEqual(remoteArchive)}
            Requested: {nitro.LastDownloadName}/{nitro.LastDownloadVersion}
            API key: {nitro.LastApiKey}
            """.MatchInlineSnapshot(
                """
                Name: products
                Version: 20260730
                Content SHA-256: 619211A45A3F75EC35BC88FAF88BA74173FE42C157B7F3020CBC896198CFE224
                Archive matches: True
                Requested: products/20260730
                API key: secret
                """);

            result.Dispose();
            Assert.Equal(new byte[ownedArchive.Length], ownedArchive);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadSourceSchemaAsync_Should_ReturnNull_When_VersionDoesNotExist()
    {
        // arrange
        var nitro = new FakeNitro();
        var workflow = CreateWorkflow(nitro);

        // act
        var result = await workflow.DownloadSourceSchemaAsync(
            CreateTarget(),
            new FusionSourceSchemaVersion("products", "missing"),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Null(result);
        Assert.Equal(1, nitro.DownloadCount);
        Assert.Equal("products", nitro.LastDownloadName);
        Assert.Equal("missing", nitro.LastDownloadVersion);
    }

    [Fact]
    public async Task DownloadSourceSchemaAsync_Should_SendAccessToken_When_TargetUsesCliSession()
    {
        // arrange
        var nitro = new FakeNitro();
        var workflow = CreateWorkflow(nitro);
        var target = new FusionTarget(
            new Uri("https://api.chillicream.com"),
            "api-id",
            NitroCredential.FromAccessToken("access-token"));

        // act
        await workflow.DownloadSourceSchemaAsync(
            target,
            new FusionSourceSchemaVersion("products", "missing"),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Null(nitro.LastApiKey);
        Assert.Equal("Bearer access-token", nitro.LastAuthorization);
    }

    [Fact]
    public async Task DownloadSourceSchemaAsync_Should_Throw_When_NitroRejectsTheApiKey()
    {
        // arrange
        var nitro = new FakeNitro { DownloadStatusCode = HttpStatusCode.Unauthorized };
        var workflow = CreateWorkflow(nitro);

        // act
        var exception = await Assert.ThrowsAsync<FusionDeploymentException>(
            () => workflow.DownloadSourceSchemaAsync(
                CreateTarget(),
                new FusionSourceSchemaVersion("products", "20260730"),
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal("Nitro rejected the supplied API key.", exception.Message);
    }

    [Fact]
    public async Task DownloadSourceSchemaAsync_Should_RejectArchive_When_NameDiffers()
    {
        // arrange
        var directory = CreateTemporaryDirectory();
        try
        {
            var remotePath = IOPath.Combine(directory, "remote.fss");
            await CreateArchiveAsync(
                remotePath,
                "inventory",
                "type Query { product: String }",
                """{"name":"inventory"}""");
            var nitro = new FakeNitro
            {
                RemoteArchive = await File.ReadAllBytesAsync(
                    remotePath,
                    TestContext.Current.CancellationToken)
            };
            var workflow = CreateWorkflow(nitro);

            // act
            var exception = await Assert.ThrowsAsync<FusionDeploymentException>(
                () => workflow.DownloadSourceSchemaAsync(
                    CreateTarget(),
                    new FusionSourceSchemaVersion("products", "20260730"),
                    TestContext.Current.CancellationToken));

            // assert
            Assert.Equal(
                "The source schema settings name must exactly match 'products'.",
                exception.Message);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadSourceSchemaAsync_Should_RejectArchive_When_DecompressedEntryIsTooLarge()
    {
        // arrange
        var directory = CreateTemporaryDirectory();
        try
        {
            var remotePath = IOPath.Combine(directory, "remote.fss");
            await CreateOversizedSettingsArchiveAsync(remotePath);
            var nitro = new FakeNitro
            {
                RemoteArchive = await File.ReadAllBytesAsync(
                    remotePath,
                    TestContext.Current.CancellationToken)
            };
            var workflow = CreateWorkflow(nitro);

            // act
            var exception = await Assert.ThrowsAsync<FusionDeploymentException>(
                () => workflow.DownloadSourceSchemaAsync(
                    CreateTarget(),
                    new FusionSourceSchemaVersion("products", "20260730"),
                    TestContext.Current.CancellationToken));

            // assert
            Assert.Equal(
                "The Fusion source schema archive is invalid.",
                exception.Message);
            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileSourceSchemaAsync_Should_NoOp_When_NormalizedContentMatches()
    {
        // arrange
        var directory = CreateTemporaryDirectory();
        try
        {
            var localPath = IOPath.Combine(directory, "local.fss");
            var remotePath = IOPath.Combine(directory, "remote.fss");
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
            var nitro = new FakeNitro
            {
                RemoteArchive = await File.ReadAllBytesAsync(
                    remotePath,
                    TestContext.Current.CancellationToken)
            };
            var workflow = CreateWorkflow(nitro);

            // act
            await workflow.ReconcileSourceSchemaAsync(
                CreateTarget(),
                await CreateUploadAsync(localPath),
                TestContext.Current.CancellationToken);

            // assert
            Assert.Equal(0, nitro.UploadCount);
            Assert.Equal(1, nitro.DownloadCount);
            Assert.Empty(nitro.Calls);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileSourceSchemaAsync_Should_ThrowCollision_When_ContentDiffers()
    {
        // arrange
        var directory = CreateTemporaryDirectory();
        try
        {
            var localPath = IOPath.Combine(directory, "local.fss");
            var remotePath = IOPath.Combine(directory, "remote.fss");
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
            var nitro = new FakeNitro
            {
                RemoteArchive = await File.ReadAllBytesAsync(
                    remotePath,
                    TestContext.Current.CancellationToken)
            };
            var workflow = CreateWorkflow(nitro);
            var upload = await CreateUploadAsync(localPath);

            // act
            var exception = await Assert.ThrowsAsync<FusionIdentityCollisionException>(
                () => workflow.ReconcileSourceSchemaAsync(
                    CreateTarget(),
                    upload,
                    TestContext.Current.CancellationToken));

            // assert
            Assert.Equal(
                "Source schema 'products' version '20260730' already exists "
                + "with different normalized schema, settings, or extensions.",
                exception.Message);
            Assert.Equal(0, nitro.UploadCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileSourceSchemaAsync_Should_UploadArchive_When_VersionIsMissing()
    {
        // arrange
        var directory = CreateTemporaryDirectory();
        try
        {
            var archivePath = IOPath.Combine(directory, "products.fss");
            await CreateArchiveAsync(
                archivePath,
                "products",
                "type Query { product: String }",
                """{"name":"products"}""");
            var nitro = new FakeNitro();
            var workflow = CreateWorkflow(nitro);

            // act
            await workflow.ReconcileSourceSchemaAsync(
                CreateTarget(),
                await CreateUploadAsync(archivePath),
                TestContext.Current.CancellationToken);

            // assert
            $"""
            Calls: {string.Join(", ", nitro.Calls)}
            Uploads: {nitro.UploadCount}
            Multipart: {nitro.LastContentType}
            File name: {nitro.UploadBody!.Contains("source-schema.zip", StringComparison.Ordinal)}
            Tag: {nitro.UploadBody!.Contains("20260730", StringComparison.Ordinal)}
            API key: {nitro.LastApiKey}
            """.MatchInlineSnapshot(
                """
                Calls: UploadFusionSourceSchema
                Uploads: 1
                Multipart: multipart/form-data
                File name: True
                Tag: True
                API key: secret
                """);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileSourceSchemaAsync_Should_VerifyReadBack_When_UploadIsUncertain()
    {
        // arrange
        var directory = CreateTemporaryDirectory();
        try
        {
            var archivePath = IOPath.Combine(directory, "products.fss");
            await CreateArchiveAsync(
                archivePath,
                "products",
                "type Query { product: String }",
                """{"name":"products"}""");
            var archive = await File.ReadAllBytesAsync(
                archivePath,
                TestContext.Current.CancellationToken);
            var nitro = new FakeNitro
            {
                UploadException = new IOException("Connection reset."),
                RemoteArchiveAfterUpload = archive
            };
            var workflow = CreateWorkflow(nitro);

            // act
            await workflow.ReconcileSourceSchemaAsync(
                CreateTarget(),
                await CreateUploadAsync(archivePath),
                TestContext.Current.CancellationToken);

            // assert
            Assert.Equal(1, nitro.UploadCount);
            Assert.Equal(2, nitro.DownloadCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileSourceSchemaAsync_Should_VerifyReadBack_When_TagIsDuplicated()
    {
        // arrange
        var directory = CreateTemporaryDirectory();
        try
        {
            var archivePath = IOPath.Combine(directory, "products.fss");
            await CreateArchiveAsync(
                archivePath,
                "products",
                "type Query { product: String }",
                """{"name":"products"}""");
            var nitro = new FakeNitro
            {
                UploadErrorTypeName = "DuplicatedTagError",
                RemoteArchiveAfterUpload = await File.ReadAllBytesAsync(
                    archivePath,
                    TestContext.Current.CancellationToken)
            };
            var workflow = CreateWorkflow(nitro);

            // act
            await workflow.ReconcileSourceSchemaAsync(
                CreateTarget(),
                await CreateUploadAsync(archivePath),
                TestContext.Current.CancellationToken);

            // assert
            Assert.Equal(1, nitro.UploadCount);
            Assert.Equal(2, nitro.DownloadCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileSourceSchemaAsync_Should_Fail_When_UploadIsRejected()
    {
        // arrange
        var directory = CreateTemporaryDirectory();
        try
        {
            var archivePath = IOPath.Combine(directory, "products.fss");
            await CreateArchiveAsync(
                archivePath,
                "products",
                "type Query { product: String }",
                """{"name":"products"}""");
            var nitro = new FakeNitro
            {
                UploadErrorTypeName = "InvalidSourceMetadataInputError",
                UploadErrorMessage = "The source metadata is invalid."
            };
            var workflow = CreateWorkflow(nitro);
            var upload = await CreateUploadAsync(archivePath);

            // act
            var exception = await Assert.ThrowsAsync<FusionDeploymentException>(
                () => workflow.ReconcileSourceSchemaAsync(
                    CreateTarget(),
                    upload,
                    TestContext.Current.CancellationToken));

            // assert
            Assert.Equal(
                "Nitro rejected the Fusion source schema upload. "
                + "The source metadata is invalid.",
                exception.Message);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task PublishAsync_Should_Commit_When_ValidationSucceeds()
    {
        // arrange
        var fusionArchive = Encoding.UTF8.GetBytes("fusion archive");
        var nitro = new FakeNitro
        {
            Events =
            [
                new WatchEvent("ProcessingTaskIsReady"),
                new WatchEvent("FusionConfigurationValidationSuccess"),
                new WatchEvent("FusionConfigurationPublishingSuccess")
            ]
        };
        var workflow = CreateWorkflow(nitro);

        // act
        await workflow.PublishAsync(
            CreatePublicationRequest(force: false),
            fusionArchive,
            TestContext.Current.CancellationToken);

        // assert
        $"""
        Calls: {string.Join(", ", nitro.Calls)}
        Validated archive: {DescribeConfiguration(nitro.ValidateBody)}
        Committed archive: {DescribeConfiguration(nitro.CommitBody)}
        """.MatchInlineSnapshot(
            """
            Calls: BeginFusionDeployment, WatchFusionDeployment, ClaimFusionDeployment, ValidateFusionDeployment, CommitFusionDeployment
            Validated archive: gateway.fgp, fusion archive
            Committed archive: gateway.far, fusion archive
            """);
    }

    [Fact]
    public async Task PublishAsync_Should_Wait_When_InitialStateIsInProgress()
    {
        // arrange
        var nitro = new FakeNitro
        {
            Events =
            [
                new WatchEvent("OperationInProgress"),
                new WatchEvent("ProcessingTaskIsReady"),
                new WatchEvent("FusionConfigurationValidationSuccess"),
                new WatchEvent("FusionConfigurationPublishingSuccess")
            ]
        };
        var workflow = CreateWorkflow(nitro);

        // act
        await workflow.PublishAsync(
            CreatePublicationRequest(force: false),
            Encoding.UTF8.GetBytes("fusion archive"),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            [
                "BeginFusionDeployment",
                "WatchFusionDeployment",
                "ClaimFusionDeployment",
                "ValidateFusionDeployment",
                "CommitFusionDeployment"
            ],
            nitro.Calls);
    }

    [Fact]
    public async Task PublishAsync_Should_DescribeUnknownRemoteState()
    {
        // arrange
        var nitro = new FakeNitro
        {
            Events = [new WatchEvent("FuturePublicationState", "RECONCILING")]
        };
        var workflow = CreateWorkflow(nitro);

        // act
        var exception = await Assert.ThrowsAsync<FusionIndeterminateStateException>(
            () => workflow.PublishAsync(
                CreatePublicationRequest(force: false),
                Encoding.UTF8.GetBytes("fusion archive"),
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "The Fusion publication request was already in an ambiguous "
            + "'FuturePublicationState (RECONCILING)' state. "
            + "Nitro request ID: 'request-id'.",
            exception.Message);
    }

    [Fact]
    public async Task PublishAsync_Should_ReleaseAndFail_When_ValidationFailsWithoutForce()
    {
        // arrange
        var fusionArchive = Encoding.UTF8.GetBytes("fusion archive");
        var nitro = new FakeNitro
        {
            Events =
            [
                new WatchEvent("ProcessingTaskIsReady"),
                new WatchEvent(
                    "FusionConfigurationValidationFailed",
                    ["Breaking schema change."])
            ]
        };
        var workflow = CreateWorkflow(nitro);

        // act
        var exception = await Assert.ThrowsAsync<FusionDeploymentException>(
            () => workflow.PublishAsync(
                CreatePublicationRequest(force: false),
                fusionArchive,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "Nitro rejected the Fusion configuration validation. "
            + "Breaking schema change.",
            exception.Message);
        Assert.Equal(
            [
                "BeginFusionDeployment",
                "WatchFusionDeployment",
                "ClaimFusionDeployment",
                "ValidateFusionDeployment",
                "ReleaseFusionDeployment"
            ],
            nitro.Calls);
    }

    [Fact]
    public async Task PublishAsync_Should_Commit_When_ValidationFailsWithForce()
    {
        // arrange
        var fusionArchive = Encoding.UTF8.GetBytes("fusion archive");
        var nitro = new FakeNitro
        {
            Events =
            [
                new WatchEvent("ProcessingTaskIsReady"),
                new WatchEvent(
                    "FusionConfigurationValidationFailed",
                    ["Breaking schema change."]),
                new WatchEvent("FusionConfigurationPublishingSuccess")
            ]
        };
        var workflow = CreateWorkflow(nitro);

        // act
        await workflow.PublishAsync(
            CreatePublicationRequest(force: true),
            fusionArchive,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            [
                "BeginFusionDeployment",
                "WatchFusionDeployment",
                "ClaimFusionDeployment",
                "ValidateFusionDeployment",
                "CommitFusionDeployment"
            ],
            nitro.Calls);
    }

    [Fact]
    public async Task PublishAsync_Should_ReleaseClaim_When_CommitIsRejected()
    {
        // arrange
        var nitro = new FakeNitro
        {
            Events =
            [
                new WatchEvent("ProcessingTaskIsReady"),
                new WatchEvent("FusionConfigurationValidationSuccess")
            ],
            CommitErrorMessage = "The commit was rejected."
        };
        var workflow = CreateWorkflow(nitro);

        // act
        var exception = await Assert.ThrowsAsync<FusionDeploymentException>(
            () => workflow.PublishAsync(
                CreatePublicationRequest(force: false),
                Encoding.UTF8.GetBytes("fusion archive"),
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "Nitro could not commit the Fusion configuration for request "
            + "'request-id'. The commit was rejected.",
            exception.Message);
        Assert.Equal(
            [
                "BeginFusionDeployment",
                "WatchFusionDeployment",
                "ClaimFusionDeployment",
                "ValidateFusionDeployment",
                "CommitFusionDeployment",
                "ReleaseFusionDeployment"
            ],
            nitro.Calls);
    }

    [Fact]
    public async Task PublishAsync_Should_Fail_When_CommitReturnsUnknownErrorType()
    {
        // arrange
        var nitro = new FakeNitro
        {
            Events =
            [
                new WatchEvent("ProcessingTaskIsReady"),
                new WatchEvent("FusionConfigurationValidationSuccess")
            ],
            CommitErrorTypeName = "FutureCommitError"
        };
        var workflow = CreateWorkflow(nitro);

        // act
        var exception = await Assert.ThrowsAsync<FusionDeploymentException>(
            () => workflow.PublishAsync(
                CreatePublicationRequest(force: false),
                Encoding.UTF8.GetBytes("fusion archive"),
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "Nitro could not commit the Fusion configuration for request "
            + "'request-id'. Nitro returned an error of type 'FutureCommitError'.",
            exception.Message);
        Assert.Equal("ReleaseFusionDeployment", nitro.Calls[^1]);
    }

    [Fact]
    public async Task PublishAsync_Should_ReleaseClaim_When_ValidationTimesOut()
    {
        // arrange
        var nitro = new FakeNitro
        {
            Events = [new WatchEvent("ProcessingTaskIsReady")],
            ValidateDelay = TimeSpan.FromSeconds(5)
        };
        var workflow = CreateWorkflow(nitro);

        // act
        var exception = await Assert.ThrowsAsync<FusionIndeterminateStateException>(
            () => workflow.PublishAsync(
                CreatePublicationRequest(
                    force: false,
                    operationTimeout: TimeSpan.FromMilliseconds(100)),
                Encoding.UTF8.GetBytes("fusion archive"),
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "The Fusion publication timed out before a terminal result could be verified. "
            + "Nitro request ID: 'request-id'.",
            exception.Message);
        Assert.Equal(
            [
                "BeginFusionDeployment",
                "WatchFusionDeployment",
                "ClaimFusionDeployment",
                "ValidateFusionDeployment",
                "ReleaseFusionDeployment"
            ],
            nitro.Calls);
    }

    [Fact]
    public async Task PublishAsync_Should_Fail_When_NitroRejectsTheRequest()
    {
        // arrange
        var nitro = new FakeNitro
        {
            BeginErrorMessage = "The stage does not exist."
        };
        var workflow = CreateWorkflow(nitro);

        // act
        var exception = await Assert.ThrowsAsync<FusionDeploymentException>(
            () => workflow.PublishAsync(
                CreatePublicationRequest(force: false),
                Encoding.UTF8.GetBytes("fusion archive"),
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "Nitro rejected the Fusion publication request. The stage does not exist.",
            exception.Message);
        Assert.Equal(["BeginFusionDeployment"], nitro.Calls);
    }

    [Fact]
    public async Task PublishAsync_Should_Fail_When_TheSubscriptionEndsWithoutATerminalResult()
    {
        // arrange
        var nitro = new FakeNitro
        {
            Events = [new WatchEvent("ProcessingTaskIsQueued")]
        };
        var workflow = CreateWorkflow(nitro);

        // act
        var exception = await Assert.ThrowsAsync<FusionIndeterminateStateException>(
            () => workflow.PublishAsync(
                CreatePublicationRequest(force: false),
                Encoding.UTF8.GetBytes("fusion archive"),
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "The Fusion publication event stream ended before Nitro reported "
            + "a terminal result. Nitro request ID: 'request-id'.",
            exception.Message);
        Assert.Equal("request-id", exception.RequestId);
    }

    [Fact]
    public async Task PublishAsync_Should_Fail_When_TheSubscriptionIsNotAnEventStream()
    {
        // arrange
        // A server that ignores the pinned Accept header answers with a single JSON result.
        var nitro = new FakeNitro
        {
            Events = [new WatchEvent("ProcessingTaskIsReady")],
            WatchIsEventStream = false
        };
        var workflow = CreateWorkflow(nitro);

        // act
        var exception = await Assert.ThrowsAsync<FusionIndeterminateStateException>(
            () => workflow.PublishAsync(
                CreatePublicationRequest(force: false),
                Encoding.UTF8.GetBytes("fusion archive"),
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "The Fusion publication event stream ended with an error. "
            + "Nitro request ID: 'request-id'.",
            exception.Message);
        Assert.Equal(
            "Nitro answered the subscription with the content type "
            + "'application/json' instead of text/event-stream.",
            exception.InnerException!.Message);
    }

    private static FusionDeploymentWorkflow CreateWorkflow(FakeNitro nitro)
        => new(new NitroFusionApi(new HttpClient(nitro), disposeHttpClient: true));

    private static FusionTarget CreateTarget()
        => new(
            new Uri("https://api.chillicream.com"),
            "api-id",
            "secret");

    private static FusionPublicationRequest CreatePublicationRequest(bool force)
        => CreatePublicationRequest(force, TimeSpan.FromMinutes(1));

    private static FusionPublicationRequest CreatePublicationRequest(
        bool force,
        TimeSpan operationTimeout)
        => new(
            CreateTarget(),
            "production",
            "20260730",
            [new FusionSourceSchemaVersion("products", "20260730")],
            WaitForApproval: false,
            Force: force,
            OperationTimeout: operationTimeout,
            ApprovalTimeout: TimeSpan.FromMinutes(1));

    private static string DescribeConfiguration(string? body)
    {
        if (body is null)
        {
            return "-";
        }

        var fileName = body.Contains("gateway.fgp", StringComparison.Ordinal)
            ? "gateway.fgp"
            : body.Contains("gateway.far", StringComparison.Ordinal)
                ? "gateway.far"
                : "unknown file";
        var content = body.Contains("fusion archive", StringComparison.Ordinal)
            ? "fusion archive"
            : "unknown content";

        return $"{fileName}, {content}";
    }

    private static byte[] GetUnderlyingArray(ReadOnlyMemory<byte> archive)
    {
        Assert.True(MemoryMarshal.TryGetArray(archive, out ArraySegment<byte> segment));
        return segment.Array!;
    }

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
        var path = IOPath.Combine(
            IOPath.GetTempPath(),
            "nitro-fusion-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed record WatchEvent(string TypeName, string State = "PROCESSING")
    {
        public WatchEvent(string typeName, IReadOnlyList<string> errors)
            : this(typeName, "PROCESSING")
        {
            Errors = errors;
        }

        public IReadOnlyList<string> Errors { get; } = [];
    }

    private sealed class FakeNitro : HttpMessageHandler
    {
        private const string ApiKeyHeader = "CCC-api-key";

        public byte[]? RemoteArchive { get; set; }

        public byte[]? RemoteArchiveAfterUpload { get; set; }

        public HttpStatusCode? DownloadStatusCode { get; set; }

        public Exception? UploadException { get; set; }

        public string? UploadErrorTypeName { get; set; }

        public string UploadErrorMessage { get; set; } = "The upload was rejected.";

        public string? BeginErrorMessage { get; set; }

        public string? CommitErrorMessage { get; set; }

        public string? CommitErrorTypeName { get; set; }

        public TimeSpan? ValidateDelay { get; set; }

        public IReadOnlyList<WatchEvent> Events { get; init; } = [];

        public bool WatchIsEventStream { get; init; } = true;

        public int DownloadCount { get; private set; }

        public int UploadCount { get; private set; }

        public string? LastDownloadName { get; private set; }

        public string? LastDownloadVersion { get; private set; }

        public string? LastApiKey { get; private set; }

        public string? LastAuthorization { get; private set; }

        public string? LastContentType { get; private set; }

        public List<string> Calls { get; } = [];

        public string? UploadBody { get; private set; }

        public string? ValidateBody { get; private set; }

        public string? CommitBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastApiKey = request.Headers.TryGetValues(ApiKeyHeader, out var values)
                ? values.Single()
                : null;
            LastAuthorization = request.Headers.Authorization?.ToString();

            if (request.Method == HttpMethod.Get)
            {
                return Download(request);
            }

            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            LastContentType = request.Content?.Headers.ContentType?.MediaType;
            var operationName = ReadOperationName(body);
            Calls.Add(operationName);

            switch (operationName)
            {
                case NitroOperationDocuments.UploadSourceSchemaOperationName:
                    UploadCount++;
                    UploadBody = body;
                    if (UploadException is not null)
                    {
                        throw UploadException;
                    }

                    return Json(UploadResult());

                case NitroOperationDocuments.BeginDeploymentOperationName:
                    return Json(BeginResult());

                case NitroOperationDocuments.WatchDeploymentOperationName:
                    return Watch();

                case NitroOperationDocuments.ClaimDeploymentOperationName:
                    return Json(CommandResult("startFusionConfigurationComposition"));

                case NitroOperationDocuments.ValidateDeploymentOperationName:
                    ValidateBody = body;
                    if (ValidateDelay is { } validateDelay)
                    {
                        await Task.Delay(validateDelay, cancellationToken);
                    }

                    return Json(CommandResult("validateFusionConfigurationComposition"));

                case NitroOperationDocuments.CommitDeploymentOperationName:
                    CommitBody = body;
                    return Json(CommandResult(
                        "commitFusionConfigurationPublish",
                        CommitErrorMessage,
                        CommitErrorTypeName));

                case NitroOperationDocuments.ReleaseDeploymentOperationName:
                    return Json(CommandResult("cancelFusionConfigurationComposition"));

                default:
                    throw new InvalidOperationException(
                        $"The Nitro operation '{operationName}' is not scripted.");
            }
        }

        private HttpResponseMessage Download(HttpRequestMessage request)
        {
            DownloadCount++;
            var segments = request.RequestUri!.AbsolutePath.Split('/');
            LastDownloadName = Uri.UnescapeDataString(segments[6]);
            LastDownloadVersion = Uri.UnescapeDataString(segments[8]);

            if (DownloadStatusCode is { } statusCode)
            {
                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(string.Empty)
                };
            }

            var archive = DownloadCount > 1 && RemoteArchiveAfterUpload is not null
                ? RemoteArchiveAfterUpload
                : RemoteArchive;

            if (archive is null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(string.Empty)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(archive.ToArray())
            };
        }

        private string UploadResult()
        {
            if (UploadErrorTypeName is null)
            {
                return """
                    {"data":{"uploadFusionSubgraph":{"fusionSubgraphVersion":{"id":"version-1"},"errors":[]}}}
                    """;
            }

            return "{\"data\":{\"uploadFusionSubgraph\":{\"fusionSubgraphVersion\":null,\"errors\":["
                + Error(UploadErrorTypeName, UploadErrorMessage)
                + "]}}}";
        }

        private string BeginResult()
            => BeginErrorMessage is null
                ? """
                    {"data":{"beginFusionConfigurationPublish":{"requestId":"request-id","errors":[]}}}
                    """
                : "{\"data\":{\"beginFusionConfigurationPublish\":{\"requestId\":null,\"errors\":["
                    + Error("StageNotFoundError", BeginErrorMessage)
                    + "]}}}";

        private static string Error(string typeName, string message)
            => "{\"__typename\":\"" + typeName + "\",\"message\":\"" + message + "\"}";

        private static string CommandResult(
            string fieldName,
            string? errorMessage = null,
            string? errorTypeName = null)
            => errorMessage is null && errorTypeName is null
                ? "{\"data\":{\"" + fieldName + "\":{\"errors\":[]}}}"
                : "{\"data\":{\"" + fieldName + "\":{\"errors\":["
                    + (errorMessage is null
                        ? "{\"__typename\":\"" + errorTypeName + "\"}"
                        : Error(
                            errorTypeName ?? "FusionConfigurationPublishingError",
                            errorMessage))
                    + "]}}}";

        private HttpResponseMessage Watch()
        {
            var payloads = Events.Select(WatchPayload).ToArray();

            return WatchIsEventStream
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        BuildEventStream(payloads),
                        Encoding.UTF8,
                        "text/event-stream")
                }
                : Json(payloads[0]);
        }

        private static string WatchPayload(WatchEvent watchEvent)
        {
            var errors = string.Join(
                ',',
                watchEvent.Errors.Select(error => "{\"message\":\"" + error + "\"}"));

            return "{\"data\":{\"onFusionConfigurationPublishingTaskChanged\":{"
                + "\"__typename\":\"" + watchEvent.TypeName + "\","
                + "\"state\":\"" + watchEvent.State + "\","
                + "\"errors\":[" + errors + "]}}}";
        }

        private static string BuildEventStream(IEnumerable<string> payloads)
        {
            var builder = new StringBuilder();

            foreach (var payload in payloads)
            {
                builder.Append("event: next\n");

                foreach (var line in payload.Split('\n'))
                {
                    builder.Append("data: ").Append(line.TrimEnd('\r')).Append('\n');
                }

                builder.Append('\n');
            }

            builder.Append("event: complete\n\n");

            return builder.ToString();
        }

        private static HttpResponseMessage Json(string json)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

        private static string ReadOperationName(string body)
        {
            string[] operationNames =
            [
                NitroOperationDocuments.UploadSourceSchemaOperationName,
                NitroOperationDocuments.BeginDeploymentOperationName,
                NitroOperationDocuments.WatchDeploymentOperationName,
                NitroOperationDocuments.ClaimDeploymentOperationName,
                NitroOperationDocuments.ValidateDeploymentOperationName,
                NitroOperationDocuments.CommitDeploymentOperationName,
                NitroOperationDocuments.ReleaseDeploymentOperationName
            ];

            return Array.Find(
                    operationNames,
                    operationName => body.Contains(operationName, StringComparison.Ordinal))
                ?? "unknown";
        }
    }
}
