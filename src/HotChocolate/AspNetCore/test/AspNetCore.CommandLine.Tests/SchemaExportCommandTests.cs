using HotChocolate.Types;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace HotChocolate.AspNetCore.CommandLine;

public class SchemaExportCommandTests
{
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
        await app.InvokeAsync("schema export", console);

        // assert
        console.Out.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task App_Should_WriteSchemaToFile_When_OutputOptionIsSpecfied()
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
        var tempFile = System.IO.Path.GetTempFileName();

        // act
        await app.InvokeAsync($"schema export --output {tempFile}", console);

        // assert
        (await File.ReadAllTextAsync(tempFile)).MatchSnapshot();
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
        await app.InvokeAsync("schema export --schema-name Foo", console);

        // assert
        console.Out.ToString().MatchSnapshot();
    }
}
