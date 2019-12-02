using System.IO;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.HelpText;

namespace StrawberryShake.Tools
{
    public class GenerateHelpTextGenerator : IHelpTextGenerator
    {
        public void Generate(CommandLineApplication application, TextWriter output)
        {
            output.WriteLine(
                "dotnet graphql generate [-p|--Path] [-l|--LanguageVersion] " +
                "[-d|--DISupport] [-n|--Namespace] [-s|--Search] [-f|--Force] " +
                "[-j|--json]");
        }
    }
}
