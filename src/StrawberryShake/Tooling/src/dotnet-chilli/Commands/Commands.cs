using System.Threading.Tasks;
using StrawberryShake.Tools.Abstractions;
using StrawberryShake.Tools.Commands.Compile;
using StrawberryShake.Tools.Commands.Download;
using StrawberryShake.Tools.Commands.Export;
using StrawberryShake.Tools.Commands.Generate;
using StrawberryShake.Tools.Options;

namespace StrawberryShake.Tools.Commands
{
    public static class Commands
    {

        public static ValueTask<int> Compile(Options.Compile compile) => Execute<CompileCommandHandler, Options.Compile>(compile);
        public static ValueTask<int> Download(Options.Download download) => Execute<DownloadCommandHandler, Options.Download>(download);
        public static ValueTask<int> Export(Options.Export export) => Execute<ExportCommandHandler, Options.Export>(export);
        public static ValueTask<int> Generate(Options.Generate generate) => Execute<GenerateCommandHandler, Options.Generate>(generate);
        public static ValueTask<int> Init(Options.Init init) => Execute<InitCommandHandler, Options.Init>(init);
        public static ValueTask<int> Publish(Options.PublishSchema publishSchema) => Execute<PublishSchemaCommandHandler, Options.PublishSchema>(publishSchema);
        public static ValueTask<int> Update(Options.Update update) => Execute<UpdateCommandHandler, Options.Update>(update);

        private static async ValueTask<int> Execute<TCommand, TOptions>(TOptions options)
            where TOptions : BaseOptions
            where TCommand : class, ICommandHandler<TOptions>
        {
            await using var provider = new CommandProvider<TCommand>(options);
            return await provider.GetCommand().ExecuteAsync(options).ConfigureAwait(false);
        }
    }
}
