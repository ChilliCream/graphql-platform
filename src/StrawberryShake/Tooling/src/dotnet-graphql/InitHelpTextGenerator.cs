using System.IO;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.HelpText;

namespace StrawberryShake.Tools
{
    public class InitHelpTextGenerator : IHelpTextGenerator
    {
        public void Generate(CommandLineApplication application, TextWriter output)
        {
            output.WriteLine(
                "dotnet graphql init {url} [-p|--Path] [-n|--SchemaName] " +
                "[-t|--token] [-s|--scheme]");
        }
    }
}
