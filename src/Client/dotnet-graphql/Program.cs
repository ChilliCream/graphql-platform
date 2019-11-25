using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace StrawberryShake.Tools
{
    internal static class Program
    {
        internal static Task<int> Main(string[] args)
        {
            var root = new CommandLineApplication();
            root.HelpTextGenerator = new RootHelpTextGenerator();
            root.AddSubcommand(InitCommand.Create());
            root.AddSubcommand(UpdateCommand.Create());
            root.AddSubcommand(CompileCommand.Create());
            root.AddSubcommand(GenerateCommand.Create());
            root.HelpOption("-h|--help");
            root.OnExecute(() => root.HelpTextGenerator.Generate(root, root.Out));
            return root.ExecuteAsync(args);
        }
    }
}
