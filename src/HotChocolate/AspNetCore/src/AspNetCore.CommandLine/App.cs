using System.Collections.Generic;
using System.CommandLine.Builder;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// A command line builder for the GraphQL server.
/// </summary>
internal sealed class App : CommandLineBuilder
{
    /// <summary>
    /// Initializes a new instance of <see cref="App"/>.
    /// </summary>
    /// <param name="host">
    /// The host that is used to resolve services from the GraphQL Server.
    /// </param>
    public App(IHost host) : base(new GraphQLRootCommand())
    {
        this.AddMiddleware(x => x.BindingContext.AddService(_ => host))
            .UseDefaults();
    }
}
