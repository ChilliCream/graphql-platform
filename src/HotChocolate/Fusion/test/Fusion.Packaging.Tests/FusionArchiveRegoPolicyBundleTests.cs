using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;

namespace HotChocolate.Fusion.Packaging;

public class FusionArchiveRegoPolicyBundleTests
{
    private static readonly Version s_version1 = new("1.0.0");
    private static readonly Version s_bundleVersion = new("2.0.0");

    private const string CartAllowSource =
        "package cart\n"
        + "import rego.v1\n"
        + "# METADATA\n"
        + "# entrypoint: true\n"
        + "default allow := false\n";

    private const string OrdersAllowSource =
        "package orders\n"
        + "import rego.v1\n"
        + "# METADATA\n"
        + "# entrypoint: true\n"
        + "default allow := false\n";

    private const string RbacLibrarySource =
        "package rbac\nimport rego.v1\nis_admin(role) if role == \"admin\"\n";

    [Fact]
    public async Task GetRegoPolicyBundle_Should_RoundTripPackage_When_BundleIsValid()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var stream = await BuildArchiveAsync(FullBundle(), ct);
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        // act
        var content = await archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        var package = Assert.Single(content.Packages);
        Assert.Equal("cart", package.Package);
        Assert.Equal(CartAllowSource, Encoding.UTF8.GetString(package.Source.Span));
        Assert.Equal("{ id }", Encoding.UTF8.GetString(package.Requirements!.Value.Span));
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_RoundTripLibrary_When_BundleIsValid()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var stream = await BuildArchiveAsync(FullBundle(), ct);
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        // act
        var content = await archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        var library = Assert.Single(content.Libraries);
        Assert.Equal("lib/rbac.rego", library.Name);
        Assert.Equal(RbacLibrarySource, Encoding.UTF8.GetString(library.Source.Span));
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_RoundTripData_When_BundleIsValid()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var stream = await BuildArchiveAsync(FullBundle(), ct);
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        // act
        var content = await archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        Assert.NotNull(content.Data);
        Assert.Equal("""{"role":"admin"}""", Encoding.UTF8.GetString(content.Data!.Value.Span));
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_ReturnNullRequirements_When_PackageHasNone()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var bundle = new RegoPolicyBundle
        {
            Packages =
            [
                new RegoPolicyBundlePackage(
                    "cart",
                    [new RegoPolicyBundleModule("allow", Encoding.UTF8.GetBytes(CartAllowSource))],
                    Requirements: null)
            ]
        };

        // act
        await using var stream = await BuildArchiveAsync(bundle, ct);
        using var archive = FusionArchive.Open(stream, leaveOpen: true);
        var content = await archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        Assert.Null(content.Packages[0].Requirements);
        Assert.Null(content.Data);
        Assert.NotEmpty(content.DataDigest.ToArray());
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_ShareLibrary_When_TwoPackagesReferenceIt()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var bundle = new RegoPolicyBundle
        {
            Packages =
            [
                new RegoPolicyBundlePackage(
                    "cart",
                    [new RegoPolicyBundleModule("allow", Encoding.UTF8.GetBytes(CartAllowSource))],
                    Requirements: null),
                new RegoPolicyBundlePackage(
                    "orders",
                    [new RegoPolicyBundleModule("allow", Encoding.UTF8.GetBytes(OrdersAllowSource))],
                    Requirements: null)
            ],
            Libraries = [new RegoPolicyBundleModule("rbac", Encoding.UTF8.GetBytes(RbacLibrarySource))]
        };

