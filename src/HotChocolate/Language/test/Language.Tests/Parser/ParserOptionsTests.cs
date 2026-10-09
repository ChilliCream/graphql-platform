namespace HotChocolate.Language;

public class ParserOptionsTests
{
    [Fact]
    public void Constructor_Should_UseTheGivenExperimentalOptions_When_ExperimentalIsProvided()
    {
        // arrange
        var experimental = new ParserOptionsExperimental(allowFragmentArguments: true);

        // act
        var options = new ParserOptions(experimental, maxAllowedFields: 10);

        // assert
        Assert.Same(experimental, options.Experimental);
        Assert.True(options.Experimental.AllowFragmentArguments);
        Assert.Equal(10, options.MaxAllowedFields);
        Assert.False(options.NoLocations);
    }

    [Fact]
    public void Constructor_Should_Throw_When_ExperimentalIsNull()
    {
        // act
        static void Action() => _ = new ParserOptions(experimental: null!);

        // assert
        var exception = Assert.Throws<ArgumentNullException>(Action);
        Assert.Equal("experimental", exception.ParamName);
    }

    [Fact]
    public void Constructor_Should_LeaveFragmentArgumentsDisabled_When_ExperimentalIsNotProvided()
    {
        // act
        var options = new ParserOptions(allowFragmentVariables: true);

        // assert
        Assert.True(options.Experimental.AllowFragmentVariables);
        Assert.False(options.Experimental.AllowFragmentArguments);
    }
}
