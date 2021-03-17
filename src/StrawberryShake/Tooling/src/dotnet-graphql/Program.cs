using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace StrawberryShake.Tools
{
    internal static class Program
    {
        internal static Task<int> Main(string[] args)
        {
            using CommandLineApplication init = InitCommand.Create();
            using CommandLineApplication upd = UpdateCommand.Create();
            using CommandLineApplication down = DownloadCommand.Create();
            
            using var root = new CommandLineApplication
            {
                HelpTextGenerator = new RootHelpTextGenerator()
            };

            root.AddSubcommand(init);
            root.AddSubcommand(upd);
            root.AddSubcommand(down);

            root.HelpOption("-h|--help");

            root.OnExecute(() => root.HelpTextGenerator.Generate(root, root.Out));

            return root.ExecuteAsync(args);
        }
    }
}