        // act
        await using var stream = await BuildArchiveAsync(bundle, ct);
        using var archive = FusionArchive.Open(stream, leaveOpen: true);
        var content = await archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        Assert.Equal(2, content.Packages.Length);
        Assert.Single(content.Libraries);
        Assert.DoesNotContain(content.Packages, p => p.Package.Equals("rbac", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetSupportedRegoPolicyFormats_Should_ReturnBothVersions_When_ArchiveHasPairsAndBundle()
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
            await archive.SetRegoPolicyBundleAsync(
                new RegoPolicyBundle
                {
                    Packages =
                    [
                        new RegoPolicyBundlePackage(
                            "cart",
                            [new RegoPolicyBundleModule("allow", Encoding.UTF8.GetBytes(CartAllowSource))],
                            Requirements: null)
                    ]
                },
                s_bundleVersion,
                ct);
            await archive.CommitAsync(ct);
        }

        stream.Position = 0;

        // act
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var formats = readArchive.GetSupportedRegoPolicyFormats().ToArray();
        var v1Names = readArchive.GetRegoPolicyNames(s_version1).ToArray();
        var bundle = await readArchive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        Assert.Equal([s_bundleVersion, s_version1], formats);
        Assert.Equal(["CanReadProduct"], v1Names);
        Assert.Single(bundle.Packages);
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_Throw_When_ModuleHashMismatch()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var bundle = SinglePackageBundle();
        await using var stream = await BuildArchiveAsync(bundle, ct);
        await using var tampered = await ReplaceZipEntry(
            stream, "policies/rego/2.0.0/cart/allow.rego", Encoding.UTF8.GetBytes(CartAllowSource + "\n# tampered\n"));

        // act
        using var archive = FusionArchive.Open(tampered, leaveOpen: true);
        var read = () => archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        await Assert.ThrowsAsync<InvalidDataException>(read);
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_Throw_When_ModuleIsMissing()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var bundle = SinglePackageBundle();
        await using var stream = await BuildArchiveAsync(bundle, ct);
        await using var withMissingModule = DeleteZipEntry(stream, "policies/rego/2.0.0/cart/allow.rego");

        // act
        using var archive = FusionArchive.Open(withMissingModule, leaveOpen: true);
        var read = () => archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        await Assert.ThrowsAsync<InvalidDataException>(read);
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_Throw_When_ArchiveContainsUnlistedFile()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var bundle = SinglePackageBundle();
        await using var stream = await BuildArchiveAsync(bundle, ct);
        await using var withExtraFile = await AddZipEntry(
            stream, "policies/rego/2.0.0/cart/extra.rego", "package cart\nimport rego.v1\n"u8.ToArray());

        // act
        using var archive = FusionArchive.Open(withExtraFile, leaveOpen: true);
        var read = () => archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        await Assert.ThrowsAsync<InvalidDataException>(read);
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_Throw_When_ManifestListsDuplicatePolicyName()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var bundle = SinglePackageBundle();
        await using var stream = await BuildArchiveAsync(bundle, ct);
        await using var duplicated = await EditManifestAsync(
            stream,
            manifest =>
            {
                var policies = (JsonArray)manifest["policies"]!;
                policies.Add(JsonNode.Parse(policies[0]!.ToJsonString()));
            });

        // act
        using var archive = FusionArchive.Open(duplicated, leaveOpen: true);
        var read = () => archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        await Assert.ThrowsAsync<InvalidDataException>(read);
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_Throw_When_FormatVersionDoesNotMatchDirectory()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var bundle = SinglePackageBundle();
        await using var stream = await BuildArchiveAsync(bundle, ct);
        await using var edited = await EditManifestAsync(
            stream,
            manifest => manifest["formatVersion"] = 3);

        // act
        using var archive = FusionArchive.Open(edited, leaveOpen: true);
        var read = () => archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        await Assert.ThrowsAsync<InvalidDataException>(read);
    }

    [Fact]
    public async Task SetRegoPolicyBundleAsync_Should_Throw_When_PackageHasNoEntrypoint()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        var bundle = new RegoPolicyBundle
        {
            Packages =
            [
                new RegoPolicyBundlePackage(
                    "cart",
                    [new RegoPolicyBundleModule("allow", "package cart\nimport rego.v1\ndefault allow := false\n"u8.ToArray())],
                    Requirements: null)
            ]
        };

        // act
        var write = () => archive.SetRegoPolicyBundleAsync(bundle, s_bundleVersion, TestContext.Current.CancellationToken);

