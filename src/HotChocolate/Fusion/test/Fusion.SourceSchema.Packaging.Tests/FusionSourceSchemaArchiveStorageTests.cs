using System.IO.Compression;
using System.Text.Json;

namespace HotChocolate.Fusion.SourceSchema.Packaging;

public sealed class FusionSourceSchemaArchiveStorageTests
{
    [Fact]
    public async Task StreamCreateAndOpen_Should_NotCreateTempFiles()
    {
        var before = SnapshotFusionTempFiles();
        await using var stream = new MemoryStream();

        using (var archive = FusionSourceSchemaArchive.Create(stream, leaveOpen: true))
        {
            await SetRequiredEntriesAsync(archive);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        stream.Position = 0;
        using (var archive = FusionSourceSchemaArchive.Open(stream, leaveOpen: true))
        {
            Assert.NotNull(await archive.GetArchiveMetadataAsync(
                TestContext.Current.CancellationToken));
            Assert.NotNull(await archive.TryGetSchemaAsync(
                TestContext.Current.CancellationToken));
            Assert.NotNull(await archive.TryGetSettingsAsync(
                TestContext.Current.CancellationToken));
        }

        Assert.Equal(before, SnapshotFusionTempFiles());
    }

    [Fact]
    public async Task PathOpen_Should_CleanTempFile_WhenExtractionIsCanceled()
    {
        var archivePath = System.IO.Path.GetTempFileName();

        try
        {
            CreateZipEntry(archivePath, "schema.graphqls", "type Query { field: String }");
            var before = SnapshotFusionTempFiles();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            using (var archive = FusionSourceSchemaArchive.Open(archivePath))
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => archive.TryGetSchemaAsync(cancellation.Token));
            }

            Assert.Equal(before, SnapshotFusionTempFiles());
        }
        finally
        {
            File.Delete(archivePath);
        }
    }

    [Fact]
    public async Task PathOpen_Should_CleanTempFile_WhenEntryIsOversized()
    {
        var archivePath = System.IO.Path.GetTempFileName();

        try
        {
            CreateZipEntry(archivePath, "schema-settings.json", new string('a', 512_001));
            var before = SnapshotFusionTempFiles();

            using (var archive = FusionSourceSchemaArchive.Open(archivePath))
            {
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => archive.TryGetSettingsAsync(TestContext.Current.CancellationToken));
            }

            Assert.Equal(before, SnapshotFusionTempFiles());
        }
        finally
        {
            File.Delete(archivePath);
        }
    }

    [Fact]
    public async Task StreamCreate_Should_EnforceEntrySizeIncrementally()
    {
        await using var stream = new MemoryStream();
        using var archive = FusionSourceSchemaArchive.Create(
            stream,
            new FusionSourceSchemaArchiveOptions
            {
                MaxAllowedSchemaSize = 4,
                MaxAllowedInMemorySessionSize = 100
            });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.SetSchemaAsync(
                "12345"u8.ToArray(),
                TestContext.Current.CancellationToken));

        Assert.Equal("File is too large and exceeds the allowed size of 4.", exception.Message);
    }

    [Fact]
    public async Task StreamCreate_Should_EnforceTotalSessionSizeIncrementally()
    {
        await using var stream = new MemoryStream();
        using var archive = FusionSourceSchemaArchive.Create(
            stream,
            new FusionSourceSchemaArchiveOptions
            {
                MaxAllowedSchemaSize = 10,
                MaxAllowedInMemorySessionSize = 6
            });
        await archive.SetSchemaAsync("12345"u8.ToArray(), TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.SetSchemaExtensionsAsync(
                "12"u8.ToArray(),
                TestContext.Current.CancellationToken));

        Assert.Equal("File is too large and exceeds the allowed size of 1.", exception.Message);
    }

    [Fact]
    public async Task MemoryAndTempStorage_Should_CreateIdenticalArchives()
    {
        var archivePath = System.IO.Path.GetTempFileName();

        try
        {
            using (var archive = FusionSourceSchemaArchive.Create(archivePath))
            {
                await SetRequiredEntriesAsync(archive);
                await archive.CommitAsync(TestContext.Current.CancellationToken);
            }

            await using var stream = new MemoryStream();
            using (var archive = FusionSourceSchemaArchive.Create(stream, leaveOpen: true))
            {
                await SetRequiredEntriesAsync(archive);
                await archive.CommitAsync(TestContext.Current.CancellationToken);
            }

            Assert.Equal(
                await File.ReadAllBytesAsync(
                    archivePath,
                    TestContext.Current.CancellationToken),
                stream.ToArray());
        }
        finally
        {
            File.Delete(archivePath);
        }
    }

    private static async Task SetRequiredEntriesAsync(FusionSourceSchemaArchive archive)
    {
        await archive.SetArchiveMetadataAsync(
            new ArchiveMetadata { FormatVersion = new Version(2, 0) },
            TestContext.Current.CancellationToken);
        await archive.SetSchemaAsync(
            "type Query { field: String }"u8.ToArray(),
            TestContext.Current.CancellationToken);
        using var settings = JsonDocument.Parse("{}");
        await archive.SetSettingsAsync(settings, TestContext.Current.CancellationToken);
    }

    private static string[] SnapshotFusionTempFiles()
        => Directory
            .EnumerateFiles(
                System.IO.Path.GetTempPath(),
                $"hotchocolate-fusion-{Environment.ProcessId}-*.tmp")
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static void CreateZipEntry(string archivePath, string entryName, string content)
    {
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update);
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }
}
