using System.CommandLine;

namespace ChilliCream.Hosting.CommandLine;

public interface IHostCommandContributor
{
    /// <summary>
    /// Adds commands and options to the shared root command.
    /// </summary>
    void Configure(RootCommand root);
}
