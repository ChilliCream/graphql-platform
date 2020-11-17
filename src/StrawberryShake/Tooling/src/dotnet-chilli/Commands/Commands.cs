using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Tools.Abstractions;
using StrawberryShake.Tools.Commands.Compile;
using StrawberryShake.Tools.Commands.Download;
using StrawberryShake.Tools.Config;
using StrawberryShake.Tools.Console;
using StrawberryShake.Tools.FileSystem;
using StrawberryShake.Tools.Http;
using StrawberryShake.Tools.Options;

namespace StrawberryShake.Tools.Commands
{
    public static class Command
    {
        public static async ValueTask<int> Compile(Options.Compile compile) =>
            await GetService<CompileCommandHandler>(compile)
                .ExecuteAsync(compile)
                .ConfigureAwait(false);

        public static async ValueTask<int> Download(Options.Download download) =>
            await GetService<DownloadCommandHandler>(download)
                .ExecuteAsync(download)
                .ConfigureAwait(false);

        public static async ValueTask<int> Generate(Generate generate)
        {
            return 0;
        }

        public static async ValueTask<int> Init(Init init)
        {
            return 0;
        }

        public static async ValueTask<int> Publish(Publish publish)
        {
            return 0;
        }

        public static async ValueTask<int> Update(Update update)
        {
            return 0;
        }

        private static T GetService<T>(BaseOptions options) where T : class
        {
            var services = new ServiceCollection();

            services.AddSingleton<IHttpClientFactory, DefaultHttpClientFactory>();
            services.AddSingleton<IFileSystem, DefaultFileSystem>();
            services.AddSingleton<IConfigurationStore, DefaultConfigurationStore>();

            if (options.Json)
            {
                services.AddSingleton<IConsoleOutput, JsonConsoleOutput>();
            }
            else
            {
                services.AddSingleton<IConsoleOutput, DefaultConsoleOutput>();
            }

            services.AddSingleton<T>();

            return services.BuildServiceProvider().GetRequiredService<T>();

        }
    }
}
