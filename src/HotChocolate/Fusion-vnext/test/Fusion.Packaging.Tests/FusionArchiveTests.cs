using System.Buffers;
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
        Assert.Throws<ArgumentNullException>(() => FusionArchive.Open(null!));
    }

    [Fact]
    public async Task SetArchiveMetadata_WithValidData_StoresCorrectly()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = new ArchiveMetadata
        {
            FormatVersion = new Version("1.0.0"),
            SupportedGatewayFormats = [new Version("2.0.0"), new Version("2.1.0")],
            SourceSchemas = ["user-service", "product-service"]
        };

        // Act
        await archive.SetArchiveMetadataAsync(metadata);

        // Assert
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
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        // Act
        var result = await archive.GetArchiveMetadataAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetArchiveMetadata_WithNullMetadata_ThrowsArgumentNullException()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            archive.SetArchiveMetadataAsync(null!));
    }

    [Fact]
    public async Task GetLatestSupportedGatewayFormat_WithValidMetadata_ReturnsHighestVersion()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("1.0.0"), new Version("2.1.0"), new Version("2.0.0")],
            SourceSchemas = ["test-service"]
        };

        await archive.SetArchiveMetadataAsync(metadata);

        // Act
        var latest = await archive.GetLatestSupportedGatewayFormatAsync();

        // Assert
        Assert.Equal(new Version("2.1.0"), latest);
    }

    [Fact]
    public async Task GetLatestSupportedGatewayFormat_WithoutMetadata_ThrowsInvalidOperationException()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            archive.GetLatestSupportedGatewayFormatAsync());
    }

    [Fact]
    public async Task SetCompositionSettings_WithValidJsonDocument_StoresCorrectly()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var settingsJson = """{"enableNodeSpec": true, "maxDepth": 10}""";
        using var settings = JsonDocument.Parse(settingsJson);

        // Act
        await archive.SetCompositionSettingsAsync(settings);

        // Assert
        using var retrieved = await archive.GetCompositionSettingsAsync();
        Assert.NotNull(retrieved);
        Assert.True(retrieved.RootElement.GetProperty("enableNodeSpec").GetBoolean());
        Assert.Equal(10, retrieved.RootElement.GetProperty("maxDepth").GetInt32());
    }

    [Fact]
    public async Task GetCompositionSettings_WhenNotSet_ReturnsNull()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        // Act
        var result = await archive.GetCompositionSettingsAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetGatewaySchema_WithStringContent_StoresCorrectly()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);

        var schema = "type Query { hello: String }";
        var version = new Version("2.0.0");

        // Act
        await archive.SetGatewaySchemaAsync(schema, version);

        // Assert
        var buffer = new ArrayBufferWriter<byte>();
        var result = await archive.TryGetGatewaySchemaAsync(version, buffer);

        Assert.True(result.IsResolved);
        Assert.Equal(version, result.ActualVersion);

        var retrievedSchema = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal(schema, retrievedSchema);
    }

    [Fact]
    public async Task SetGatewaySchema_WithByteContent_StoresCorrectly()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);

        var schema = "type Query { hello: String }"u8.ToArray();
        var version = new Version("2.0.0");

        // Act
        await archive.SetGatewaySchemaAsync(schema, version);

        // Assert
        var buffer = new ArrayBufferWriter<byte>();
        var result = await archive.TryGetGatewaySchemaAsync(version, buffer);

        Assert.True(result.IsResolved);
        Assert.Equal(version, result.ActualVersion);
        Assert.True(schema.AsSpan().SequenceEqual(buffer.WrittenSpan));
    }

    [Fact]
    public async Task SetGatewaySchema_WithoutMetadata_ThrowsInvalidOperationException()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            archive.SetGatewaySchemaAsync("schema", new Version("1.0.0")));
    }

    [Fact]
    public async Task SetGatewaySchema_WithUnsupportedVersion_ThrowsInvalidOperationException()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            archive.SetGatewaySchemaAsync("schema", new Version("3.0.0")));
    }

    [Fact]
    public async Task TryGetGatewaySchema_WithCompatibleVersion_ReturnsCorrectVersion()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("1.0.0"), new Version("2.0.0"), new Version("2.1.0")],
            SourceSchemas = ["test-service"]
        };
        await archive.SetArchiveMetadataAsync(metadata);

        await archive.SetGatewaySchemaAsync("schema v1.0", new Version("1.0.0"));
        await archive.SetGatewaySchemaAsync("schema v2.0", new Version("2.0.0"));
        await archive.SetGatewaySchemaAsync("schema v2.1", new Version("2.1.0"));

        // Act - Request max version 2.0.0, should get 2.0.0
        var buffer = new ArrayBufferWriter<byte>();
        var result = await archive.TryGetGatewaySchemaAsync(new Version("2.0.0"), buffer);

        // Assert
        Assert.True(result.IsResolved);
        Assert.Equal(new Version("2.0.0"), result.ActualVersion);

        var schema = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("schema v2.0", schema);
    }

    [Fact]
    public async Task TryGetGatewaySchema_WithIncompatibleVersion_ReturnsFalse()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("2.0.0")],
            SourceSchemas = ["test-service"]
        };
        await archive.SetArchiveMetadataAsync(metadata);

        // Act
        var buffer = new ArrayBufferWriter<byte>();
        var result = await archive.TryGetGatewaySchemaAsync(new Version("1.0.0"), buffer);

        // Assert
        Assert.False(result.IsResolved);
        Assert.Null(result.ActualVersion);
    }

    [Fact]
    public async Task SetGatewaySettings_WithValidSettings_StoresCorrectly()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);

        var settingsJson = """
        {
            "transportProfiles": {
                "http-profile": {
                    "type": "graphql-over-http"
                }
            }
        }
        """;
        using var settings = JsonDocument.Parse(settingsJson);
        var version = new Version("2.0.0");

        // Act
        await archive.SetGatewaySettingsAsync(settings, version);

        // Assert
        var result = await archive.TryGetGatewaySettingsAsync(version);
        Assert.True(result.IsResolved);
        Assert.Equal(version, result.ActualVersion);
        Assert.NotNull(result.Settings);

        var transportProfiles = result.Settings.RootElement.GetProperty("transportProfiles");
        Assert.True(transportProfiles.TryGetProperty("http-profile", out var profile));
        Assert.Equal("graphql-over-http", profile.GetProperty("type").GetString());
    }

    [Fact]
    public async Task AddSourceSchema_WithValidSchema_StoresCorrectly()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);

        var schemaContent = "type User { id: ID! name: String! }"u8.ToArray();
        var schemaName = "user-service";

        // Act
        await archive.AddSourceSchemaAsync(schemaName, schemaContent);

        // Assert
        var buffer = new ArrayBufferWriter<byte>();
        var found = await archive.TryGetSourceSchemaAsync(schemaName, buffer);

        Assert.True(found);
        Assert.True(schemaContent.AsSpan().SequenceEqual(buffer.WrittenSpan));
    }

    [Fact]
    public async Task AddSourceSchema_WithInvalidSchemaName_ThrowsArgumentException()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            archive.AddSourceSchemaAsync("invalid name!", "schema"u8.ToArray()));
    }

    [Fact]
    public async Task AddSourceSchema_WithUndeclaredSchemaName_ThrowsInvalidOperationException()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("2.0.0")],
            SourceSchemas = ["declared-schema"]
        };
        await archive.SetArchiveMetadataAsync(metadata);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            archive.AddSourceSchemaAsync("undeclared-schema", "schema"u8.ToArray()));
    }

    [Fact]
    public async Task TryGetSourceSchema_WithNonExistentSchema_ReturnsFalse()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        // Act
        var buffer = new ArrayBufferWriter<byte>();
        var found = await archive.TryGetSourceSchemaAsync("non-existent", buffer);

        // Assert
        Assert.False(found);
    }

    [Fact]
    public async Task SignArchive_WithValidCertificate_CreatesSignature()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.SetGatewaySchemaAsync("schema", new Version("2.0.0"));

        using var cert = CreateTestCertificate();

        // Act
        await archive.SignArchiveAsync(cert);

        // Assert
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
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        using var cert = CreateTestCertificate();
        using var publicOnlyCert = new X509Certificate2(cert.Export(X509ContentType.Cert));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            archive.SignArchiveAsync(publicOnlyCert));
    }

    [Fact]
    public async Task VerifySignature_WithValidSignature_ReturnsValid()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.SetGatewaySchemaAsync("schema", new Version("2.0.0"));

        using var cert = CreateTestCertificate();
        await archive.SignArchiveAsync(cert);

        // Act
        var result = await archive.VerifySignatureAsync(cert);

        // Assert
        Assert.Equal(SignatureVerificationResult.Valid, result);
    }

    [Fact]
    public async Task VerifySignature_WithUnsignedArchive_ReturnsNotSigned()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        using var cert = CreateTestCertificate();

        // Act
        var result = await archive.VerifySignatureAsync(cert);

        // Assert
        Assert.Equal(SignatureVerificationResult.NotSigned, result);
    }

    [Fact]
    public async Task VerifySignature_WithModifiedFile_ReturnsFilesModified()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.SetGatewaySchemaAsync("original schema", new Version("2.0.0"));

        using var cert = CreateTestCertificate();
        await archive.SignArchiveAsync(cert);

        // Modify the file after signing
        await archive.SetGatewaySchemaAsync("modified schema", new Version("2.0.0"));

        // Act
        var result = await archive.VerifySignatureAsync(cert);

        // Assert
        Assert.Equal(SignatureVerificationResult.FilesModified, result);
    }

    [Fact]
    public async Task GetSourceSchemaNames_WithMetadata_ReturnsOrderedNames()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("2.0.0")],
            SourceSchemas = ["zebra-service", "alpha-service", "beta-service"]
        };
        await archive.SetArchiveMetadataAsync(metadata);

        // Act
        var names = await archive.GetSourceSchemaNamesAsync();

        // Assert
        Assert.Equal(["alpha-service", "beta-service", "zebra-service"], names);
    }

    [Fact]
    public async Task GetSupportedGatewayFormats_WithMetadata_ReturnsDescendingOrder()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("1.0.0"), new Version("2.1.0"), new Version("2.0.0")],
            SourceSchemas = ["test-service"]
        };
        await archive.SetArchiveMetadataAsync(metadata);

        // Act
        var versions = await archive.GetSupportedGatewayFormatsAsync();

        // Assert
        Assert.Equal([new Version("2.1.0"), new Version("2.0.0"), new Version("1.0.0")], versions);
    }

    [Fact]
    public async Task OverwriteEntry_WhenSettingSameFileTwice_OverwritesCorrectly()
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = CreateTestMetadata();
        await archive.SetArchiveMetadataAsync(metadata);

        // Act - Set schema twice
        await archive.SetGatewaySchemaAsync("first schema", new Version("2.0.0"));
        await archive.SetGatewaySchemaAsync("second schema", new Version("2.0.0"));

        // Assert
        var buffer = new ArrayBufferWriter<byte>();
        var result = await archive.TryGetGatewaySchemaAsync(new Version("2.0.0"), buffer);

        Assert.True(result.IsResolved);
        var schema = Encoding.UTF8.GetString(buffer.WrittenSpan);
        Assert.Equal("second schema", schema);
    }

    [Theory]
    [InlineData("valid-schema")]
    [InlineData("Valid_Schema")]
    [InlineData("schema123")]
    [InlineData("_schema")]
    public async Task AddSourceSchema_WithValidSchemaNames_Succeeds(string schemaName)
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("2.0.0")],
            SourceSchemas = [schemaName]
        };
        await archive.SetArchiveMetadataAsync(metadata);

        // Act & Assert - Should not throw
        await archive.AddSourceSchemaAsync(schemaName, "schema"u8.ToArray());
    }

    [Theory]
    [InlineData("invalid name")]
    [InlineData("123invalid")]
    [InlineData("")]
    [InlineData("schema/name")]
    public async Task AddSourceSchema_WithInvalidSchemaNames_ThrowsException(string schemaName)
    {
        // Arrange
        using var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [new Version("2.0.0")],
            SourceSchemas = [schemaName]
        };
        await archive.SetArchiveMetadataAsync(metadata);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            archive.AddSourceSchemaAsync(schemaName, "schema"u8.ToArray()));
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