        // assert
        await Assert.ThrowsAsync<ArgumentException>(write);
    }

    [Fact]
    public async Task SetRegoPolicyBundleAsync_Should_Throw_When_PackageHasTwoEntrypointModules()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        var bundle = new RegoPolicyBundle
        {
            Packages =
            [
                new RegoPolicyBundlePackage(
                    "cart",
                    [
                        new RegoPolicyBundleModule("allow", Encoding.UTF8.GetBytes(CartAllowSource)),
                        new RegoPolicyBundleModule(
                            "deny",
                            Encoding.UTF8.GetBytes(
                                "package cart\nimport rego.v1\n# METADATA\n# entrypoint: true\n"
                                + "default deny := false\n"))
                    ],
                    Requirements: null)
            ]
        };

        // act
        var write = () => archive.SetRegoPolicyBundleAsync(bundle, s_bundleVersion, TestContext.Current.CancellationToken);

        // assert
        await Assert.ThrowsAsync<ArgumentException>(write);
    }

    [Fact]
    public async Task SetRegoPolicyBundleAsync_Should_Throw_When_ModuleDeclaresDifferentPackage()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        var bundle = new RegoPolicyBundle
        {
            Packages =
            [
                new RegoPolicyBundlePackage(
                    "cart",
                    [new RegoPolicyBundleModule("allow", "package other\nimport rego.v1\n"u8.ToArray())],
                    Requirements: null)
            ]
        };

        // act
        var write = () => archive.SetRegoPolicyBundleAsync(bundle, s_bundleVersion, TestContext.Current.CancellationToken);

        // assert
        await Assert.ThrowsAsync<ArgumentException>(write);
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_Throw_When_ArchiveContainsExtraDataMount()
    {
        // arrange: an extra flat data mount alongside a bundle can no longer be produced through the
        // writer API (SetRegoDataAsync rejects a bundle version), so it is injected directly into the
        // zip to simulate a tampered or hand-crafted archive.
        var ct = TestContext.Current.CancellationToken;
        var bundle = new RegoPolicyBundle
        {
            Packages =
            [
                new RegoPolicyBundlePackage(
                    "cart",
                    [new RegoPolicyBundleModule("allow", Encoding.UTF8.GetBytes(CartAllowSource))],
                    Requirements: null)
            ],
            Data = Encoding.UTF8.GetBytes("""{"role":"admin"}""")
        };
        await using var stream = await BuildArchiveAsync(bundle, ct);
        await using var withExtraMount = await AddZipEntry(
            stream, "policies/rego/2.0.0/data/roles/data.json", """{"admin":true}"""u8.ToArray());

        // act
        using var archive = FusionArchive.Open(withExtraMount, leaveOpen: true);
        var read = () => archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        await Assert.ThrowsAsync<InvalidDataException>(read);
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_Throw_When_DataDocumentIsNotListed()
    {
        // arrange: a root data document with no manifest.Data entry can no longer be produced through
        // the writer API (SetRegoDataAsync rejects a bundle version), so it is injected directly into
        // the zip to simulate a tampered or hand-crafted archive.
        var ct = TestContext.Current.CancellationToken;
        var bundle = SinglePackageBundle();
        await using var stream = await BuildArchiveAsync(bundle, ct);
        await using var withUnlistedData = await AddZipEntry(
            stream, "policies/rego/2.0.0/data/data.json", """{"role":"admin"}"""u8.ToArray());

        // act
        using var archive = FusionArchive.Open(withUnlistedData, leaveOpen: true);
        var read = () => archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        await Assert.ThrowsAsync<InvalidDataException>(read);
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_Throw_When_ManifestPolicyMissingRequiredProperty()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var bundle = SinglePackageBundle();
        await using var stream = await BuildArchiveAsync(bundle, ct);
        await using var tampered = await EditManifestAsync(
            stream,
            manifest =>
            {
                var policy = (JsonObject)((JsonArray)manifest["policies"]!)[0]!;
                policy.Remove("package");
            });

        // act
        using var archive = FusionArchive.Open(tampered, leaveOpen: true);
        var read = () => archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        await Assert.ThrowsAsync<InvalidDataException>(read);
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_Throw_When_ManifestHasDuplicateSha256Key()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var bundle = SinglePackageBundle();
        await using var stream = await BuildArchiveAsync(bundle, ct);
        await using var tampered = await ReplaceManifestTextAsync(stream, DuplicateSha256Entry);

        // act
        using var archive = FusionArchive.Open(tampered, leaveOpen: true);
        var read = () => archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        await Assert.ThrowsAsync<InvalidDataException>(read);
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_Throw_When_ManifestReferencesPathTraversal()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var bundle = SinglePackageBundle();
        await using var stream = await BuildArchiveAsync(bundle, ct);
        await using var tampered = await EditManifestAsync(
            stream,
            manifest =>
            {
                var policy = (JsonObject)((JsonArray)manifest["policies"]!)[0]!;
                ((JsonArray)policy["modules"]!)[0] = "../outside.rego";
            });

        // act
        using var archive = FusionArchive.Open(tampered, leaveOpen: true);
        var read = () => archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        await Assert.ThrowsAsync<InvalidDataException>(read);
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_Throw_When_TwoPackagesReferenceSameModulePath()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var bundle = new RegoPolicyBundle
        {
            Packages =
            [
                new RegoPolicyBundlePackage(
                    "cart",
                    [new RegoPolicyBundleModule("allow", Encoding.UTF8.GetBytes(CartAllowSource))],
                    Requirements: null),
                new RegoPolicyBundlePackage(
                    "orders",
                    [new RegoPolicyBundleModule("allow", Encoding.UTF8.GetBytes(OrdersAllowSource))],
                    Requirements: null)
            ]
        };
        await using var stream = await BuildArchiveAsync(bundle, ct);
        await using var tampered = await EditManifestAsync(
            stream,
            manifest =>
            {
                var policies = (JsonArray)manifest["policies"]!;
                var orders = (JsonObject)policies.Single(p => (string)p!["package"]! == "orders")!;
                ((JsonArray)orders["modules"]!)[0] = "cart/allow.rego";
            });

        // act
        using var archive = FusionArchive.Open(tampered, leaveOpen: true);
        var read = () => archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        await Assert.ThrowsAsync<InvalidDataException>(read);
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_Throw_When_LibraryPathCollidesWithPolicyModulePath()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        const string libPackageSource =
            "package lib\n"
            + "import rego.v1\n"
            + "# METADATA\n"
            + "# entrypoint: true\n"
            + "default allow := false\n";
        var bundle = new RegoPolicyBundle
        {
            Packages =
            [
                new RegoPolicyBundlePackage(
                    "lib",
                    [new RegoPolicyBundleModule("rbac", Encoding.UTF8.GetBytes(libPackageSource))],
                    Requirements: null)
            ]
        };
        await using var stream = await BuildArchiveAsync(bundle, ct);
        await using var tampered = await EditManifestAsync(
            stream,
            manifest =>
            {
                var policy = (JsonObject)((JsonArray)manifest["policies"]!)[0]!;
                var modulePath = (string)((JsonArray)policy["modules"]!)[0]!;
                var digest = (string)((JsonObject)policy["sha256"]!)[modulePath]!;
                var libraries = (JsonArray)manifest["libraries"]!;
                libraries.Add(new JsonObject { ["path"] = modulePath, ["sha256"] = digest });
            });

        // act
        using var archive = FusionArchive.Open(tampered, leaveOpen: true);
        var read = () => archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        await Assert.ThrowsAsync<InvalidDataException>(read);
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_Throw_When_LibraryHashMismatch()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var bundle = FullBundle();
        await using var stream = await BuildArchiveAsync(bundle, ct);
        await using var tampered = await ReplaceZipEntry(
            stream,
            "policies/rego/2.0.0/lib/rbac.rego",
            Encoding.UTF8.GetBytes(RbacLibrarySource + "\n# tampered\n"));

        // act
        using var archive = FusionArchive.Open(tampered, leaveOpen: true);
        var read = () => archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        await Assert.ThrowsAsync<InvalidDataException>(read);
    }

    [Fact]
    public async Task GetRegoPolicyBundle_Should_Throw_When_DataHashMismatch()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var bundle = FullBundle();
        await using var stream = await BuildArchiveAsync(bundle, ct);
        await using var tampered = await ReplaceZipEntry(
            stream,
            "policies/rego/2.0.0/data/data.json",
            """{"role":"tampered"}"""u8.ToArray());

        // act
        using var archive = FusionArchive.Open(tampered, leaveOpen: true);
        var read = () => archive.GetRegoPolicyBundleAsync(s_bundleVersion, ct);

        // assert
        await Assert.ThrowsAsync<InvalidDataException>(read);
    }

    [Fact]
    public async Task SetRegoPolicyAsync_Should_Throw_When_VersionIsBundle()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetRegoPolicyBundleAsync(SinglePackageBundle(), s_bundleVersion, ct);

        // act
        var write = () => archive.SetRegoPolicyAsync(
            "extra",
            "package extra"u8.ToArray(),
            "fragment Requirements on Product { id }"u8.ToArray(),
            s_bundleVersion,
            ct);

        // assert
        await Assert.ThrowsAsync<InvalidOperationException>(write);
    }

    [Fact]
    public async Task SetRegoDataAsync_Should_Throw_When_VersionIsBundle()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetRegoPolicyBundleAsync(SinglePackageBundle(), s_bundleVersion, ct);

        // act
        var write = () => archive.SetRegoDataAsync("roles", """{"admin":true}"""u8.ToArray(), s_bundleVersion, ct);

        // assert
        await Assert.ThrowsAsync<InvalidOperationException>(write);
    }

    private static RegoPolicyBundle SinglePackageBundle()
        => new()
        {
            Packages =
            [
                new RegoPolicyBundlePackage(
                    "cart",
                    [new RegoPolicyBundleModule("allow", Encoding.UTF8.GetBytes(CartAllowSource))],
                    Requirements: null)
            ]
        };

    private static RegoPolicyBundle FullBundle()
        => new()
        {
            Packages =
            [
                new RegoPolicyBundlePackage(
                    "cart",
                    [new RegoPolicyBundleModule("allow", Encoding.UTF8.GetBytes(CartAllowSource))],
                    Encoding.UTF8.GetBytes("{ id }"))
            ],
            Libraries = [new RegoPolicyBundleModule("rbac", Encoding.UTF8.GetBytes(RbacLibrarySource))],
            Data = Encoding.UTF8.GetBytes("""{"role":"admin"}""")
        };

    private static async Task<MemoryStream> BuildArchiveAsync(RegoPolicyBundle bundle, CancellationToken ct)
    {
        var stream = new MemoryStream();

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetRegoPolicyBundleAsync(bundle, s_bundleVersion, ct);
            await archive.CommitAsync(ct);
        }

        stream.Position = 0;
        return stream;
    }

    private static async Task<MemoryStream> ReplaceZipEntry(MemoryStream source, string entryName, byte[] content)
    {
        var stream = ToExpandableStream(source.ToArray());

#if NET10_0_OR_GREATER
        await using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
#else
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
#endif
        {
            zip.GetEntry(entryName)?.Delete();
            var entry = zip.CreateEntry(entryName);
            await using var entryStream = entry.Open();
            await entryStream.WriteAsync(content);
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream DeleteZipEntry(MemoryStream source, string entryName)
    {
        var stream = ToExpandableStream(source.ToArray());

        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
        {
            zip.GetEntry(entryName)?.Delete();
        }

        stream.Position = 0;
        return stream;
    }

    private static async Task<MemoryStream> AddZipEntry(MemoryStream source, string entryName, byte[] content)
    {
        var stream = ToExpandableStream(source.ToArray());

#if NET10_0_OR_GREATER
        await using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
#else
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
#endif
        {
            var entry = zip.CreateEntry(entryName);
            await using var entryStream = entry.Open();
            await entryStream.WriteAsync(content);
        }

        stream.Position = 0;
        return stream;
    }

    private static async Task<MemoryStream> EditManifestAsync(MemoryStream source, Action<JsonObject> edit)
    {
        var buffer = source.ToArray();
        JsonObject manifest;

        await using (var readStream = ToExpandableStream(buffer))
#if NET10_0_OR_GREATER
        await using (var zip = new ZipArchive(readStream, ZipArchiveMode.Read, leaveOpen: true))
#else
        using (var zip = new ZipArchive(readStream, ZipArchiveMode.Read, leaveOpen: true))
#endif
        {
            var entry = zip.GetEntry("policies/rego/2.0.0/manifest.json")!;
            await using var entryStream = entry.Open();
            manifest = (JsonObject)JsonNode.Parse(entryStream)!;
        }

        edit(manifest);

        return await ReplaceZipEntry(
            ToExpandableStream(buffer),
            "policies/rego/2.0.0/manifest.json",
            Encoding.UTF8.GetBytes(manifest.ToJsonString()));
    }

    // Edits the manifest as raw text rather than through the JsonObject model, which silently
    // collapses duplicate keys; used for malformed-manifest cases the object model cannot represent.
    private static async Task<MemoryStream> ReplaceManifestTextAsync(
        MemoryStream source, Func<string, string> edit)
    {
        var buffer = source.ToArray();
        string manifestText;

        await using (var readStream = ToExpandableStream(buffer))
#if NET10_0_OR_GREATER
        await using (var zip = new ZipArchive(readStream, ZipArchiveMode.Read, leaveOpen: true))
#else
        using (var zip = new ZipArchive(readStream, ZipArchiveMode.Read, leaveOpen: true))
#endif
        {
            var entry = zip.GetEntry("policies/rego/2.0.0/manifest.json")!;
            await using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream, Encoding.UTF8);
            manifestText = await reader.ReadToEndAsync();
        }

        return await ReplaceZipEntry(
            ToExpandableStream(buffer),
            "policies/rego/2.0.0/manifest.json",
            Encoding.UTF8.GetBytes(edit(manifestText)));
    }

    // Duplicates the single key/value pair inside the first "sha256":{...} object of a manifest, by
    // plain substring manipulation: the JsonObject model collapses duplicate keys on assignment, so a
    // genuinely duplicate key can only be produced by editing the serialized text directly.
    private static string DuplicateSha256Entry(string manifestText)
    {
        const string marker = "\"sha256\":{";
        var start = manifestText.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = manifestText.IndexOf('}', start);
        var inner = manifestText[start..end];

        return manifestText.Remove(start, end - start).Insert(start, $"{inner},{inner}");
    }

    private static MemoryStream ToExpandableStream(byte[] data)
    {
        var stream = new MemoryStream();
        stream.Write(data, 0, data.Length);
        stream.Position = 0;
        return stream;
    }
}
