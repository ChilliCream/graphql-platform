using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using IOPath = System.IO.Path;
using static McMaster.Extensions.CommandLineUtils.CommandLineApplication;
using StrawberryShake.Generators;

namespace dotnet_graphql
{
    internal class Program
    {
        internal static Task<int> Main(string[] args) =>
            ExecuteAsync<CommandRouter>(args);
    }

    [Command(ThrowOnUnexpectedArgument = false)]
    public class CommandRouter
        : ICommand
    {
        [Argument(0)]
        public Command Command { get; set; }

        public string[] RemainingArgs { get; set; }

        public Task<int> OnExecute()
        {
            switch (Command)
            {
                case Command.Compile:
                    return ExecuteAsync<CompileCommand>(RemainingArgs);

                default:
                    return Task.FromResult(1);
            }
        }
    }


    public interface ICommand
    {
        Task<int> OnExecute();
    }


    public enum Command
    {
        Init,
        Compile
    }

    public class CompileCommand
        : ICommand
    {
        [Argument(0)]
        public string Path { get; set; }

        public async Task<int> OnExecute()
        {
            ClientGenerator generator = ClientGenerator.New();




            return 0;
        }

        private async Task<Configuration> LoadConfig()
        {
            Configuration config;

            if (Path is null)
            {
                Path = Environment.CurrentDirectory;
            }

            using (var stream = File.OpenRead(IOPath.Combine(Path, "config.json")))
            {
                config = await JsonSerializer.DeserializeAsync<Configuration>(
                    stream,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    });
            }

            return config;
        }



        private static IReadOnlyList<string> GetGraphQLFiles()
        {
            return Directory.GetFiles("*.graphql");
        }

        private struct DocumentInfo
        {
            public string FileName { get; set; }
            public DocumentKind Kind { get; set; }
        }

        private enum DocumentKind
        {
            Schema,
            Query
        }
    }

    /*

     */

    public class Configuration
    {
        public List<SchemaFile> Schemas { get; set; }
    }

    public class SchemaFile
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string File { get; set; }
    }
}
