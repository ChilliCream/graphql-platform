using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp;

public class ServerTests
{
    [Fact]
    public async Task Generate_StarWarsAsync()
    {
        // arrange
        using var cts = new CancellationTokenSource(3000000);
        var readStream = new TestStream();
        var writeStream = new TestStream();
        await using var client = new CSharpGeneratorClient(readStream, writeStream);
        await using var server = new CSharpGeneratorServer(writeStream, readStream);
        var configFile = FilePath(".graphqlrc.json");
        var documents = new[]
        {
            FilePath("ChatGetPeople.graphql"),
            FilePath("Schema.extensions.graphql"),
            FilePath("Schema.graphql")
        };

        // act
        GeneratorRequest request = new(configFile, documents);
        GeneratorResponse response = await client.GenerateAsync(request, cts.Token);

        // assert
        response.MatchSnapshot();
    }

    private static string FilePath(string name)
        => Path.Combine("__resources__", name);
}
