using System.Runtime.Versioning;
using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

[UnsupportedOSPlatform("windows")]
public sealed class UnixFileOwnerTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot = Directory.CreateTempSubdirectory("nitro-owner-tests");

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task IsCurrentUser_Should_ReturnTrue_When_TheCurrentUserCreatedTheFile()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows files have no Unix owner.");

        // arrange
        var path = Path.Combine(_tempRoot.FullName, "4242.0a1b2c3d.key");
        await File.WriteAllTextAsync(path, "{}", TestContext.Current.CancellationToken);

        // act
        var ownedByCurrentUser = UnixFileOwner.IsCurrentUser(path);

        // assert
        Assert.True(ownedByCurrentUser);
    }

    [Fact]
    public void IsCurrentUser_Should_ReturnFalse_When_AnotherAccountOwnsTheEntry()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows files have no Unix owner.");
        Assert.SkipWhen(Environment.IsPrivilegedProcess, "The root directory is owned by root.");

        // arrange
        // The file system root is owned by root on every Unix system.
        var path = Path.GetPathRoot(_tempRoot.FullName)!;

        // act
        var ownedByCurrentUser = UnixFileOwner.IsCurrentUser(path);

        // assert
        Assert.False(ownedByCurrentUser);
    }

    [Fact]
    public void IsCurrentUser_Should_ReturnFalse_When_TheFileDoesNotExist()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows files have no Unix owner.");

        // arrange
        var path = Path.Combine(_tempRoot.FullName, "missing.key");

        // act
        var ownedByCurrentUser = UnixFileOwner.IsCurrentUser(path);

        // assert
        Assert.False(ownedByCurrentUser);
    }

    [Fact]
    public void IsCurrentUser_Should_JudgeTheLinkItself_When_ThePathIsASymbolicLinkToAnotherAccountsEntry()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows files have no Unix owner.");
        Assert.SkipWhen(Environment.IsPrivilegedProcess, "The root directory is owned by root.");

        // arrange
        var path = Path.Combine(_tempRoot.FullName, "4242.0a1b2c3d.key");
        File.CreateSymbolicLink(path, Path.GetPathRoot(_tempRoot.FullName)!);

        // act
        var ownedByCurrentUser = UnixFileOwner.IsCurrentUser(path);

        // assert
        Assert.True(ownedByCurrentUser);
    }
}
