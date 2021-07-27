using System.IO;
using System.Threading.Tasks;
using Nerdbank.Streams;
using StreamJsonRpc;

namespace CodeGeneration.CSharp.Analyzers.Worker
{
    class Program
    {
        static Task Main(string[] args) => Server.RunAsync();
    }
}
