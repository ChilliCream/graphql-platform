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
        var output = new StringWriter();
        var app = new App(host);

        // act
        await app.InvokeAsync("-h", output);

        // assert
        OutputHelper.ReplaceExecutableName(output.ToString()).MatchSnapshot();
    }
}
