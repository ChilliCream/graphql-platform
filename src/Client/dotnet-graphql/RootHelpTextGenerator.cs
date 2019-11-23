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
                "[-t|--token] [-s|--scheme]");
            output.WriteLine();
            output.WriteLine("- Update local schema");
            output.WriteLine("  dotnet graphql update");
            output.WriteLine(
                "  dotnet graphql update [-p|--Path] [-u|--uri] " +
                "[-t|--token] [-s|--scheme]");
            output.WriteLine();
            output.WriteLine("- Compile queries");
            output.WriteLine("  dotnet graphql compile");
            output.WriteLine(
                "dotnet graphql compile [-s|--Search] [-f|--Force] [-j|--json]");
            output.WriteLine();
            output.WriteLine("- Compile queries and generate C# client files");
            output.WriteLine("  dotnet graphql generate");
            output.WriteLine(
                "  dotnet graphql generate [-p|--Path] [-l|--LanguageVersion] " +
                "[-d|--DISupport] [-n|--Namespace] [-s|--Search] [-f|--Force] " +
                "[-j|--json]");
        }
    }
}
