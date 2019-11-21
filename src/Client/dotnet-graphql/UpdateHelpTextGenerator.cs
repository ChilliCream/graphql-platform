using System.IO;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.HelpText;

namespace StrawberryShake.Tools
{
    public class UpdateHelpTextGenerator : IHelpTextGenerator
    {
        public void Generate(CommandLineApplication application, TextWriter output)
        {
            output.WriteLine(
                "dotnet graphql [-u|--url] [-p|--Path] [-n|--SchemaName] " +
                "[-t|--token] [-s|--scheme]");
        }
    }
}
