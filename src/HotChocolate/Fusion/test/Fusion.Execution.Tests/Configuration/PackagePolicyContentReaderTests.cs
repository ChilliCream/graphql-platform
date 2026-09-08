using System.Text;
using HotChocolate.Fusion.Packaging;

namespace HotChocolate.Fusion.Configuration;

public class PackagePolicyContentReaderTests
{
    private static readonly Version s_version1 = new(1, 0, 0);
    private static readonly Version s_bundleVersion = new(2, 0, 0);

    private const string CartAllowSource =
        "package cart\n"
        + "import rego.v1\n"
        + "# METADATA\n"
        + "# entrypoint: true\n"
        + "default allow := false\n";

    [Fact]
    public async Task ReadAsync_Should_ReturnNull_When_ArchiveHasNoPolicies()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.CommitAsync(TestContext.Current.CancellationToken);

        // act
        var snapshot = await PackagePolicyContentReader.ReadAsync(
            archive, WellKnownVersions.LatestRegoPolicyFormatVersion, TestContext.Current.CancellationToken);

        // assert
        Assert.Null(snapshot);
    }

    [Fact]
    public async Task ReadAsync_Should_ReadBundle_When_ArchiveHasBundleFormat()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var stream = await BuildBundleArchiveAsync(ct);
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        // act
        var snapshot = await PackagePolicyContentReader.ReadAsync(archive, s_bundleVersion, ct);

        // assert
        Assert.NotNull(snapshot);
        Assert.Equal(s_bundleVersion, snapshot.FormatVersion);
        var policy = Assert.Single(snapshot.Policies);
        Assert.Equal("cart", policy.Name);
        Assert.Equal(CartAllowSource, Encoding.UTF8.GetString(policy.Source.Span));
        var library = Assert.Single(snapshot.Libraries);
        Assert.Equal("lib/rbac.rego", library.Name);
        snapshot.Dispose();
    }

    [Fact]
    public async Task ReadAsync_Should_SelectBundleFormat_When_ArchiveHasPairAndBundle()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var stream = new MemoryStream();

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetRegoPolicyAsync(
                "CanReadProduct",
                "package CanReadProduct"u8.ToArray(),
                "fragment Requirements on Product { id }"u8.ToArray(),
                s_version1,
                ct);
            await SetBundleAsync(archive, ct);
            await archive.CommitAsync(ct);
        }

        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);

        // act: a runtime supporting up to the bundle format picks the bundle over the pair.
        var atBundleMax = await PackagePolicyContentReader.ReadAsync(readArchive, s_bundleVersion, ct);

        // assert
        Assert.NotNull(atBundleMax);
        Assert.Equal(s_bundleVersion, atBundleMax.FormatVersion);
        atBundleMax.Dispose();

        // act: a runtime that only understands the pair format still reads it.
        var atPairMax = await PackagePolicyContentReader.ReadAsync(readArchive, s_version1, ct);

        // assert
        Assert.NotNull(atPairMax);
        Assert.Equal(s_version1, atPairMax.FormatVersion);
        Assert.Empty(atPairMax.Libraries);
        atPairMax.Dispose();
    }

    [Fact]
    public async Task ReadAsync_Should_ReadFlatPair_When_PairVersionIsTwoAndMaxVersionIsTwo()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var stream = new MemoryStream();

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetRegoPolicyAsync(
                "cart",
                Encoding.UTF8.GetBytes(CartAllowSource),
                "fragment Requirements on Cart { id }"u8.ToArray(),
                s_bundleVersion,
                ct);
            await archive.CommitAsync(ct);
        }

        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);

        // act
        var snapshot = await PackagePolicyContentReader.ReadAsync(
            readArchive, WellKnownVersions.LatestRegoPolicyFormatVersion, ct);

        // assert
        Assert.NotNull(snapshot);
        Assert.Equal(s_bundleVersion, snapshot.FormatVersion);
        var policy = Assert.Single(snapshot.Policies);
        Assert.Equal("cart", policy.Name);
        Assert.Empty(snapshot.Libraries);
        snapshot.Dispose();
    }

    [Fact]
    public async Task ReadAsync_Should_Throw_When_OnlyBundleFormatExceedsRuntimeMax()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var stream = await BuildBundleArchiveAsync(ct);
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        // act
        var read = () => PackagePolicyContentReader.ReadAsync(archive, s_version1, ct);

        // assert
        await Assert.ThrowsAsync<InvalidOperationException>(read);
    }

    private static async Task<MemoryStream> BuildBundleArchiveAsync(CancellationToken ct)
    {
        var stream = new MemoryStream();

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await SetBundleAsync(archive, ct);
            await archive.CommitAsync(ct);
        }

        stream.Position = 0;
        return stream;
    }

    private static Task SetBundleAsync(FusionArchive archive, CancellationToken ct)
        => archive.SetRegoPolicyBundleAsync(
            new RegoPolicyBundle
            {
                Packages =
                [
                    new RegoPolicyBundlePackage(
                        "cart",
                        [new RegoPolicyBundleModule("allow", Encoding.UTF8.GetBytes(CartAllowSource))],
                        Requirements: null)
                ],
                Libraries =
                [
                    new RegoPolicyBundleModule(
                        "rbac",
                        "package lib\nimport rego.v1\nis_admin(role) if role == \"admin\"\n"u8.ToArray())
                ]
            },
            s_bundleVersion,
            ct);
}
