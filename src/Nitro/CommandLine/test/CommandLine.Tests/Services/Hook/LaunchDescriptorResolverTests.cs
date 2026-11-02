using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

public sealed class LaunchDescriptorResolverTests
{
    [Fact]
    public void Resolve_Should_UsePortableCommandName_When_RunningAsGlobalToolShim()
    {
        // act
        var descriptor = LaunchDescriptorResolver.Resolve(
            "/home/agent/.dotnet/tools/nitro",
            "/home/agent/.dotnet/tools/.store/chillicream.nitro.commandline/99.0.0/tools/net11.0/any/nitro.dll");

        // assert
        Assert.Equal("nitro", descriptor.Executable);
        Assert.Empty(descriptor.ArgumentPrefix);
    }

    [Fact]
    public void Resolve_Should_KeepAssemblyPrefix_When_RunningThroughDotnetMuxer()
    {
        // act
        var descriptor = LaunchDescriptorResolver.Resolve(
            "/usr/share/dotnet/dotnet",
            "/work/nitro.dll");

        // assert
        Assert.Equal("/usr/share/dotnet/dotnet", descriptor.Executable);
        Assert.Equal(["/work/nitro.dll"], descriptor.ArgumentPrefix);
    }

    [Fact]
    public void Resolve_Should_KeepExecutablePath_When_RunningAsAppHost()
    {
        // act
        var descriptor = LaunchDescriptorResolver.Resolve(
            "/opt/nitro/nitro",
            "/opt/nitro/nitro");

        // assert
        Assert.Equal("/opt/nitro/nitro", descriptor.Executable);
        Assert.Empty(descriptor.ArgumentPrefix);
    }
}
