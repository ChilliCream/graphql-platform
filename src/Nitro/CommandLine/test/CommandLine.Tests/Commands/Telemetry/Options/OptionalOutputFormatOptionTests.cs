using ChilliCream.Nitro.CommandLine.Results;
using System.CommandLine;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Options;

public sealed class OptionalOutputFormatOptionTests
{
    [Fact]
    public void Parse_Should_AcceptNdjson_When_OutputFormatIsSpecified()
    {
        // arrange
        var output = new OptionalOutputFormatOption();
        var command = new Command("telemetry");
        command.Options.Add(output);

        // act
        var result = command.Parse(["--output", "ndjson"]);

        // assert
        Assert.Empty(result.Errors);
        Assert.Equal(OutputFormat.Ndjson, result.GetValue(output));
    }
}
