using StrawberryShake.Tools.Options;
using CommandLine;

namespace StrawberryShake.Tools
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<InitOptions, UpdateOptions, CompileOptions, GenerateOptions, DownloadOptions, PublishOptions>(args);
        }
    }
}
