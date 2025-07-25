using HotChocolate.Types;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace HotChocolate.AspNetCore.CommandLine;

public class SchemaListCommandTests
{
    [Fact]
    public async Task App_Should_List_All_SchemaNames()
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
        await app.InvokeAsync("schema list", console);

        // assert
        console.Out.ToString().MatchSnapshot();
    }
}
