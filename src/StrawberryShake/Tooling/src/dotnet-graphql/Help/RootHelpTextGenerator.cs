using System.IO;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.HelpText;

namespace StrawberryShake.Tools
{
    public class RootHelpTextGenerator : IHelpTextGenerator
    {
        public void Generate(CommandLineApplication application, TextWriter output)
        {
            output.WriteLine("The following commands are supported:");
            output.WriteLine();
            output.WriteLine("- Initialize project and download schema");
            output.WriteLine("  dotnet graphql init http://localhost");
            output.WriteLine(
                "  dotnet graphql init {url} [-p|--Path] [-n|--SchemaName] " +
                "[--token] [--scheme] [--tokenEndpoint] [--clientId] " +
                "[--clientSecret] [--scope]");
            output.WriteLine();

            output.WriteLine("- Update local schema");
            output.WriteLine("  dotnet graphql update");
            output.WriteLine(
                "  dotnet graphql update [-p|--Path] [-u|--uri] " +
                "[--token] [--scheme] [--tokenEndpoint] [--clientId] " +
                "[--clientSecret] [--scope]");
            output.WriteLine();
            
            output.WriteLine("- Download the schema as GraphQL SDL");
            output.WriteLine("  dotnet graphql download http://localhost");
            output.WriteLine(
                "  dotnet graphql download {url} [-f|--FileName] " +
                "[--token] [--scheme] [--tokenEndpoint] [--clientId] " +
                "[--clientSecret] [--scope]");
            output.WriteLine();
        }
    }
}
