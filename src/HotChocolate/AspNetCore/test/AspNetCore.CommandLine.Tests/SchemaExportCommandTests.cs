using HotChocolate.Types;
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
        var output = new StringWriter();
        var app = new App(host);

        // act
        await app.InvokeAsync("schema export -h", output);

        // assert
        OutputHelper.ReplaceExecutableName(output.ToString()).MatchSnapshot();
    }

    [Fact]
    public async Task App_Should_OutputCorrectHelpTest_When_Print_HelpIsRequested()
    {
        // arrange
        var host = new Mock<IHost>().Object;
        var output = new StringWriter();
        var app = new App(host);

        // act
        await app.InvokeAsync("schema print -h", output);

        // assert
        OutputHelper.ReplaceExecutableName(output.ToString()).MatchSnapshot();
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
        var output = new StringWriter();
        var app = new App(host);

        // act
        await app.InvokeAsync("schema print", output);

        // assert
        output.ToString().MatchSnapshot();
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
        var output = new StringWriter();
        var app = new App(host);
        var tempFile = CreateSchemaFileName();

        // act
        await app.InvokeAsync($"schema export --output {tempFile}", output);

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
        var output = new StringWriter();
        var app = new App(host);

        // act
        await app.InvokeAsync("schema print --schema-name Foo", output);

        // assert
        output.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task App_Should_Return_ExitCode_1_If_Schema_Is_Invalid()
    {
        // arrange
        var services = new ServiceCollection();
        // We're intentionally no specifying any fields here to create an invalid schema.
        services.AddGraphQL().AddQueryType();

        var hostMock = new Mock<IHost>();
        hostMock
            .Setup(x => x.Services)
            .Returns(services.BuildServiceProvider());

        var host = hostMock.Object;

        // act
        var exitCode = await host.RunWithGraphQLCommandsAsync(["schema", "print"]);

        // assert
        Assert.Equal(1, exitCode);
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
