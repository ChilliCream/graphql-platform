using System.CommandLine;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Options;

public class EnvironmentVariableDefaultTests
{
    [Fact]
    public async Task DefaultFromEnvironmentValue_Should_ReadFromProvider_When_EnvironmentVariableIsSet()
    {
        // Arrange
        const string expectedValue = "my-test-value";
        var envProviderMock = new Mock<IEnvironmentVariableProvider>();
        envProviderMock
            .Setup(x => x.GetEnvironmentVariable("NITRO_TEST_VAR"))
            .Returns(expectedValue);

        string? capturedValue = null;

        var services = new ServiceCollection();
        services.AddNitroServices();

        var sessionMock = new Mock<ISessionService>();
        sessionMock
            .Setup(x => x.LoadSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session?)null);
        sessionMock.SetupGet(x => x.Session).Returns((Session?)null);
        services.Replace(ServiceDescriptor.Singleton(sessionMock.Object));
        services.Replace(ServiceDescriptor.Singleton<IEnvironmentVariableProvider>(envProviderMock.Object));

        services.AddSingleton<NitroClientContext>();
        services.AddSingleton<INitroClientContextProvider>(
            sp => sp.GetRequiredService<NitroClientContext>());

        var testConsole = new TestConsole();
        var errorConsole = new TestConsole();
        services.AddSingleton<INitroConsole>(
            new NitroConsole(testConsole, errorConsole, envProviderMock.Object));

        await using var provider = services.BuildServiceProvider();

        var testOption = new Option<string>("--test-var");
        testOption.DefaultFromEnvironmentValue("TEST_VAR");

        var rootCommand = new NitroRootCommand();
        var probeCommand = new Command("__probe");
        probeCommand.Options.Add(testOption);
        probeCommand.AddGlobalNitroOptions();
        probeCommand.SetAction((parseResult, _) =>
        {
            capturedValue = parseResult.GetValue(testOption);
            return Task.FromResult(0);
        });
        rootCommand.Add(probeCommand);

        var invocationConfig = new InvocationConfiguration
        {
            Output = TextWriter.Null,
            Error = TextWriter.Null
        };

        // Act
        await rootCommand.ExecuteAsync(
            ["__probe"], provider, invocationConfig, CancellationToken.None);

        // Assert
        Assert.Equal(expectedValue, capturedValue);
    }
}
