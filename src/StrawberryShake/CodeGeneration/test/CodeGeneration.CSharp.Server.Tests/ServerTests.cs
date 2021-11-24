using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using ChilliCream.Testing;
using Microsoft.AspNetCore.Http;
using Nerdbank.Streams;
using Newtonsoft.Json;
using Snapshooter.Xunit;
using StreamJsonRpc;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp;

public class ServerTests
{
    [Fact]
    public async Task Generate_StarWarsAsync()
    {
        // arrange
        var server = new CSharpGeneratorServer();
        var configFile = FilePath(".graphqlrc.json");
        var documents = new[]
        {
            FilePath("ChatGetPeople.graphql"),
            FilePath("Schema.extensions.graphql"),
            FilePath("Schema.graphql")
        };

        // act
        GeneratorRequest request = new(configFile, documents);
        GeneratorResponse response = await server.GenerateAsync(request);

        // assert
        response.MatchSnapshot();
    }

    private static string FilePath(string name)
        => Path.Combine("__resources__", name);
}

public class ProtocolTests
{
    [Fact]
    public async Task Generate_StarWarsAsync()
    {
        // arrange
        // .. start server
        using var cts = new CancellationTokenSource(4000);
        (Stream, Stream) streams = FullDuplexStream.CreatePair();
        Stream serverStream = streams.Item1;
        Stream clientStream = streams.Item2;

        using var rpcServer = JsonRpc.Attach(serverStream, new CSharpGeneratorServer());

        // .. prepare request
        var configFile = FilePath(".graphqlrc.json");
        var documents = new[]
        {
            FilePath("ChatGetPeople.graphql"),
            FilePath("Schema.extensions.graphql"),
            FilePath("Schema.graphql")
        };
        var request = new GeneratorRequest(configFile, documents);

        // act
        var client = new CSharpGeneratorClient(clientStream, clientStream);
        GeneratorResponse response = await client.GenerateAsync(request, cts.Token);

        // assert
        response.MatchSnapshot();
    }

    private static string FilePath(string name)
        => Path.Combine("__resources__", name);
}

