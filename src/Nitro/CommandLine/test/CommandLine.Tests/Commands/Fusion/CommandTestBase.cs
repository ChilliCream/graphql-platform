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
    protected readonly Mock<IFusionConfigurationClient> FusionConfigurationClientMock = new();

    protected CommandTestBase(NitroCommandFixture fixture)
    {
        _fixture = fixture;

        _fileSystemMock.Setup(x => x.GetCurrentDirectory())
            .Returns(_currentDirectory);
    }

    protected void SetupInteractionMode(InteractionMode mode)
    {
    }

    // TODO: Default should be non-interactive
    protected async Task<CommandResult> ExecuteCommandAsync(params string[] args)
    {
        return await new CommandBuilder(_fixture)
            .AddInteractionMode(InteractionMode.NonInteractive)
            // TODO: Get rid of this
            .AddApiKey()
            .AddService(_fileSystemMock.Object)
            .AddService(_environmentVariableProviderMock.Object)
            .AddService(FusionConfigurationClientMock.Object)
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
