using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
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
            FilePath("ChatGetPeople.graphql"), FilePath("Schema.extensions.graphql"),
            FilePath("Schema.graphql")
        };

        GeneratorRequest request = new(configFile, documents);
        var requestSink = RequestFormatter.Format(request);

        // act

        var status = await CSharpGeneratorServer.RunAsync(requestSink);

        // assert
        Assert.Equal(0, status);
        ResponseFormatter.Take(requestSink).MatchSnapshot();
        Assert.False(File.Exists(requestSink));
    }

    [Fact]
    public async Task Generate_StarWars_With_Razor_Components()
    {
        // arrange
        GeneratorRequest request = CreateConfig(
            new GraphQLConfig {Extensions = {StrawberryShake = {RazorComponents = true}}});

        var requestSink = RequestFormatter.Format(request);

        // act
        var status = await CSharpGeneratorServer.RunAsync(requestSink);

        // assert
        Assert.Equal(0, status);
        ResponseFormatter.Take(requestSink).MatchSnapshot();
        Assert.False(File.Exists(requestSink));
        Assert.True(File.Exists(Path.Combine(
            request.RootDirectory,
            "Generated",
            "Client.components.g.cs")));
    }

    [Fact]
    public async Task Generate_StarWars_With_PersistedQueries()
    {
        // arrange
        GeneratorRequest request = CreateConfig(
            new GraphQLConfig
            {
                Extensions =
                {
                    StrawberryShake =
                    {
                        RazorComponents = true,
                        RequestStrategy = Tools.Configuration.RequestStrategy.PersistedQuery
                    }
                }
            },
            persistedQueryDirectory: "pq");

        var requestSink = RequestFormatter.Format(request);

        // act
        var status = await CSharpGeneratorServer.RunAsync(requestSink);

        // assert
        Assert.Equal(0, status);
        ResponseFormatter.Take(requestSink).MatchSnapshot();
        Assert.False(File.Exists(requestSink));
        Assert.True(Directory.Exists(Path.Combine(request.RootDirectory, "Generated")));
        Assert.True(File.Exists(Path.Combine(
            request.PersistedQueryDirectory!,
            "GetPeople.graphql")));
    }

    [Fact]
    public async Task Generate_StarWars_With_PersistedQueries2()
    {
        // arrange
        GeneratorRequest request = CreateConfig(
            new GraphQLConfig
            {
                Extensions =
                {
                    StrawberryShake =
                    {
                        RazorComponents = true,
                        RequestStrategy = Tools.Configuration.RequestStrategy.PersistedQuery
                    }
                }
            },
            persistedQueryDirectory: "pq",
            option: RequestOptions.ExportPersistedQueries);

        var requestSink = RequestFormatter.Format(request);

        // act
        var status = await CSharpGeneratorServer.RunAsync(requestSink);

        // assert
        Assert.Equal(0, status);
        ResponseFormatter.Take(requestSink).MatchSnapshot();
        Assert.False(File.Exists(requestSink));
        Assert.False(Directory.Exists(Path.Combine(request.RootDirectory, "Generated")));
        Assert.True(File.Exists(Path.Combine(
            request.PersistedQueryDirectory!,
            "GetPeople.graphql")));
    }

    [Fact]
    public async Task Generate_StarWars_With_PersistedQueries_Two_Runs()
    {
        // arrange
        GeneratorRequest request = CreateConfig(
            new GraphQLConfig
            {
                Extensions =
                {
                    StrawberryShake =
                    {
                        RazorComponents = true,
                        RequestStrategy = Tools.Configuration.RequestStrategy.PersistedQuery
                    }
                }
            },
            persistedQueryDirectory: "pq");

        var requestSink = RequestFormatter.Format(request);
        var status = await CSharpGeneratorServer.RunAsync(requestSink);

        Assert.Equal(0, status);
        ResponseFormatter.Take(requestSink).MatchSnapshot();
        Assert.False(File.Exists(requestSink));
        Assert.True(Directory.Exists(Path.Combine(request.RootDirectory, "Generated")));
        Assert.True(File.Exists(Path.Combine(
            request.PersistedQueryDirectory!,
            "GetPeople.graphql")));

        // act
        request = CreateConfig(
            new GraphQLConfig
            {
                Extensions =
                {
                    StrawberryShake =
                    {
                        RazorComponents = true,
                        RequestStrategy = Tools.Configuration.RequestStrategy.PersistedQuery
                    }
                }
            },
            request.RootDirectory,
            persistedQueryDirectory: "pq");
        requestSink = RequestFormatter.Format(request);
        status = await CSharpGeneratorServer.RunAsync(requestSink);

        // assert
        Assert.Equal(0, status);
        ResponseFormatter.Take(requestSink).MatchSnapshot();
        Assert.False(File.Exists(requestSink));
        Assert.True(Directory.Exists(Path.Combine(request.RootDirectory, "Generated")));
        Assert.True(File.Exists(Path.Combine(
            request.PersistedQueryDirectory!,
            "GetPeople.graphql")));
    }

    [Fact]
    public async Task Generate_StarWars_With_PersistedQueries_Json()
    {
        // arrange
        GeneratorRequest request = CreateConfig(
            new GraphQLConfig
            {
                Extensions =
                {
                    StrawberryShake =
                    {
                        RazorComponents = true,
                        RequestStrategy = Tools.Configuration.RequestStrategy.PersistedQuery
                    }
                }
            },
            persistedQueryDirectory: "pq",
            option: RequestOptions.ExportPersistedQueriesJson);

        var requestSink = RequestFormatter.Format(request);

        // act
        var status = await CSharpGeneratorServer.RunAsync(requestSink);

        // assert
        Assert.Equal(0, status);
        ResponseFormatter.Take(requestSink).MatchSnapshot();
        Assert.False(File.Exists(requestSink));
        Assert.False(Directory.Exists(Path.Combine(request.RootDirectory, "Generated")));
        Assert.True(File.Exists(Path.Combine(
            request.PersistedQueryDirectory!,
            "persisted-queries.json")));
    }

    [Fact]
    public async Task Generate_StarWars_Generate_CSharpFiles()
    {
        // arrange
        GeneratorRequest request = CreateConfig(
            new GraphQLConfig
            {
                Extensions =
                {
                    StrawberryShake =
                    {
                        RazorComponents = true,
                        RequestStrategy = Tools.Configuration.RequestStrategy.PersistedQuery
                    }
                }
            },
            persistedQueryDirectory: "pq",
            option: RequestOptions.GenerateCSharpClient);

        var requestSink = RequestFormatter.Format(request);

        // act
        var status = await CSharpGeneratorServer.RunAsync(requestSink);

        // assert
        Assert.Equal(0, status);
        ResponseFormatter.Take(requestSink).MatchSnapshot();
        Assert.False(File.Exists(requestSink));
        Assert.False(Directory.Exists(request.PersistedQueryDirectory!));
        Assert.False(File.Exists(Path.Combine(
            request.RootDirectory,
            "Generated",
            "Client.components.g.cs")));
        Assert.True(File.Exists(Path.Combine(
            request.RootDirectory,
            "Generated",
            "Client.g.cs")));
    }

    [Fact]
    public async Task Generate_StarWars_Generate_CSharpFiles_DoNot_Touch_Components()
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
            },
            option: RequestOptions.Default);

        var requestSink = RequestFormatter.Format(request);
        var status = await CSharpGeneratorServer.RunAsync(requestSink);
        Assert.Equal(0, status);
        Assert.Empty(ResponseFormatter.Take(requestSink).Errors);
        Assert.True(File.Exists(Path.Combine(
            request.RootDirectory,
            "Generated",
            "Client.components.g.cs")));

        request = CreateConfig(
            new GraphQLConfig
            {
                Extensions =
                {
                    StrawberryShake =
                    {
                        RazorComponents = true,
                        RequestStrategy = Tools.Configuration.RequestStrategy.PersistedQuery
                    }
                }
            },
            rootDirectory: request.RootDirectory,
            persistedQueryDirectory: "pq",
            option: RequestOptions.GenerateCSharpClient);

        requestSink = RequestFormatter.Format(request);

        // act
        status = await CSharpGeneratorServer.RunAsync(requestSink);

        // assert
        Assert.Equal(0, status);
        ResponseFormatter.Take(requestSink).MatchSnapshot();
        Assert.False(File.Exists(requestSink));
        Assert.False(Directory.Exists(request.PersistedQueryDirectory!));
        Assert.True(File.Exists(Path.Combine(
            request.RootDirectory,
            "Generated",
            "Client.components.g.cs")));
        Assert.True(File.Exists(Path.Combine(
            request.RootDirectory,
            "Generated",
            "Client.g.cs")));
    }

    private static string FilePath(string name)
        => Path.Combine("__resources__", name);

    private GeneratorRequest CreateConfig(
        GraphQLConfig config,
        string? rootDirectory = null,
        string? defaultNamespace = null,
        string? persistedQueryDirectory = null,
        RequestOptions option = RequestOptions.Default)
    {
        const string chatGetPeople = "ChatGetPeople.graphql";
        const string extensions = "Schema.extensions.graphql";
        const string schema = "Schema.graphql";

        var root = rootDirectory;

        if (root is null)
        {
            root = Path.GetTempFileName();
            File.Delete(root);
            Directory.CreateDirectory(root);
            _directories.Add(root);
        }

        var configFile = Path.Combine(root, ".graphqlrc.json");
        var chatGetPeopleFile = Path.Combine(root, chatGetPeople);
        var extensionsFile = Path.Combine(root, extensions);
        var schemaFile = Path.Combine(root, schema);

        if (!File.Exists(configFile))
        {
            File.WriteAllText(configFile, config.ToString(), Encoding.UTF8);
            File.Copy(FilePath(chatGetPeople), chatGetPeopleFile);
            File.Copy(FilePath(extensions), extensionsFile);
            File.Copy(FilePath(schema), schemaFile);
        }

        if (persistedQueryDirectory is not null)
        {
            persistedQueryDirectory = Path.Combine(root, persistedQueryDirectory);
        }

        var documents = new[] {chatGetPeopleFile, extensionsFile, schemaFile};

        return new(
            configFile,
            documents,
            root,
            defaultNamespace,
            persistedQueryDirectory,
            option);
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
