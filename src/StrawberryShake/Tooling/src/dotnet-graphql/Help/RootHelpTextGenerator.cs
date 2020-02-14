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
                "[--token] [--scheme] [--tokenEndpoint] [--clientId]" +
                "[--clientSecret] [--scope]");
            output.WriteLine();
            output.WriteLine("- Update local schema");
            output.WriteLine("  dotnet graphql update");
            output.WriteLine(
                "  dotnet graphql update [-p|--Path] [-u|--uri] " +
                "[--token] [--scheme] [--tokenEndpoint] [--clientId]" +
                "[--clientSecret] [--scope]");
            output.WriteLine();
            output.WriteLine("- Compile queries");
            output.WriteLine("  dotnet graphql compile");
            output.WriteLine(
                "  dotnet graphql compile [-s|--Search] [-f|--Force] [-j|--json]");
            output.WriteLine();
            output.WriteLine("- Compile queries and generate C# client files");
            output.WriteLine("  dotnet graphql generate");
            output.WriteLine(
                "  dotnet graphql generate [-p|--Path] [-l|--LanguageVersion] " +
                "[-d|--DISupport] [-n|--Namespace] [-s|--Search] [-f|--Force] " +
                "[-j|--json]");
            output.WriteLine();
            output.WriteLine("- Download the schema as GraphQL SDL");
            output.WriteLine("  dotnet graphql download http://localhost");
            output.WriteLine(
                "  dotnet graphql download {url} [-f|--FileName] " +
                "[--token] [--scheme] [--tokenEndpoint] [--clientId]" +
                "[--clientSecret] [--scope]");
            output.WriteLine();
            output.WriteLine("- Publish schema version to schema registry");
            output.WriteLine(
                "  dotnet graphql publish schema http://localhost " +
                " Dev Foo Foo_1.0.0 -f schema.graphql -t version:1.0.0");
            output.WriteLine(
                "  dotnet graphql publish schema {Registry} " +
                "{EnvironmentName} {SchemaName} {ExternalId} [-f|--SchemaFileName] " +
                "[-t|--Tag] " +
                "[--token] [--scheme] [--tokenEndpoint] [--clientId]" +
                "[--clientSecret] [--scope]");
            output.WriteLine();
            output.WriteLine("- Mark schema version published on schema registry");
            output.WriteLine(
                "  dotnet graphql publish schema http://localhost " +
                " Dev Foo Foo_1.0.0 -p");
            output.WriteLine(
                "  dotnet graphql publish schema {Registry} " +
                "{EnvironmentName} {SchemaName} {ExternalId} " +
                "[-p|--Published] " +
                "[--token] [--scheme] [--tokenEndpoint] [--clientId]" +
                "[--clientSecret] [--scope]");
            output.WriteLine();
            output.WriteLine("- Publish client version to schema registry");
            output.WriteLine(
                "  dotnet graphql publish client http://localhost " +
                " Dev Foo Bar Bar_1.0.0 -f query.graphql -t version:1.0.0");
            output.WriteLine(
                "  dotnet graphql publish client {Registry} " +
                "{EnvironmentName} {SchemaName} {ClientName} {ExternalId} " +
                "[-d|--searchDirectory] [-f|--queryFileName] [-r|--relayFileFormat] " +
                "[-t|--Tag] " +
                "[--token] [--scheme] [--tokenEndpoint] [--clientId]" +
                "[--clientSecret] [--scope]");
            output.WriteLine();
            output.WriteLine("- Mark client version published on schema registry");
            output.WriteLine(
                "  dotnet graphql publish client http://localhost " +
                " Dev Foo Bar Bar_1.0.0 -p");
            output.WriteLine(
                "  dotnet graphql publish client {Registry} " +
                "{EnvironmentName} {SchemaName} {ClientName} {ExternalId} " +
                "[-p|--Published] " +
                "[--token] [--scheme] [--tokenEndpoint] [--clientId]" +
                "[--clientSecret] [--scope]");
        }
    }
}
