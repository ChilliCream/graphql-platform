using System.CommandLine.Builder;
using System.Diagnostics.CodeAnalysis;
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
    [RequiresUnreferencedCode("Calls HotChocolate.Fusion.CommandLine.Commands.RootCommand.RootCommand()")]
    private App() : base(new RootCommand())
    {
        this.UseDefaults();
    }

    [RequiresUnreferencedCode("Calls HotChocolate.Fusion.CommandLine.App.App()")]
    public static App CreateBuilder() => new();
}
