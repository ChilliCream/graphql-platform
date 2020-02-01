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
                "dotnet graphql update [-p|--Path] [-u|--uri] " +
                "[-t|--token] [-s|--scheme]");
        }
    }
}
