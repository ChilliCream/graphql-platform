using System.Text;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Services;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

// TODO: Disposable to validate all session/file-system cases were validated
public abstract class CommandTestBase
    : IClassFixture<NitroCommandFixture>, IAsyncDisposable
{
    private readonly string _currentDirectory = "/some/working/directory";
    private readonly NitroCommandFixture _fixture;
    private readonly List<Stream> _files = [];
    private readonly Mock<IFileSystem> _fileSystemMock = new();
    private readonly Mock<IEnvironmentVariableProvider> _environmentVariableProviderMock = new();
    protected readonly Mock<IFusionConfigurationClient> FusionConfigurationClientMock = new(MockBehavior.Strict);
    private InteractionMode _interactionMode = InteractionMode.NonInteractive;
    private bool _authenticated = true;

    protected CommandTestBase(NitroCommandFixture fixture)
    {
        _fixture = fixture;

        _fileSystemMock.Setup(x => x.GetCurrentDirectory())
            .Returns(_currentDirectory);
    }

    protected void SetupInteractionMode(InteractionMode mode)
    {
        _interactionMode  = mode;
    }

    protected void SetupNoAuthentication()
    {
        _authenticated = false;
    }

    protected async Task<CommandResult> ExecuteCommandAsync(params string[] args)
    {
         var builder = new CommandBuilder(_fixture)
             .AddService(_fileSystemMock.Object)
             .AddService(_environmentVariableProviderMock.Object)
             .AddService(FusionConfigurationClientMock.Object);

         if (_authenticated)
         {
             builder.AddApiKey();
         }

         return await builder
            .AddInteractionMode(_interactionMode)
            .AddArguments(args)
            .ExecuteAsync();
    }

    protected void SetupFile(string path, string content)
    {
        SetupFile(path, new MemoryStream(Encoding.UTF8.GetBytes(content)));
    }

    protected void SetupFile(string path, MemoryStream stream)
    {
        var fullPath = Path.Combine(_currentDirectory, path);

        _files.Add(stream);

        _fileSystemMock.Setup(x => x.FileExists(fullPath)).Returns(true);
        _fileSystemMock.Setup(x => x.OpenReadStream(fullPath)).Returns(stream);
        _fileSystemMock
            .Setup(x => x.ReadAllBytesAsync(fullPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream.ToArray());
        _fileSystemMock
            .Setup(x => x.ReadAllTextAsync(fullPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetString(stream.ToArray()));
    }

    protected void SetupFusionPublishingStateCache(string requestId)
    {
        var cacheFile = Path.Combine(Path.GetTempPath(), "fusion.configuration.publishing.state");
        _fileSystemMock.Setup(x => x.FileExists(cacheFile)).Returns(true);
        _fileSystemMock
            .Setup(x => x.ReadAllTextAsync(cacheFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(requestId);
    }

    protected void SetupFusionPublishingStateCacheMiss()
    {
        var cacheFile = Path.Combine(Path.GetTempPath(), "fusion.configuration.publishing.state");
        _fileSystemMock.Setup(x => x.FileExists(cacheFile)).Returns(false);
    }

    protected void SetupOpenReadStream(string path, byte[]? content = null)
    {
        var fullPath = Path.Combine(_currentDirectory, path);
        _fileSystemMock.Setup(x => x.FileExists(fullPath)).Returns(true);
        _fileSystemMock.Setup(x => x.OpenReadStream(fullPath))
            .Returns(new MemoryStream(content ?? "archive-content"u8.ToArray()));
    }

    protected void SetupEnvironmentVariable(string variableName, string value)
    {
        _environmentVariableProviderMock
            .Setup(x => x.GetEnvironmentVariable("NITRO_" + variableName))
            .Returns(value);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var file in _files)
        {
            await file.DisposeAsync();
        }

        FusionConfigurationClientMock.VerifyAll();
    }
}
