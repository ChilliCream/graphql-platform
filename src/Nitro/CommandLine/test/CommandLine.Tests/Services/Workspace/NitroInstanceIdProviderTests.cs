using System.Security.Cryptography;
using System.Text;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="NitroInstanceIdProvider"/>'s two paths: the hashed
/// machine identifier (via an injected reader, so this does not depend on
/// what the test machine actually reports) and the generated-uuid fallback,
/// including its atomic create-or-read-winner semantics.
/// </summary>
public sealed class NitroInstanceIdProviderTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot;
    private readonly string _globalConfigDirectory;

    public NitroInstanceIdProviderTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-instance-id-tests");
        _globalConfigDirectory = Path.Combine(_tempRoot.FullName, "nitro");
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task GetIdAsync_Should_ReturnHashedMachineId_When_MachineIdReaderReturnsAValue()
    {
        // arrange
        var fileSystem = new TestFileSystem(_tempRoot.FullName);
        var provider = new NitroInstanceIdProvider(fileSystem, () => "fixed-machine-id");
        var expected = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes("fixed-machine-id"))).ToLowerInvariant();

        // act
        var id = await provider.GetIdAsync(_globalConfigDirectory, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expected, id);
        Assert.False(fileSystem.DirectoryExists(_globalConfigDirectory));
    }

    [Fact]
    public async Task GetIdAsync_Should_BeDeterministic_When_CalledTwiceWithTheSameMachineId()
    {
        // arrange
        var fileSystem = new TestFileSystem(_tempRoot.FullName);
        var provider = new NitroInstanceIdProvider(fileSystem, () => "fixed-machine-id");

        // act
        var first = await provider.GetIdAsync(_globalConfigDirectory, TestContext.Current.CancellationToken);
        var second = await provider.GetIdAsync(_globalConfigDirectory, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task GetIdAsync_Should_CreateAndPersistFallbackId_When_NoMachineIdCanBeRead()
    {
        // arrange
        var fileSystem = new TestFileSystem(_tempRoot.FullName);
        var provider = new NitroInstanceIdProvider(fileSystem, () => null);

        // act
        var id = await provider.GetIdAsync(_globalConfigDirectory, TestContext.Current.CancellationToken);

        // assert
        var persisted = await File.ReadAllTextAsync(
            Path.Combine(_globalConfigDirectory, "instance-id"), TestContext.Current.CancellationToken);
        Assert.Equal(persisted.Trim(), id);
        Assert.False(string.IsNullOrWhiteSpace(id));
    }

    [Fact]
    public async Task GetIdAsync_Should_ReturnTheSameFallbackId_When_CalledAgainAfterCreating()
    {
        // arrange
        var fileSystem = new TestFileSystem(_tempRoot.FullName);
        var provider = new NitroInstanceIdProvider(fileSystem, () => null);
        var first = await provider.GetIdAsync(_globalConfigDirectory, TestContext.Current.CancellationToken);

        // act
        var second = await provider.GetIdAsync(_globalConfigDirectory, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(first, second);
    }

    /// <summary>
    /// Mirrors what a concurrent first use looks like: another process (or
    /// call) already won the create race and left its own id file behind
    /// before this call's atomic create runs. The create-or-read-winner
    /// contract means this call must return the winner's id, not the
    /// candidate it generated for itself.
    /// </summary>
    [Fact]
    public async Task GetIdAsync_Should_ReturnTheExistingId_When_FallbackFileAlreadyExists()
    {
        // arrange
        var fileSystem = new TestFileSystem(_tempRoot.FullName);
        Directory.CreateDirectory(_globalConfigDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(_globalConfigDirectory, "instance-id"),
            "winner-id",
            TestContext.Current.CancellationToken);
        var provider = new NitroInstanceIdProvider(fileSystem, () => null);

        // act
        var id = await provider.GetIdAsync(_globalConfigDirectory, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("winner-id", id);
    }
}
