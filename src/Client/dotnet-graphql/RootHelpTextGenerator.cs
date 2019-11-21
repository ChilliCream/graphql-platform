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
            output.WriteLine();
            output.WriteLine("- Update local schema");
            output.WriteLine("  dotnet graphql update");
            output.WriteLine();
            output.WriteLine("- Compile queries");
            output.WriteLine("  dotnet graphql compile");
            output.WriteLine();
            output.WriteLine("- Compile queries and generate C# client files");
            output.WriteLine("  dotnet graphql generate");
        }
    }
}
