using System.IO;
using System.Threading.Tasks;
using Nerdbank.Streams;
using StrawberryShake.CodeGeneration.CSharp.Analyzers;
using StreamJsonRpc;

namespace CodeGeneration.CSharp.Analyzers.Worker
{
    class Program
    {
        async static Task Main(string[] args)
        {
            var server = new Server();
            await server.SetConfiguration(
                "/Users/michaelstaib/local/hc-1/src/StrawberryShake/SourceGenerator/test/CodeGeneration.CSharp.Analyzers.Tests/StarWars/.graphqlrc.json");
            server.SetDocuments(new string[]
            {
                "/Users/michaelstaib/local/hc-1/src/StrawberryShake/SourceGenerator/test/CodeGeneration.CSharp.Analyzers.Tests/StarWars/ChatGetPeople.graphql",
                "/Users/michaelstaib/local/hc-1/src/StrawberryShake/SourceGenerator/test/CodeGeneration.CSharp.Analyzers.Tests/StarWars/Schema.extensions.graphql",
                "/Users/michaelstaib/local/hc-1/src/StrawberryShake/SourceGenerator/test/CodeGeneration.CSharp.Analyzers.Tests/StarWars/Schema.graphql"
            });
            var result = await server.Generate();

        }
            // => Server.RunAsync();
    }
}
