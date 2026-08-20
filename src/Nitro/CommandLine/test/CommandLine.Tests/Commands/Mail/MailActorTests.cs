using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

/// <summary>
/// Exercises <see cref="MailActor.Resolve"/>'s full precedence chain: the
/// option value, then NITRO_MAIL_ACTOR, then NITRO_TASK_ACTOR, then the OS
/// user name.
/// </summary>
public sealed class MailActorTests
{
    [Fact]
    public void Resolve_Should_UseOption_When_OptionValueIsGiven()
    {
        // arrange
        var environmentVariables = new Mock<IEnvironmentVariableProvider>();
        environmentVariables
            .Setup(x => x.GetEnvironmentVariable(MailActor.EnvironmentVariableName))
            .Returns("from-mail-env");
        environmentVariables
            .Setup(x => x.GetEnvironmentVariable(MailActor.FallbackEnvironmentVariableName))
            .Returns("from-task-env");

        // act
        var actor = MailActor.Resolve("Explicit-Actor", environmentVariables.Object);

        // assert
        Assert.Equal("explicit-actor", actor);
    }

    [Fact]
    public void Resolve_Should_UseMailEnvironmentVariable_When_NoOptionIsGiven()
    {
        // arrange
        var environmentVariables = new Mock<IEnvironmentVariableProvider>();
        environmentVariables
            .Setup(x => x.GetEnvironmentVariable(MailActor.EnvironmentVariableName))
            .Returns("from-mail-env");
        environmentVariables
            .Setup(x => x.GetEnvironmentVariable(MailActor.FallbackEnvironmentVariableName))
            .Returns("from-task-env");

        // act
        var actor = MailActor.Resolve(null, environmentVariables.Object);

        // assert
        Assert.Equal("from-mail-env", actor);
    }

    [Fact]
    public void Resolve_Should_UseTaskEnvironmentVariable_When_MailEnvironmentVariableIsUnset()
    {
        // arrange
        var environmentVariables = new Mock<IEnvironmentVariableProvider>();
        environmentVariables
            .Setup(x => x.GetEnvironmentVariable(MailActor.EnvironmentVariableName))
            .Returns((string?)null);
        environmentVariables
            .Setup(x => x.GetEnvironmentVariable(MailActor.FallbackEnvironmentVariableName))
            .Returns("from-task-env");

        // act
        var actor = MailActor.Resolve(null, environmentVariables.Object);

        // assert
        Assert.Equal("from-task-env", actor);
    }

    [Fact]
    public void Resolve_Should_UseOsUser_When_NoOptionOrEnvironmentVariableIsGiven()
    {
        // arrange
        var environmentVariables = new Mock<IEnvironmentVariableProvider>();
        environmentVariables
            .Setup(x => x.GetEnvironmentVariable(MailActor.EnvironmentVariableName))
            .Returns((string?)null);
        environmentVariables
            .Setup(x => x.GetEnvironmentVariable(MailActor.FallbackEnvironmentVariableName))
            .Returns((string?)null);

        // act
        var actor = MailActor.Resolve(null, environmentVariables.Object);

        // assert
        Assert.Equal(MailAgentName.Normalize(Environment.UserName), actor);
    }
}
