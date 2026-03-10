using HotChocolate.Types;
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
        services
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("foo").Resolve("bar"));

        var hostMock = new Mock<IHost>();
        hostMock
            .Setup(x => x.Services)
            .Returns(services.BuildServiceProvider());

        var host = hostMock.Object;
        var output = new StringWriter();
        var app = new App(host);

        // act
        await app.InvokeAsync("schema list", output);

        // assert
        output.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task App_Should_List_All_SchemaNames_2()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQL("schema1")
            .AddQueryType(x => x.Name("Query").Field("foo").Resolve("bar"))
            .Services
            .AddGraphQL("schema2")
            .AddQueryType(x => x.Name("Query").Field("foo").Resolve("bar"));

        var hostMock = new Mock<IHost>();
        hostMock
            .Setup(x => x.Services)
            .Returns(services.BuildServiceProvider());

        var host = hostMock.Object;
        var output = new StringWriter();
        var app = new App(host);

        // act
        await app.InvokeAsync("schema list", output);

        // assert
        output.ToString().MatchSnapshot();
    }
}
