using System;
using System.Threading.Tasks;

namespace CodeGeneration.CSharp.Analyzers.Worker
{
    internal class Server
    {
        public static async Task RunAsync()
        {
            await LogAsync($"StrawberryShake is initializing.");
            using var stream = FullDuplexStream.Splice(Console.OpenStandardInput(), Console.OpenStandardOutput());
            var jsonRpc = JsonRpc.Attach(stream, new Server());
            await LogAsync($"StrawberryShake is ready for requests.");
            await jsonRpc.Completion;
            await LogAsync($"StrawberryShake terminated.");
        }

        private static async Task LogAsync(string s)
            => Console.Error.WriteLineAsync(s);
    }
}
