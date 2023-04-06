using System.CommandLine.Builder;
using HotChocolate.Fusion.CommandLine.Commands;

namespace HotChocolate.Fusion.CommandLine;

/// <summary>
/// A command line builder for the GraphQL server.
/// </summary>
internal sealed class App : CommandLineBuilder
{
    /// <summary>
    /// Initializes a new instance of <see cref="App"/>.
    /// </summary>
    private App() : base(new RootCommand())
    {
        this.UseDefaults();
    }

    public static App CreateBuilder() => new();
}
