using System.Threading.Tasks;
using CommandLine;
using StrawberryShake.Tools.Options;
using StrawberryShake.Tools.Commands;

namespace StrawberryShake.Tools
{
    public class Program
    {
        public static async Task<int> Main(string[] args) =>
            await Parser.Default.ParseArguments<
                Compile,
                Download,
                Export,
                Generate,
                Init,
                PublishSchema,
                Update
            >(args).MapResult(
                (Compile z) => Commands.Commands.Compile(z),
                (Download z) => Commands.Commands.Download(z),
                (Generate z) => Commands.Commands.Generate(z),
                (Export z) => Commands.Commands.Export(z),
                (Init z) => Commands.Commands.Init(z),
                (PublishSchema z) => Commands.Commands.Publish(z),
                (Update z) => Commands.Commands.Update(z),
                errors => new ValueTask<int>(1)
            );
    }
}
