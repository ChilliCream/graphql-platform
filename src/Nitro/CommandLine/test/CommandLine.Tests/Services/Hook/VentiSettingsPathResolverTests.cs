using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Tests <see cref="VentiSettingsPathResolver"/> against a real workspace in a temporary directory.
/// </summary>
public sealed class VentiSettingsPathResolverTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot = Directory.CreateTempSubdirectory("nitro-venti-settings-path-tests");

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task Resolve_Should_TargetTheCurrentDirectory_When_ProjectScopeRunsInAWorkspaceSubdirectory()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(workspaceDirectory);
        await using (await new AgentDatabase().InitializeAsync(workspaceDirectory, cancellationToken))
        {
        }

        var launchDirectory = Path.Combine(_tempRoot.FullName, "src", "app");
        Directory.CreateDirectory(launchDirectory);
        var fileSystem = new TestFileSystem(launchDirectory);

        // act
        var ventiPath = new VentiSettingsPathResolver(fileSystem).Resolve(HookInstallScopes.Project);
        var claudePath = new ClaudeSettingsPathResolver(fileSystem).Resolve(HookInstallScopes.Project);

        // assert
        Assert.Equal(
            (Path.Combine(launchDirectory, ".venti", "settings.json"),
                Path.Combine(_tempRoot.FullName, ".claude", "settings.json")),
            (ventiPath, claudePath));
    }

    [Fact]
    public void Resolve_Should_ThrowWithInitGuidance_When_ProjectScopeRunsOutsideAWorkspace()
    {
        // arrange
        var resolver = new VentiSettingsPathResolver(new TestFileSystem(_tempRoot.FullName));

        // act
        var exception = Assert.Throws<ExitException>(() => resolver.Resolve(HookInstallScopes.Project));

        // assert
        exception.Message.MatchInlineSnapshot("No agent workspace found. Run `nitro agent init` first.");
    }

    [Fact]
    public void Resolve_Should_TargetTheGlobalVentiSettings_When_UserScopeIsSelected()
    {
        // arrange
        var resolver = new VentiSettingsPathResolver(new TestFileSystem(_tempRoot.FullName));

        // act
        var path = resolver.Resolve(HookInstallScopes.User);

        // assert
        Assert.Equal(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".venti", "settings.json"),
            path);
    }
}
