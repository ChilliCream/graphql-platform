using System.Threading.Tasks;
using CommandLine;
using StrawberryShake.Tools.Options;
using StrawberryShake.Tools.Commands;

namespace StrawberryShake.Tools
{
    class Program
    {
        static async Task<int> Main(string[] args) =>
            await Parser.Default.ParseArguments<
                Compile,
                Download,
                Generate,
                Init,
                Publish,
                Update
            >(args).MapResult(
                (Compile z) => Command.Compile(z),
                (Download z) => Command.Download(z),
                (Generate z) => Command.Generate(z),
                (Init z) => Command.Init(z),
                (Publish z) => Command.Publish(z),
                (Update z) => Command.Update(z),
                errors => new ValueTask<int>(1)
            );
    }
}
