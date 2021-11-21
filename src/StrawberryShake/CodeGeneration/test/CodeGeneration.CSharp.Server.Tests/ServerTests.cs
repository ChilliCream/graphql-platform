using System;
using System.IO;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp;

public class ServerTests
{
    [Fact]
    public async Task SetConfiguration_FileIsNull()
    {
        // arrange
        var server = new Server();

        // act
        ServerResponse response = await server.SetConfigurationAsync(null!);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task SetConfiguration_FileIsEmpty()
    {
        // arrange
        var server = new Server();

        // act
        ServerResponse response = await server.SetConfigurationAsync(string.Empty);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task SetConfiguration_ValidConfig()
    {
        // arrange
        var server = new Server();
        var configFile = FilePath(".graphqlrc.json");

        // act
        ServerResponse response = await server.SetConfigurationAsync(configFile);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public void SetDocuments_FilesIsNull()
    {
        // arrange
        var server = new Server();

        // act
        ServerResponse response = server.SetDocuments(null!);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public void SetDocuments_FilesIsEmpty()
    {
        // arrange
        var server = new Server();

        // act
        ServerResponse response = server.SetDocuments(Array.Empty<string>());

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public void SetDocuments_ValidFiles()
    {
        // arrange
        var server = new Server();
        var document = FilePath("ChatGetPeople.graphql");

        // act
        ServerResponse response = server.SetDocuments(new[] { document });

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Generate_NoConfig()
    {
        // arrange
        var server = new Server();

        // act
        GeneratorResponse response = await server.GenerateAsync();

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Generate_NoFiles()
    {
        // arrange
        var server = new Server();
        var configFile = FilePath(".graphqlrc.json");
        await server.SetConfigurationAsync(configFile);

        // act
        GeneratorResponse response = await server.GenerateAsync();

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Generate_StarWars()
    {
        // arrange
        var server = new Server();
        var configFile = FilePath(".graphqlrc.json");
        var documents = new[]
        {
            FilePath("ChatGetPeople.graphql"),
            FilePath("Schema.extensions.graphql"),
            FilePath("Schema.graphql")
        };


        // act
        await server.SetConfigurationAsync(configFile);
        server.SetDocuments(documents);
        GeneratorResponse response = await server.GenerateAsync();

        // assert
        response.MatchSnapshot();
    }

    private static string FilePath(string name)
        => Path.Combine("__resources__", name);
}
