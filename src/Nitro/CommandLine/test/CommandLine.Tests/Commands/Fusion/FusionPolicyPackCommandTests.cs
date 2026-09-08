using ChilliCream.Nitro.CommandLine.Tests.Agents;
using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionPolicyPackCommandTests : FusionCommandTestBase
{
    private readonly string _workingDirectory;

    public FusionPolicyPackCommandTests(NitroCommandFixture fixture) : base(fixture)
    {
        _workingDirectory = Directory.CreateTempSubdirectory("nitro-policy-pack-tests").FullName;
        SetupFileSystem(new TestFileSystem(_workingDirectory));
    }

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("fusion", "policy", "pack", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Pack a Rego policy authoring directory into a Rego policy bundle.

            Usage:
              nitro fusion policy pack <ROOT> [options]

            Arguments:
              <ROOT>  The root directory of the Rego policy authoring layout (<root>/<package>/*.rego, <root>/<package>.graphql, <root>/lib/*.rego)

            Options:
              --out <out> (REQUIRED)   The output path: a '.far' file, or a directory to write the unpacked bundle into
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>      The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro fusion policy pack ./policies --out ./gateway.far
            """);
    }

    [Fact]
    public async Task Pack_Should_RoundTripThroughReader_When_AuthoringDirectoryIsValid()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var root = Path.Combine(_workingDirectory, "policies");
        Directory.CreateDirectory(Path.Combine(root, "cart"));
        Directory.CreateDirectory(Path.Combine(root, "lib"));

        await File.WriteAllTextAsync(
            Path.Combine(root, "cart", "allow.rego"),
            """
            package cart
            import rego.v1
            import data.lib
            # METADATA
            # entrypoint: true
            default allow := false
            allow if lib.is_admin(data.role)
            """,
            ct);
        await File.WriteAllTextAsync(Path.Combine(root, "cart.graphql"), "{ id }", ct);
        await File.WriteAllTextAsync(
            Path.Combine(root, "lib", "rbac.rego"),
            """
            package lib
            import rego.v1
            is_admin(role) if role == "admin"
            """,
            ct);

        var outFile = Path.Combine(_workingDirectory, "gateway.far");

        // act
        var result = await ExecuteCommandAsync("fusion", "policy", "pack", root, "--out", outFile);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(outFile));

        await using var stream = File.OpenRead(outFile);
        using var archive = FusionArchive.Open(stream, leaveOpen: true);
        var bundle = await archive.GetRegoPolicyBundleAsync(
            new Version(2, 0, 0), TestContext.Current.CancellationToken);

        Assert.Single(bundle.Packages);
        Assert.Equal("cart", bundle.Packages[0].Package);
        Assert.Equal("{ id }", System.Text.Encoding.UTF8.GetString(bundle.Packages[0].Requirements!.Value.Span));
        Assert.Single(bundle.Libraries);
        Assert.Equal("lib/rbac.rego", bundle.Libraries[0].Name);
    }

    [Fact]
    public async Task Pack_Should_ReturnError_When_RootDoesNotExist()
    {
        // arrange
        var root = Path.Combine(_workingDirectory, "does-not-exist");
        var outFile = Path.Combine(_workingDirectory, "gateway.far");

        // act
        var result = await ExecuteCommandAsync("fusion", "policy", "pack", root, "--out", outFile);

        // assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.False(File.Exists(outFile));
    }
}
