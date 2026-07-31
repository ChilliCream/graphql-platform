using System.IO.Compression;
using System.Text.Json;

namespace HotChocolate.Fusion.Packaging;

public sealed class FusionArchiveStorageTests
{
    [Fact]
    public async Task StreamCreateAndOpen_Should_NotCreateTempFiles()
    {
        var before = SnapshotFusionTempFiles();
        await using var stream = new MemoryStream();

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await using var content = new MemoryStream("archive"u8.ToArray());
            await archive.SetLegacyArchiveFileAsync(content, TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        stream.Position = 0;
        using (var archive = FusionArchive.Open(stream, leaveOpen: true))
        {
            await using var content = await archive.TryGetLegacyArchiveFileAsync(
                TestContext.Current.CancellationToken);
            Assert.NotNull(content);
        }

        Assert.Equal(before, SnapshotFusionTempFiles());
    }

    [Fact]
    public async Task PathOpen_Should_CleanTempFile_WhenExtractionIsCanceled()
    {
        var archivePath = System.IO.Path.GetTempFileName();

        try
        {
            CreateZipEntry(archivePath, "legacy-v1-archive.fgp", "archive");
            var before = SnapshotFusionTempFiles();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            using (var archive = FusionArchive.Open(archivePath))
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => archive.TryGetLegacyArchiveFileAsync(cancellation.Token));
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
            CreateZipEntry(
                archivePath,
                "composition-settings.json",
                new string('a', 512_001));
            var before = SnapshotFusionTempFiles();

            using (var archive = FusionArchive.Open(archivePath))
            {
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => archive.GetCompositionSettingsAsync(TestContext.Current.CancellationToken));
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
        using var archive = FusionArchive.Create(
            stream,
            new FusionArchiveOptions
            {
                MaxAllowedLegacyArchiveSize = 4,
                MaxAllowedInMemorySessionSize = 100
            });
        await using var content = new MemoryStream("12345"u8.ToArray());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.SetLegacyArchiveFileAsync(content, TestContext.Current.CancellationToken));

        Assert.Equal("File is too large and exceeds the allowed size of 4.", exception.Message);
    }

    [Fact]
    public async Task StreamCreate_Should_EnforceTotalSessionSizeIncrementally()
    {
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(
            stream,
            new FusionArchiveOptions
            {
                MaxAllowedLegacyArchiveSize = 10,
                MaxAllowedSettingsSize = 10,
                MaxAllowedInMemorySessionSize = 6
            });
        await using var content = new MemoryStream("12345"u8.ToArray());
        await archive.SetLegacyArchiveFileAsync(content, TestContext.Current.CancellationToken);
        using var settings = JsonDocument.Parse("{}");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.SetCompositionSettingsAsync(
                settings,
                TestContext.Current.CancellationToken));

        Assert.Equal("File is too large and exceeds the allowed size of 1.", exception.Message);
    }

    [Fact]
    public async Task MemoryAndTempStorage_Should_CreateIdenticalArchives()
    {
        var archivePath = System.IO.Path.GetTempFileName();
        var content = "deterministic archive"u8.ToArray();

        try
        {
            using (var archive = FusionArchive.Create(archivePath))
            {
                await using var input = new MemoryStream(content);
                await archive.SetLegacyArchiveFileAsync(input, TestContext.Current.CancellationToken);
                await archive.CommitAsync(TestContext.Current.CancellationToken);
            }

            await using var stream = new MemoryStream();
            using (var archive = FusionArchive.Create(stream, leaveOpen: true))
            {
                await using var input = new MemoryStream(content);
                await archive.SetLegacyArchiveFileAsync(input, TestContext.Current.CancellationToken);
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
