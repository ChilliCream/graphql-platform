using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Tools.Abstractions;
using StrawberryShake.Tools.Config;
using StrawberryShake.Tools.Console;
using StrawberryShake.Tools.FileSystem;
using StrawberryShake.Tools.Http;
using StrawberryShake.Tools.Options;

namespace StrawberryShake.Tools.Commands
{
    public class CommandProvider<T> : IAsyncDisposable where T : class
    {
        private readonly ServiceProvider _services;

        public CommandProvider(BaseOptions baseOptions)
        {
            var services = new ServiceCollection();

            services.AddSingleton<IHttpClientFactory, DefaultHttpClientFactory>();
            services.AddSingleton<IFileSystem, DefaultFileSystem>();
            services.AddSingleton<IConfigurationStore, DefaultConfigurationStore>();

            if (baseOptions.Json)
            {
                services.AddSingleton<IConsoleOutput, JsonConsoleOutput>();
            }
            else
            {
                services.AddSingleton<IConsoleOutput, DefaultConsoleOutput>();
            }

            services.AddSingleton<T>();

            _services = services.BuildServiceProvider();
        }

        public T GetCommand() => _services.GetService<T>();

        public async ValueTask DisposeAsync() => await _services.DisposeAsync();
    }
}
