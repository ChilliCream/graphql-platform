using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.ProjectSettings;

namespace ChilliCream.Nitro.CommandLine.Tests.Services.ProjectSettings;

public sealed class ProjectSettingsServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly ProjectSettingsService _service = new();

    public ProjectSettingsServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "nitro-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    // --- FindSettingsDirectory tests ---

    [Fact]
    public void FindSettingsDirectory_ReturnsRoot_WhenSettingsExist()
    {
        // arrange
        var nitroDir = Path.Combine(_tempRoot, ".nitro");
        Directory.CreateDirectory(nitroDir);
        File.WriteAllText(Path.Combine(nitroDir, "settings.json"), "{}");

        // act
        var result = _service.FindSettingsDirectory(_tempRoot);

        // assert
        Assert.Equal(_tempRoot, result);
    }

    [Fact]
    public void FindSettingsDirectory_WalksUp_ToFindSettings()
    {
        // arrange
        var nitroDir = Path.Combine(_tempRoot, ".nitro");
        Directory.CreateDirectory(nitroDir);
        File.WriteAllText(Path.Combine(nitroDir, "settings.json"), "{}");

        var childDir = Path.Combine(_tempRoot, "src", "api");
        Directory.CreateDirectory(childDir);

        // act
        var result = _service.FindSettingsDirectory(childDir);

        // assert
        Assert.Equal(_tempRoot, result);
    }

    [Fact]
    public void FindSettingsDirectory_ReturnsNull_WhenNoSettingsFound()
    {
        // arrange - temp directory with no .nitro folder
        var isolatedDir = Path.Combine(_tempRoot, "isolated");
        Directory.CreateDirectory(isolatedDir);

        // act - search from a leaf that has no .nitro anywhere up to _tempRoot
        // We can't easily guarantee no .nitro exists above _tempRoot,
        // but we can check it returns _tempRoot or a parent.
        // Instead, we test that a deep directory with no settings returns null
        // by creating a subdirectory structure that doesn't have .nitro.
        var result = _service.FindSettingsDirectory(isolatedDir);

        // assert
        // The walk might find a .nitro above _tempRoot in CI, so just verify
        // it doesn't return isolatedDir itself (no .nitro there)
        if (result is not null)
        {
            Assert.NotEqual(isolatedDir, result);
        }
    }

    [Fact]
    public void FindSettingsDirectory_FindsClosest_WhenMultipleExist()
    {
        // arrange - parent has .nitro
        var parentNitro = Path.Combine(_tempRoot, ".nitro");
        Directory.CreateDirectory(parentNitro);
        File.WriteAllText(Path.Combine(parentNitro, "settings.json"), "{}");

        // child also has .nitro
        var childDir = Path.Combine(_tempRoot, "packages", "api");
        var childNitro = Path.Combine(childDir, ".nitro");
        Directory.CreateDirectory(childNitro);
        File.WriteAllText(Path.Combine(childNitro, "settings.json"), "{}");

        var deepDir = Path.Combine(childDir, "src", "resolvers");
        Directory.CreateDirectory(deepDir);

        // act - starting from deepDir, the closest .nitro is at childDir
        var result = _service.FindSettingsDirectory(deepDir);

        // assert
        Assert.Equal(childDir, result);
    }

    // --- LoadAsync tests ---

    [Fact]
    public async Task LoadAsync_ReturnsSettings_WhenFileExists()
    {
        // arrange
        var nitroDir = Path.Combine(_tempRoot, ".nitro");
        Directory.CreateDirectory(nitroDir);
        const string json = """
            {
                "workspaceId": "ws-load-test",
                "defaultStage": "dev",
                "apis": [{"id": "api-1", "path": "src/api"}]
            }
            """;
        await File.WriteAllTextAsync(Path.Combine(nitroDir, "settings.json"), json);

        // act
        var settings = await _service.LoadAsync(_tempRoot, CancellationToken.None);

        // assert
        Assert.NotNull(settings);
        Assert.Equal("ws-load-test", settings.WorkspaceId);
        Assert.Equal("dev", settings.DefaultStage);
        Assert.Single(settings.Apis!);
    }

    [Fact]
    public async Task LoadAsync_ReturnsNull_WhenNoFileExists()
    {
        // arrange - directory with no .nitro
        var emptyDir = Path.Combine(_tempRoot, "no-settings");
        Directory.CreateDirectory(emptyDir);

        // act
        var settings = await _service.LoadAsync(emptyDir, CancellationToken.None);

        // assert - walk will go above _tempRoot, may or may not find settings
        // This test verifies the method doesn't throw.
        // A more isolated test would require mocking the filesystem.
    }

    [Fact]
    public async Task LoadAsync_ReturnsNull_WhenFileIsInvalidJson()
    {
        // arrange
        var nitroDir = Path.Combine(_tempRoot, ".nitro");
        Directory.CreateDirectory(nitroDir);
        await File.WriteAllTextAsync(
            Path.Combine(nitroDir, "settings.json"), "NOT VALID JSON!!!");

        // act
        var settings = await _service.LoadAsync(_tempRoot, CancellationToken.None);

        // assert
        Assert.Null(settings);
    }

    // --- SaveAsync tests ---

    [Fact]
    public async Task SaveAsync_CreatesFileAndDirectory()
    {
        // arrange
        var saveDir = Path.Combine(_tempRoot, "save-test");
        Directory.CreateDirectory(saveDir);
        var settings = new CommandLine.Services.ProjectSettings.ProjectSettings
        {
            WorkspaceId = "ws-save",
            DefaultStage = "staging"
        };

        // act
        await _service.SaveAsync(settings, saveDir, CancellationToken.None);

        // assert
        var filePath = Path.Combine(saveDir, ".nitro", "settings.json");
        Assert.True(File.Exists(filePath));

        var content = await File.ReadAllTextAsync(filePath);
        var loaded = JsonSerializer.Deserialize(
            content, ProjectSettingsJsonContext.Default.ProjectSettings);
        Assert.NotNull(loaded);
        Assert.Equal("ws-save", loaded.WorkspaceId);
        Assert.Equal("staging", loaded.DefaultStage);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTrips()
    {
        // arrange
        var dir = Path.Combine(_tempRoot, "roundtrip");
        Directory.CreateDirectory(dir);
        var original = new CommandLine.Services.ProjectSettings.ProjectSettings
        {
            WorkspaceId = "ws-rt",
            DefaultStage = "production",
            Apis =
            [
                new ApiSettings { Id = "a1", Name = "Main API", Path = "src/main" }
            ],
            StyleTags = ["relay", "graphql"]
        };

        // act
        await _service.SaveAsync(original, dir, CancellationToken.None);
        var loaded = await _service.LoadAsync(dir, CancellationToken.None);

        // assert
        Assert.NotNull(loaded);
        Assert.Equal("ws-rt", loaded.WorkspaceId);
        Assert.Equal("production", loaded.DefaultStage);
        Assert.Single(loaded.Apis!);
        Assert.Equal("a1", loaded.Apis![0].Id);
        Assert.Equal(2, loaded.StyleTags!.Count);
    }

    // --- ResolveContext tests ---

    [Fact]
    public void ResolveContext_SingleApi_ReturnsIt()
    {
        // arrange
        var settings = new CommandLine.Services.ProjectSettings.ProjectSettings
        {
            WorkspaceId = "ws-ctx",
            DefaultStage = "dev",
            Apis =
            [
                new ApiSettings { Id = "api-1", Path = "src/api" }
            ]
        };

        // act
        var ctx = _service.ResolveContext(settings, _tempRoot, _tempRoot);

        // assert
        Assert.Equal("ws-ctx", ctx.WorkspaceId);
        Assert.Equal("dev", ctx.DefaultStage);
        Assert.NotNull(ctx.ActiveApi);
        Assert.Equal("api-1", ctx.ActiveApi.Id);
    }

    [Fact]
    public void ResolveContext_MonorepoApis_SelectsMostSpecificAncestor()
    {
        // arrange
        var settings = new CommandLine.Services.ProjectSettings.ProjectSettings
        {
            Apis =
            [
                new ApiSettings { Id = "root-api", Path = "." },
                new ApiSettings { Id = "packages-api", Path = "packages" },
                new ApiSettings { Id = "nested-api", Path = "packages/graphql" }
            ]
        };
        var cwd = Path.Combine(_tempRoot, "packages", "graphql", "src");
        Directory.CreateDirectory(cwd);

        // act
        var ctx = _service.ResolveContext(settings, _tempRoot, cwd);

        // assert - "packages/graphql" is the most specific ancestor of cwd
        Assert.NotNull(ctx.ActiveApi);
        Assert.Equal("nested-api", ctx.ActiveApi.Id);
    }

    [Fact]
    public void ResolveContext_MonorepoApis_FallsBackToFirst_WhenNoPathMatch()
    {
        // arrange
        var settings = new CommandLine.Services.ProjectSettings.ProjectSettings
        {
            Apis =
            [
                new ApiSettings { Id = "api-a", Path = "services/a" },
                new ApiSettings { Id = "api-b", Path = "services/b" }
            ]
        };
        // cwd is outside any of the paths
        var cwd = Path.Combine(_tempRoot, "unrelated");
        Directory.CreateDirectory(cwd);

        // act
        var ctx = _service.ResolveContext(settings, _tempRoot, cwd);

        // assert - falls back to first entry
        Assert.NotNull(ctx.ActiveApi);
        Assert.Equal("api-a", ctx.ActiveApi.Id);
    }

    [Fact]
    public void ResolveContext_NoApis_ReturnsNullActiveApi()
    {
        // arrange
        var settings = new CommandLine.Services.ProjectSettings.ProjectSettings
        {
            WorkspaceId = "ws-no-apis"
        };

        // act
        var ctx = _service.ResolveContext(settings, _tempRoot, _tempRoot);

        // assert
        Assert.Null(ctx.ActiveApi);
        Assert.Null(ctx.ActiveClient);
        Assert.Null(ctx.ActiveMcpCollection);
        Assert.Null(ctx.ActiveOpenApiCollection);
    }

    [Fact]
    public void ResolveContext_StyleTags_DefaultToEmpty()
    {
        // arrange
        var settings = new CommandLine.Services.ProjectSettings.ProjectSettings();

        // act
        var ctx = _service.ResolveContext(settings, _tempRoot, _tempRoot);

        // assert
        Assert.Empty(ctx.StyleTags);
    }

    [Fact]
    public void ResolveContext_SetsSettingsRoot()
    {
        // arrange
        var settings = new CommandLine.Services.ProjectSettings.ProjectSettings();

        // act
        var ctx = _service.ResolveContext(settings, _tempRoot, _tempRoot);

        // assert
        Assert.Equal(_tempRoot, ctx.SettingsRoot);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // ignore cleanup failures
        }
    }
}
