using System.CommandLine;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore.CommandLine;

internal sealed class App
{
    private readonly RootCommand _root = new("A command line tool for a GraphQL Server.");

    public App(IHost host)
    {
        GraphQLCommands.Configure(_root, host.Services);
    }

    public int Invoke(string[] args) => _root.Parse(args).Invoke();

    public Task<int> InvokeAsync(string[] args) => _root.Parse(args).InvokeAsync();

    public Task<int> InvokeAsync(string args, TextWriter output)
        => _root.Parse(args).InvokeAsync(new InvocationConfiguration { Output = output });
}
