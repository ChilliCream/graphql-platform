using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using StrawberryShake.Tools.Configuration;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp;

public class ServerTests : IDisposable
{
    private readonly List<string> _files = new();

    [Fact]
    public async Task Generate_StarWars()
    {
        // arrange
        var configFile = FilePath(".graphqlrc.json");
        var documents = new[]
        {
            FilePath("ChatGetPeople.graphql"),
            FilePath("Schema.extensions.graphql"),
            FilePath("Schema.graphql")
        };

        GeneratorRequest request = new(configFile, documents);
        var requestSink = RequestFormatter.Format(request);

        // act

        var status = await CSharpGeneratorServer.RunAsync(requestSink);

        // assert
        Assert.Equal(0, status);
        ResponseParser.Parse(requestSink).MatchSnapshot();
        Assert.False(File.Exists(requestSink));
    }

    [Fact]
    public async Task Generate_StarWars_With_Razor_Components()
    {
        // arrange
        var configFile = CreateConfig(
            new GraphQLConfig
            {
                Extensions =
                {
                    StrawberryShake =
                    {
                        RazorComponents = true
                    }
                }
            });

        var documents = new[]
        {
            FilePath("ChatGetPeople.graphql"),
            FilePath("Schema.extensions.graphql"),
            FilePath("Schema.graphql")
        };

        GeneratorRequest request = new(configFile, documents, "__resources__");
        var requestSink = RequestFormatter.Format(request);

        // act
        var status = await CSharpGeneratorServer.RunAsync(requestSink);

        // assert
        Assert.Equal(0, status);
        ResponseParser.Parse(requestSink).MatchSnapshot();
        Assert.False(File.Exists(requestSink));
    }

    private static string FilePath(string name)
        => Path.Combine("__resources__", name);

    private string CreateConfig(GraphQLConfig config)
    {
        var fileName = Path.GetTempFileName();
        _files.Add(fileName);
        File.WriteAllText(fileName, config.ToString(), Encoding.UTF8);
        return fileName;
    }

    public void Dispose()
    {
        if (_files.Count > 0)
        {
            foreach (var file in _files)
            {
                File.Delete(file);
            }
        }
    }
}
