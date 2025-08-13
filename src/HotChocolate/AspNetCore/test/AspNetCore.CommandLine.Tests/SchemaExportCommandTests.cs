using HotChocolate.Types;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace HotChocolate.AspNetCore.CommandLine;

public class SchemaExportCommandTests : IDisposable
{
    private readonly List<string> _files = [];

    [Fact]
    public async Task App_Should_OutputCorrectHelpTest_When_HelpIsRequested()
    {
        // arrange
        var host = new Mock<IHost>().Object;
        var console = new TestConsole();
        var app = new App(host).Build();

        // act
        await app.InvokeAsync("schema export -h", console);

        // assert
        console.Out.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task App_Should_OutputCorrectHelpTest_When_Print_HelpIsRequested()
    {
        // arrange
        var host = new Mock<IHost>().Object;
        var console = new TestConsole();
        var app = new App(host).Build();

        // act
        await app.InvokeAsync("schema print -h", console);

        // assert
        console.Out.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task App_Should_PrintSchema_When_OutputNotSpecified()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("foo").Resolve("bar"));

        var hostMock = new Mock<IHost>();
        hostMock
            .Setup(x => x.Services)
            .Returns(services.BuildServiceProvider());

        var host = hostMock.Object;
        var console = new TestConsole();
        var app = new App(host).Build();

        // act
        await app.InvokeAsync("schema print", console);

        // assert
        console.Out.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task App_Should_WriteSchemaToFile_When_OutputOptionIsSpecified()
    {
        // arrange
        var snapshot = new Snapshot();
        var services = new ServiceCollection();
        services.AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("foo").Resolve("bar"));

        var hostMock = new Mock<IHost>();
        hostMock
            .Setup(x => x.Services)
            .Returns(services.BuildServiceProvider());

        var host = hostMock.Object;
        var console = new TestConsole();
        var app = new App(host).Build();
        var tempFile = CreateSchemaFileName();

        // act
        await app.InvokeAsync($"schema export --output {tempFile}", console);

        // assert
        snapshot.Add(await File.ReadAllTextAsync(tempFile + ".graphqls"), "Schema", markdownLanguage: "graphql");
        snapshot.Add(await File.ReadAllTextAsync(tempFile + "-settings.json"), "Settings", markdownLanguage: "json");
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task App_Should_WriteNamedSchemaToOutput_When_SchemaNameIsSpecified()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddGraphQL("Foo")
            .AddQueryType(x => x.Name("Query").Field("foo").Resolve("bar"));

        var hostMock = new Mock<IHost>();
        hostMock
            .Setup(x => x.Services)
            .Returns(services.BuildServiceProvider());

        var host = hostMock.Object;
        var console = new TestConsole();
        var app = new App(host).Build();

        // act
        await app.InvokeAsync("schema print --schema-name Foo", console);

        // assert
        console.Out.ToString().MatchSnapshot();
    }

    public string CreateSchemaFileName()
    {
        var tempFile = System.IO.Path.GetTempFileName();
        var schemaFile = tempFile + ".graphqls";
        var settingsFile = tempFile + "-settings.json";
        _files.Add(tempFile);
        _files.Add(schemaFile);
        _files.Add(settingsFile);
        return tempFile;
    }

    public void Dispose()
    {
        foreach (var file in _files)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // ignore
                }
            }
        }

        _files.Clear();
    }
}
