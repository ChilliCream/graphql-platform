using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Commands.Fusion;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Packaging;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

internal static class LegacyArchiveE2EFixtures
{
    private const string LatestVersion = "2.0.0";
    private const string LegacyVersion = "1.0.0";
    private const string EmbeddedLegacyFileName = "legacy-v1-archive.fgp";

    private static readonly string s_gatewayFarPath = Path.Combine(
        AppContext.BaseDirectory,
        "__resources__",
        "fusion-archives",
        "gateway.far");

    public static byte[] ReadGatewayFarBytes()
    {
        return File.ReadAllBytes(s_gatewayFarPath);
    }

    public static async Task<byte[]> BuildV2ArchiveWithEmbeddedLegacyAsync(byte[] embeddedLegacyBytes)
    {
        var farBytes = ReadGatewayFarBytes();

        var workStream = new MemoryStream();
        workStream.Write(farBytes, 0, farBytes.Length);
        workStream.Position = 0;

        {
            using var archive = FusionArchive.Open(
                workStream,
                mode: FusionArchiveMode.Update,
                leaveOpen: true);

            await using var embedded = new MemoryStream(embeddedLegacyBytes);
            await archive.SetFileAsync(EmbeddedLegacyFileName, embedded);
            await archive.CommitAsync();
        }

        return workStream.ToArray();
    }

    public static async Task<byte[]> BuildLegacyV1BytesAsync(bool alternate = false)
    {
        await using var buffer = alternate
            ? await LegacyFusionArchiveFixtures.CreateNoSettingsAsync()
            : await LegacyFusionArchiveFixtures.CreateMultiSubgraphAsync();

        return buffer.ToArray();
    }

    public static void SetupV2Download(
        Mock<IFusionConfigurationClient> clientMock,
        string apiId,
        string stageName,
        byte[] v2Bytes)
    {
        clientMock
            .Setup(x => x.DownloadLatestFusionArchiveAsync(
                apiId,
                stageName,
                LatestVersion,
                ArchiveFormats.Far,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(v2Bytes));
    }

    public static void SetupNoV2Download(
        Mock<IFusionConfigurationClient> clientMock,
        string apiId,
        string stageName)
    {
        clientMock
            .Setup(x => x.DownloadLatestFusionArchiveAsync(
                apiId,
                stageName,
                LatestVersion,
                ArchiveFormats.Far,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => null);
    }

    public static void SetupV1Download(
        Mock<IFusionConfigurationClient> clientMock,
        string apiId,
        string stageName,
        byte[] v1Bytes)
    {
        clientMock
            .Setup(x => x.DownloadLatestFusionArchiveAsync(
                apiId,
                stageName,
                LegacyVersion,
                ArchiveFormats.Fgp,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(v1Bytes));
    }

    public static void SetupNoV1Download(
        Mock<IFusionConfigurationClient> clientMock,
        string apiId,
        string stageName)
    {
        clientMock
            .Setup(x => x.DownloadLatestFusionArchiveAsync(
                apiId,
                stageName,
                LegacyVersion,
                ArchiveFormats.Fgp,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => null);
    }

    public static async Task<byte[]?> ReadEmbeddedLegacyAsync(FusionArchive archive)
    {
        await using var stream = await archive.GetFileAsync(EmbeddedLegacyFileName);
        if (stream is null)
        {
            return null;
        }

        var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }
}
