using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

public sealed class MailAgentNameTests
{
    [Fact]
    public void Normalize_Should_Lowercase_When_InputHasUppercase()
    {
        // arrange
        const string value = "Claude-Sonnet_5";

        // act
        var normalized = MailAgentName.Normalize(value);

        // assert
        Assert.Equal("claude-sonnet_5", normalized);
    }

    [Fact]
    public void Normalize_Should_Throw_When_Empty()
    {
        // act & assert
        Assert.Throws<ExitException>(() => MailAgentName.Normalize(""));
    }

    [Theory]
    [InlineData("agent.mail")]
    [InlineData("agent name")]
    [InlineData("agent@mail")]
    public void Normalize_Should_Throw_When_ContainsInvalidCharacter(string value)
    {
        // act & assert
        Assert.Throws<ExitException>(() => MailAgentName.Normalize(value));
    }

    [Fact]
    public void Normalize_Should_NotStripInvalidCharacters_When_Rejecting()
    {
        // arrange
        const string value = "agent.mail";

        // act
        var exception = Assert.Throws<ExitException>(() => MailAgentName.Normalize(value));

        // assert
        Assert.Contains(value, exception.Message);
    }
}
