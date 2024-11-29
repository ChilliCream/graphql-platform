using System.CommandLine.IO;
using System.CommandLine.Parsing;
using Microsoft.Extensions.Hosting;
using Moq;

namespace HotChocolate.AspNetCore.CommandLine;

public class RootCommandTests
{
    [Fact]
    public async Task App_Should_OutputCorrectHelpTest_When_HelpIsRequested()
    {
        // arrange
        var host = new Mock<IHost>().Object;
        var console = new TestConsole();
        var app = new App(host).Build();

        // act
        await app.InvokeAsync("-h", console);

        // assert
        console.Out.ToString().MatchSnapshot();
    }
}
