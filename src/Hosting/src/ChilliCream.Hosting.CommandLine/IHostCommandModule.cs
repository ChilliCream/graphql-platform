using System.CommandLine;

namespace ChilliCream.Hosting.CommandLine;

public interface IHostCommandModule
{
    static abstract void Configure(RootCommand root, IServiceProvider services);
}
