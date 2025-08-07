using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace HotChocolate.Fusion.Packaging;

public class FusionArchiveTests : IDisposable
{
    private readonly List<Stream> _streamsToDispose = new();
    private readonly List<X509Certificate2> _certificatesToDispose = new();

    [Fact]
    public void Create_WithNullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => FusionArchive.Create(null!));
    }

    [Fact]
    public void Open_WithNullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => FusionArchive.Open(default(Stream)!));
    }

    [Fact]
    public void Open_WithNullString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => FusionArchive.Open(default(string)!));
    }

    [Fact]
    public async Task SetArchiveMetadata_WithValidData_StoresCorrectly()
    {
        // Arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata
        {
            FormatVersion = new Version("1.0.0"),
            SupportedGatewayFormats = [new Version("2.0.0"), new Version("2.1.0")],
            SourceSchemas = ["user-service", "product-service"]
        };

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);

        // Can read immediately within the same session
        var retrieved = await archive.GetArchiveMetadataAsync();
        Assert.NotNull(retrieved);
        Assert.Equal(metadata.FormatVersion, retrieved.FormatVersion);
        Assert.Equal(metadata.SupportedGatewayFormats, retrieved.SupportedGatewayFormats);
        Assert.Equal(metadata.SourceSchemas, retrieved.SourceSchemas);
    }

    [Fact]
    public async Task GetArchiveMetadata_WhenNotSet_ReturnsNull()
    {
        // Arrange
        await using var stream = CreateStream();

        // Act & Assert
        using var archive = FusionArchive.Create(stream);
        var result = await archive.GetArchiveMetadataAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task SetArchiveMetadata_WithNullMetadata_ThrowsArgumentNullException()
    {
        // Arrange
        await using var stream = CreateStream();

        // Act & Assert
        using var archive = FusionArchive.Create(stream);
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => archive.SetArchiveMetadataAsync(null!));
    }

    [Fact]
    public async Task GetLatestSupportedGatewayFormat_WithValidMetadata_ReturnsHighestVersion()
    {
        // Arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("1.0.0"), new Version("2.1.0"), new Version("2.0.0")],
            SourceSchemas = ["test-service"]
        };

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        var latest = await archive.GetLatestSupportedGatewayFormatAsync();
        Assert.Equal(new Version("2.1.0"), latest);
    }

    [Fact]
    public async Task GetLatestSupportedGatewayFormat_WithoutMetadata_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var stream = CreateStream();

        // Act & Assert
        using var archive = FusionArchive.Create(stream);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.GetLatestSupportedGatewayFormatAsync());
    }

    [Fact]
    public async Task SetCompositionSettings_WithValidJsonDocument_StoresCorrectly()
    {
        // Arrange
        await using var stream = CreateStream();
        const string settingsJson = """{"enableNodeSpec": true, "maxDepth": 10}""";
        using var settings = JsonDocument.Parse(settingsJson);

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetCompositionSettingsAsync(settings);

        // Can read immediately within the same session
        using var retrieved = await archive.GetCompositionSettingsAsync();
        Assert.NotNull(retrieved);
        Assert.True(retrieved.RootElement.GetProperty("enableNodeSpec").GetBoolean());
        Assert.Equal(10, retrieved.RootElement.GetProperty("maxDepth").GetInt32());
    }

    [Fact]
    public async Task GetCompositionSettings_WhenNotSet_ReturnsNull()
    {
        // Arrange
        await using var stream = CreateStream();

        // Act & Assert
        using var archive = FusionArchive.Create(stream);
        var result = await archive.GetCompositionSettingsAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task SetGatewaySchema_WithStringContent_StoresCorrectly()
    {
        // Arrange
        await using var stream = CreateStream();
        const string schema = "type Query { hello: String }";
        var settings = CreateSettingsJson();
        var version = new Version("2.0.0");

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.SetGatewayConfigurationAsync(schema, settings, version);

        // Can read immediately within the same session
        var result = await archive.TryGetGatewayConfigurationAsync(version);

        Assert.NotNull(result);
        Assert.Equal(version, result.Version);

        var retrievedSchema = Encoding.UTF8.GetString(result.Schema);
        Assert.Equal(schema, retrievedSchema);

        result.Dispose();
    }

    [Fact]
    public async Task SetGatewaySchema_WithByteContent_StoresCorrectly()
    {
        // Arrange
        await using var stream = CreateStream();
        var schema = "type Query { hello: String }"u8.ToArray();
        var settings = CreateSettingsJson();
        var version = new Version("2.0.0");

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.SetGatewayConfigurationAsync(schema, settings, version);

        // Can read immediately within the same session
        var result = await archive.TryGetGatewayConfigurationAsync(version);

        Assert.NotNull(result);
        Assert.Equal(version, result.Version);
        Assert.Equal(schema, result.Schema.ToArray());

        result.Dispose();
    }

    [Fact]
    public async Task SetGatewaySchema_WithoutMetadata_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var stream = CreateStream();

        // Act & Assert
        using var archive = FusionArchive.Create(stream);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.SetGatewayConfigurationAsync("schema", CreateSettingsJson(), new Version("1.0.0")));
    }

    [Fact]
    public async Task SetGatewaySchema_WithUnsupportedVersion_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var stream = CreateStream();

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            archive.SetGatewayConfigurationAsync("schema", CreateSettingsJson(),new Version("3.0.0")));
    }

    [Fact]
    public async Task TryGetGatewaySchema_WithCompatibleVersion_ReturnsCorrectVersion()
    {
        // Arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("1.0.0"), new Version("2.0.0"), new Version("2.1.0")],
            SourceSchemas = ["test-service"]
        };

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.SetGatewayConfigurationAsync("schema v1.0", CreateSettingsJson(), new Version("1.0.0"));
        await archive.SetGatewayConfigurationAsync("schema v2.0", CreateSettingsJson(), new Version("2.0.0"));
        await archive.SetGatewayConfigurationAsync("schema v2.1", CreateSettingsJson(), new Version("2.1.0"));

        // Request max version 2.0.0, should get 2.0.0
        var result = await archive.TryGetGatewayConfigurationAsync(new Version("2.0.0"));

        Assert.NotNull(result);
        Assert.Equal(new Version("2.0.0"), result.Version);

        var schema = Encoding.UTF8.GetString(result.Schema);
        Assert.Equal("schema v2.0", schema);

        result.Dispose();
    }

    [Fact]
    public async Task TryGetGatewaySchema_WithIncompatibleVersion_ReturnsFalse()
    {
        // Arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("2.0.0")],
            SourceSchemas = ["test-service"]
        };

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);

        var result = await archive.TryGetGatewayConfigurationAsync(new Version("1.0.0"));

        Assert.Null(result);
    }

    [Fact]
    public async Task SetSourceSchema_WithValidSchema_StoresCorrectly()
    {
        // Arrange
        await using var stream = CreateStream();
        var schemaContent = "type User { id: ID! name: String! }"u8.ToArray();
        var settings = CreateSettingsJson();
        const string schemaName = "user-service";

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.SetSourceSchemaConfigurationAsync(schemaName, schemaContent, settings);

        // Can read immediately within the same session
        var found = await archive.TryGetSourceSchemaConfigurationAsync(schemaName);

        Assert.NotNull(found);
        Assert.True(schemaContent.AsSpan().SequenceEqual(found.Schema));
    }

    [Fact]
    public async Task SetSourceSchema_WithInvalidSchemaName_ThrowsArgumentException()
    {
        // Arrange
        await using var stream = CreateStream();

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);

        await Assert.ThrowsAsync<ArgumentException>(
            () => archive.SetSourceSchemaConfigurationAsync(
                "invalid name!",
                "schema"u8.ToArray(),
                CreateSettingsJson()));
    }

    [Fact]
    public async Task SetSourceSchema_WithUndeclaredSchemaName_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("2.0.0")],
            SourceSchemas = ["declared-schema"]
        };

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.SetSourceSchemaConfigurationAsync(
                "undeclared-schema",
                "schema"u8.ToArray(),
                CreateSettingsJson()));
    }

    [Fact]
    public async Task TryGetSourceSchema_WithNonExistentSchema_ReturnsFalse()
    {
        // Arrange
        await using var stream = CreateStream();

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        var found = await archive.TryGetSourceSchemaConfigurationAsync("non-existent");
        Assert.Null(found);
    }

    [Fact]
    public async Task SignArchive_WithValidCertificate_CreatesSignature()
    {
        // Arrange
        await using var stream = CreateStream();
        using var cert = CreateTestCertificate();

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.SetGatewayConfigurationAsync("schema", CreateSettingsJson(), new Version("2.0.0"));
        await archive.SignArchiveAsync(cert);

        // Can verify immediately within the same session
        Assert.True(archive.IsSigned);

        var signatureInfo = await archive.GetSignatureInfoAsync();
        Assert.NotNull(signatureInfo);
        Assert.Equal("SHA256", signatureInfo.Algorithm);
        Assert.NotNull(signatureInfo.SignerCertificate);
    }

    [Fact]
    public async Task SignArchive_WithCertificateWithoutPrivateKey_ThrowsArgumentException()
    {
        // Arrange
        await using var stream = CreateStream();
        using var cert = CreateTestCertificate();
#if NET9_0_OR_GREATER
        using var publicOnlyCert = X509CertificateLoader.LoadCertificate(cert.Export(X509ContentType.Cert));
#else
        using var publicOnlyCert = new X509Certificate2(cert.Export(X509ContentType.Cert));
#endif

        // Act & Assert
        using var archive = FusionArchive.Create(stream);
        await Assert.ThrowsAsync<ArgumentException>(
            () => archive.SignArchiveAsync(publicOnlyCert));
    }

    [Fact]
    public async Task VerifySignature_WithValidSignature_ReturnsValid()
    {
        // Arrange
        await using var stream = CreateStream();
        using var cert = CreateTestCertificate(); // Has private key

        // Extract public key only for verification
#if NET9_0_OR_GREATER
        using var publicOnlyCert = X509CertificateLoader.LoadCertificate(cert.Export(X509ContentType.Cert));
#else
        using var publicOnlyCert = new X509Certificate2(cert.Export(X509ContentType.Cert));
#endif

        // Act & Assert
        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            var metadata = CreateTestMetadata();
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.SetGatewayConfigurationAsync("schema", CreateSettingsJson(), new Version("2.0.0"));

            // Sign with private key
            await archive.SignArchiveAsync(cert);

            // Verify with public key only
            var result = await archive.VerifySignatureAsync(publicOnlyCert);
            Assert.Equal(SignatureVerificationResult.Valid, result);
        }
    }

    [Fact]
    public async Task VerifySignature_WithUnsignedArchive_ReturnsNotSigned()
    {
        // Arrange
        await using var stream = CreateStream();
        using var cert = CreateTestCertificate();

        // Act & Assert
        using var archive = FusionArchive.Create(stream);
        var result = await archive.VerifySignatureAsync(cert);
        Assert.Equal(SignatureVerificationResult.NotSigned, result);
    }

    [Fact]
    public async Task CommitAndReopen_PersistsChanges()
    {
        // Arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("2.0.0")],
            SourceSchemas = ["test-service"]
        };
        const string schema = "type Query { hello: String }";

        // Act - Create and commit
        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.SetGatewayConfigurationAsync(schema, CreateSettingsJson(), new Version("2.0.0"));
            await archive.CommitAsync();
        }

        // Assert - Reopen and verify persistence
        stream.Position = 0;
        using (var readArchive = FusionArchive.Open(stream, leaveOpen: true))
        {
            var retrievedMetadata = await readArchive.GetArchiveMetadataAsync();
            Assert.NotNull(retrievedMetadata);
            Assert.Equal(
                metadata.SupportedGatewayFormats.ToArray(),
                retrievedMetadata.SupportedGatewayFormats.ToArray());

            var result = await readArchive.TryGetGatewayConfigurationAsync(new Version("2.0.0"));
            Assert.NotNull(result);

            var retrievedSchema = Encoding.UTF8.GetString(result.Schema);
            Assert.Equal(schema, retrievedSchema);

            result.Dispose();
        }
    }

    [Fact]
    public async Task UpdateMode_CanModifyExistingArchive()
    {
        // Arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("2.0.0")],
            SourceSchemas = ["test-service"]
        };

        // Act - Create initial archive
        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.SetGatewayConfigurationAsync("original schema", CreateSettingsJson(), new Version("2.0.0"));
            await archive.CommitAsync();
        }

        // Act - Update existing archive
        stream.Position = 0;
        using (var updateArchive = FusionArchive.Open(stream, FusionArchiveMode.Update, leaveOpen: true))
        {
            await updateArchive.SetGatewayConfigurationAsync(
                "modified schema",
                CreateSettingsJson(),
                new Version("2.0.0"));
            await updateArchive.CommitAsync();
        }

        // Assert - Verify modification
        stream.Position = 0;
        using (var readArchive = FusionArchive.Open(stream, leaveOpen: true))
        {
            var result = await readArchive.TryGetGatewayConfigurationAsync(new Version("2.0.0"));
            Assert.NotNull(result);

            var schema = Encoding.UTF8.GetString(result.Schema);
            Assert.Equal("modified schema", schema);

            result.Dispose();
        }
    }

    [Fact]
    public async Task OverwriteFile_WithinSession_ReplacesContent()
    {
        // Arrange
        await using var stream = CreateStream();

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);

        // Set schema twice within the same session
        await archive.SetGatewayConfigurationAsync("first schema", CreateSettingsJson(), new Version("2.0.0"));
        await archive.SetGatewayConfigurationAsync("second schema", CreateSettingsJson(), new Version("2.0.0"));

        // Should get the last value
        var result = await archive.TryGetGatewayConfigurationAsync(new Version("2.0.0"));

        Assert.NotNull(result);
        var schema = Encoding.UTF8.GetString(result.Schema);
        Assert.Equal("second schema", schema);

        result.Dispose();
    }

    [Fact]
    public async Task GetSourceSchemaNames_WithMetadata_ReturnsOrderedNames()
    {
        // Arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("2.0.0")],
            SourceSchemas = ["zebra-service", "alpha-service", "beta-service"]
        };

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        var names = await archive.GetSourceSchemaNamesAsync();
        Assert.Equal(["alpha-service", "beta-service", "zebra-service"], names);
    }

    [Fact]
    public async Task GetSupportedGatewayFormats_WithMetadata_ReturnsDescendingOrder()
    {
        // Arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("1.0.0"), new Version("2.1.0"), new Version("2.0.0")],
            SourceSchemas = ["test-service"]
        };

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        var versions = await archive.GetSupportedGatewayFormatsAsync();
        Assert.Equal([new Version("2.1.0"), new Version("2.0.0"), new Version("1.0.0")], versions);
    }

    [Theory]
    [InlineData("valid-schema")]
    [InlineData("Valid_Schema")]
    [InlineData("schema123")]
    [InlineData("_schema")]
    public async Task SetSourceSchema_WithValidSchemaNames_Succeeds(string schemaName)
    {
        // Arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("2.0.0")],
            SourceSchemas = [schemaName]
        };

        // Act & Assert - Should not throw
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.SetSourceSchemaConfigurationAsync(schemaName, "schema"u8.ToArray(), CreateSettingsJson());
    }

    [Theory]
    [InlineData("invalid name")]
    [InlineData("123invalid")]
    [InlineData("")]
    [InlineData("schema/name")]
    public async Task SetSourceSchema_WithInvalidSchemaNames_ThrowsException(string schemaName)
    {
        // Arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("2.0.0")],
            SourceSchemas = [schemaName]
        };

        // Act & Assert
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);

        await Assert.ThrowsAsync<ArgumentException>(
            () => archive.SetSourceSchemaConfigurationAsync(
                schemaName,
                "schema"u8.ToArray(),
                CreateSettingsJson()));
    }

    [Fact]
    public async Task GetSupportedGatewayFormats_WithoutMetadata_ReturnsEmpty()
    {
        // Arrange
        await using var stream = CreateStream();

        // Act & Assert
        using var archive = FusionArchive.Create(stream);
        var formats = await archive.GetSupportedGatewayFormatsAsync();
        Assert.Empty(formats);
    }

    [Fact]
    public async Task GetSourceSchemaNames_WithoutMetadata_ReturnsEmpty()
    {
        // Arrange
        await using var stream = CreateStream();

        // Act & Assert
        using var archive = FusionArchive.Create(stream);
        var names = await archive.GetSourceSchemaNamesAsync();
        Assert.Empty(names);
    }

    private Stream CreateStream()
    {
        var stream = new MemoryStream();
        _streamsToDispose.Add(stream);
        return stream;
    }

    private ArchiveMetadata CreateTestMetadata()
    {
        return new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("2.0.0"), new Version("2.1.0")],
            SourceSchemas = ["user-service", "product-service"]
        };
    }

    private JsonDocument CreateSettingsJson()
    {
        return JsonDocument.Parse("{ }");
    }

    private X509Certificate2 CreateTestCertificate()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

        _certificatesToDispose.Add(cert);
        return cert;
    }

    public void Dispose()
    {
        foreach (var stream in _streamsToDispose)
        {
            stream.Dispose();
        }

        foreach (var cert in _certificatesToDispose)
        {
            cert.Dispose();
        }
    }
}
