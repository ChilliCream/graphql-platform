using System.IO.Compression;
using System.Text;

namespace HotChocolate.Fusion.Packaging;

public class FusionArchiveRegoDataTests
{
    private static readonly Version s_version = new("1.0.0");

    [Fact]
    public async Task SetAndGetRegoData_Should_RoundTrip_When_RootAndNestedMounts()
    {
        // arrange
        var root = """{ "version": 1 }"""u8.ToArray();
        var roles = """{ "admin": ["product:read"] }"""u8.ToArray();
        var tenant = """{ "name": "Acme" }"""u8.ToArray();
        await using var stream = new MemoryStream();

        // act
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetRegoDataAsync("", root, s_version, TestContext.Current.CancellationToken);
        await archive.SetRegoDataAsync("roles", roles, s_version, TestContext.Current.CancellationToken);
        await archive.SetRegoDataAsync("tenants/acme", tenant, s_version, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(root, await GetAsync(archive, ""));
        Assert.Equal(roles, await GetAsync(archive, "roles"));
        Assert.Equal(tenant, await GetAsync(archive, "tenants/acme"));

        static async Task<byte[]> GetAsync(FusionArchive archive, string mountPath)
        {
            var data = await archive.TryGetRegoDataAsync(mountPath, s_version, TestContext.Current.CancellationToken);
            return data!.Value.ToArray();
        }
    }

    [Fact]
    public async Task SetRegoData_Should_WriteExpectedPaths_When_RootAndNestedMounts()
    {
        // arrange
        await using var stream = new MemoryStream();

        // act
        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetRegoDataAsync("", "{ }"u8.ToArray(), s_version, TestContext.Current.CancellationToken);
            await archive.SetRegoDataAsync("roles", "{ }"u8.ToArray(), s_version, TestContext.Current.CancellationToken);
            await archive.SetRegoDataAsync(
                "tenants/acme",
                "{ }"u8.ToArray(),
                s_version,
                TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        // assert
        stream.Position = 0;
#if NET10_0_OR_GREATER
        await using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
#else
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
#endif
        Assert.Equal(
            [
                "policies/rego/1.0.0/data/data.json",
                "policies/rego/1.0.0/data/roles/data.json",
                "policies/rego/1.0.0/data/tenants/acme/data.json"
            ],
            zip.Entries
                .Select(entry => entry.FullName)
                .Where(name => name.StartsWith("policies/", StringComparison.Ordinal))
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task GetRegoDataDocument_Should_MergeMounts_When_MultipleMountsExist()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetRegoDataAsync(
            "",
            """{ "version": 1 }"""u8.ToArray(),
            s_version,
            TestContext.Current.CancellationToken);
        await archive.SetRegoDataAsync(
            "roles",
            """{ "admin": ["product:read"] }"""u8.ToArray(),
            s_version,
            TestContext.Current.CancellationToken);
        await archive.SetRegoDataAsync(
            "tenants/acme",
            """{ "name": "Acme" }"""u8.ToArray(),
            s_version,
            TestContext.Current.CancellationToken);

        // act
        using var owner = await archive.TryGetRegoDataDocumentAsync(s_version, TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(owner);
        var document = owner.Document.RootElement;
        Assert.Equal(1, document.GetProperty("version").GetInt32());
        Assert.Equal("product:read", document.GetProperty("roles").GetProperty("admin")[0].GetString());
        Assert.Equal("Acme", document.GetProperty("tenants").GetProperty("acme").GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetRegoDataDocument_Should_ReturnNull_When_NoDataSubtreeExists()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);

        // act
        var owner = await archive.TryGetRegoDataDocumentAsync(s_version, TestContext.Current.CancellationToken);

        // assert
        Assert.Null(owner);
    }

    [Fact]
    public async Task SetRegoData_Should_Throw_When_ContentIsNotJsonObject()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);

        // act & assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => archive.SetRegoDataAsync(
                "",
                "[1, 2, 3]"u8.ToArray(),
                s_version,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetRegoData_Should_Throw_When_AncestorAlreadyDefinesDescendantKey()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetRegoDataAsync(
            "",
            """{ "roles": { "admin": [] } }"""u8.ToArray(),
            s_version,
            TestContext.Current.CancellationToken);

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.SetRegoDataAsync(
                "roles",
                """{ "customer": [] }"""u8.ToArray(),
                s_version,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetRegoData_Should_Throw_When_DescendantMountExistsBeforeAncestor()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetRegoDataAsync(
            "roles",
            """{ "customer": [] }"""u8.ToArray(),
            s_version,
            TestContext.Current.CancellationToken);

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.SetRegoDataAsync(
                "",
                """{ "roles": { "admin": [] } }"""u8.ToArray(),
                s_version,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetRegoData_Should_Throw_When_AncestorDefinesNonObjectAlongDescendantPath()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetRegoDataAsync(
            "",
            """{ "a": 5 }"""u8.ToArray(),
            s_version,
            TestContext.Current.CancellationToken);

        // act & assert
        // The root mount defines 'a' as a number, so a descendant mount cannot descend through it.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.SetRegoDataAsync(
                "a/b",
                """{ "value": 1 }"""u8.ToArray(),
                s_version,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetRegoData_Should_Throw_When_NewAncestorDefinesNonObjectOverDescendant()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetRegoDataAsync(
            "a/b",
            """{ "value": 1 }"""u8.ToArray(),
            s_version,
            TestContext.Current.CancellationToken);

        // act & assert
        // The new root mount defines 'a' as a number where the existing descendant mount lives.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.SetRegoDataAsync(
                "",
                """{ "a": 5 }"""u8.ToArray(),
                s_version,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetRegoDataDocument_Should_Throw_When_AncestorMountDefinesDescendantKey()
    {
        // arrange
        // The write API would reject this layout; an externally produced archive must be rejected on read too.
        await using var stream = CreateArchive(
            ("policies/rego/1.0.0/data/data.json", """{ "roles": { "admin": [] } }"""),
            ("policies/rego/1.0.0/data/roles/data.json", """{ "customer": [] }"""));
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        // act & assert
        await Assert.ThrowsAsync<InvalidDataException>(
            () => archive.TryGetRegoDataDocumentAsync(s_version, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetRegoDataDocument_Should_Throw_When_AncestorDefinesNonObjectAlongDescendantPath()
    {
        // arrange
        await using var stream = CreateArchive(
            ("policies/rego/1.0.0/data/data.json", """{ "a": 5 }"""),
            ("policies/rego/1.0.0/data/a/b/data.json", """{ "value": 1 }"""));
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        // act & assert
        await Assert.ThrowsAsync<InvalidDataException>(
            () => archive.TryGetRegoDataDocumentAsync(s_version, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetRegoData_Should_Succeed_When_AncestorDefinesDisjointKey()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetRegoDataAsync(
            "",
            """{ "tenants": { "beta": {} } }"""u8.ToArray(),
            s_version,
            TestContext.Current.CancellationToken);

        // act
        await archive.SetRegoDataAsync(
            "tenants/acme",
            """{ "name": "Acme" }"""u8.ToArray(),
            s_version,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(["", "tenants/acme"], archive.GetRegoDataMountPaths(s_version));
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("roles/..")]
    [InlineData("a//b")]
    [InlineData("/roles")]
    [InlineData("roles/")]
    [InlineData("roles\\admin")]
    public async Task SetRegoData_Should_Throw_When_MountPathSegmentIsInvalid(string mountPath)
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);

        // act & assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => archive.SetRegoDataAsync(
                mountPath,
                "{ }"u8.ToArray(),
                s_version,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetRegoDataMountPaths_Should_EnumerateWritten_When_MultipleMountsExist()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetRegoDataAsync("", "{ }"u8.ToArray(), s_version, TestContext.Current.CancellationToken);
        await archive.SetRegoDataAsync("roles", "{ }"u8.ToArray(), s_version, TestContext.Current.CancellationToken);
        await archive.SetRegoDataAsync(
            "tenants/acme",
            "{ }"u8.ToArray(),
            s_version,
            TestContext.Current.CancellationToken);

        // act
        var mountPaths = archive.GetRegoDataMountPaths(s_version);

        // assert
        Assert.Equal(["", "roles", "tenants/acme"], mountPaths);
    }

    [Fact]
    public async Task RemoveRegoData_Should_RemoveMount_When_MountExists()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetRegoDataAsync("", "{ }"u8.ToArray(), s_version, TestContext.Current.CancellationToken);
        await archive.SetRegoDataAsync("roles", "{ }"u8.ToArray(), s_version, TestContext.Current.CancellationToken);

        // act
        var removed = await archive.RemoveRegoDataAsync("roles", s_version, TestContext.Current.CancellationToken);

        // assert
        Assert.True(removed);
        Assert.Null(await archive.TryGetRegoDataAsync("roles", s_version, TestContext.Current.CancellationToken));
        Assert.Equal([""], archive.GetRegoDataMountPaths(s_version));
    }

    [Fact]
    public async Task TryGetRegoData_Should_ReturnNull_When_MountIsAbsent()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);

        // act
        var data = await archive.TryGetRegoDataAsync("roles", s_version, TestContext.Current.CancellationToken);

        // assert
        Assert.Null(data);
    }

    [Fact]
    public async Task GetRegoDataMountPaths_Should_Throw_When_NonDataJsonFileIsUnderDataSubtree()
    {
        // arrange
        await using var stream = CreateArchive(
            ("policies/rego/1.0.0/data/roles/roles.json", "{ }"));
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        // act & assert
        Assert.Throws<InvalidDataException>(() => archive.GetRegoDataMountPaths(s_version));
    }

    [Fact]
    public async Task GetSupportedRegoPolicyFormats_Should_Throw_When_DataJsonIsOutsideDataSubtree()
    {
        // arrange
        await using var stream = CreateArchive(
            ("policies/rego/1.0.0/data.json", "{ }"));
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        // act & assert
        Assert.Throws<InvalidDataException>(() => archive.GetSupportedRegoPolicyFormats());
    }

    [Fact]
    public async Task SetRegoData_Should_Throw_When_MountEqualsPolicyPackageVirtualRoot()
    {
        // arrange
        // The policy declares 'package CanReadProduct', so its rules form the virtual document
        // rooted at data.CanReadProduct, which a data mount at the same path must not overlap.
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetRegoPolicyAsync(
            "CanReadProduct",
            "package CanReadProduct\nallow := true"u8.ToArray(),
            "fragment R on Product { id }"u8.ToArray(),
            s_version,
            TestContext.Current.CancellationToken);

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.SetRegoDataAsync(
                "CanReadProduct",
                "{ }"u8.ToArray(),
                s_version,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetRegoData_Should_Throw_When_RootMountDefinesPolicyPackageVirtualRoot()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetRegoPolicyAsync(
            "CanReadProduct",
            "package authz\nallow := true"u8.ToArray(),
            "fragment R on Product { id }"u8.ToArray(),
            s_version,
            TestContext.Current.CancellationToken);

        // act & assert
        // The root data document defines the key 'authz', which is the policy package's virtual root.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.SetRegoDataAsync(
                "",
                """{ "authz": { "seed": 1 } }"""u8.ToArray(),
                s_version,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetRegoPolicy_Should_Throw_When_PackageOverlapsExistingDataMount()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetRegoDataAsync(
            "authz",
            "{ }"u8.ToArray(),
            s_version,
            TestContext.Current.CancellationToken);

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.SetRegoPolicyAsync(
                "CanReadProduct",
                "package authz\nallow := true"u8.ToArray(),
                "fragment R on Product { id }"u8.ToArray(),
                s_version,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetRegoPolicyAndData_Should_Succeed_When_PackageAndMountAreDisjoint()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetRegoPolicyAsync(
            "CanReadProduct",
            "package authz\nallow := true"u8.ToArray(),
            "fragment R on Product { id }"u8.ToArray(),
            s_version,
            TestContext.Current.CancellationToken);

        // act
        await archive.SetRegoDataAsync(
            "roles",
            """{ "admin": [] }"""u8.ToArray(),
            s_version,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(["roles"], archive.GetRegoDataMountPaths(s_version));
    }

    [Fact]
    public async Task GetRegoDataDocument_Should_Throw_When_MountOverlapsPolicyPackageVirtualRoot()
    {
        // arrange
        // The write API would reject this layout; an externally produced archive must be rejected on read too.
        await using var stream = CreateArchive(
            ("policies/rego/1.0.0/CanReadProduct.rego", "package CanReadProduct\nallow := true"),
            ("policies/rego/1.0.0/CanReadProduct.graphql", "fragment R on Product { id }"),
            ("policies/rego/1.0.0/data/data.json", """{ "CanReadProduct": { "seed": 1 } }"""));
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        // act & assert
        await Assert.ThrowsAsync<InvalidDataException>(
            () => archive.TryGetRegoDataDocumentAsync(s_version, TestContext.Current.CancellationToken));
    }

    private static MemoryStream CreateArchive(params (string Path, string Content)[] files)
    {
        var stream = new MemoryStream();

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.Path);
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8, leaveOpen: false);
                writer.Write(file.Content);
            }
        }

        stream.Position = 0;
        return stream;
    }
}
