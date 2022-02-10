using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using StrawberryShake.Tools.Configuration;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp;

public class CSharpGeneratorServerTests : IDisposable
{
    private readonly List<string> _directories = new();

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
        GeneratorRequest request = CreateConfig(
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

        var requestSink = RequestFormatter.Format(request);

        // act
        var status = await CSharpGeneratorServer.RunAsync(requestSink);

        // assert
        Assert.Equal(0, status);
        ResponseParser.Parse(requestSink).MatchSnapshot();
        Assert.False(File.Exists(requestSink));
        Assert.False(File.Exists(Path.Combine(request.RootDirectory, "Client.components.g.cs")));
    }

    private static string FilePath(string name)
        => Path.Combine("__resources__", name);

    private GeneratorRequest CreateConfig(GraphQLConfig config)
    {
        const string chatGetPeople = "ChatGetPeople.graphql";
        const string extensions = "Schema.extensions.graphql";
        const string schema = "Schema.graphql";

        var root = Path.GetTempFileName();
        File.Delete(root);
        Directory.CreateDirectory(root);
        _directories.Add(root);

        var configFile = Path.Combine(root, ".graphqlrc.json");
        var chatGetPeopleFile = Path.Combine(root, chatGetPeople);
        var extensionsFile = Path.Combine(root, extensions);
        var schemaFile = Path.Combine(root, schema);

        File.WriteAllText(configFile, config.ToString(), Encoding.UTF8);
        File.Copy(FilePath(chatGetPeople), chatGetPeopleFile);
        File.Copy(FilePath(extensions), extensionsFile);
        File.Copy(FilePath(schema), schemaFile);

        var documents = new[]
        {
            chatGetPeopleFile,
            extensionsFile,
            schemaFile
        };
        return new(configFile, documents, root);
    }

    public void Dispose()
    {
        if (_directories.Count > 0)
        {
            foreach (var directory in _directories)
            {
                foreach (var file in
                    Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
                {
                    File.Delete(file);
                }
                Directory.Delete(directory, true);
            }
        }
    }
}
