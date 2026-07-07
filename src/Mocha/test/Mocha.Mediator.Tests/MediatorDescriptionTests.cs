using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator.Tests;

public sealed class MediatorDescriptionTests
{
    [Fact]
    public void Describe_Should_ReturnMediatorDescription_When_HandlersRegistered()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMediator()
            .ConfigureOptions(o => o.ServiceName = "orders")
            .AddHandler<ManualVoidCommandHandler>()
            .AddHandler<ManualCommandHandler>()
            .AddHandler<ManualQueryHandler>()
            .AddHandler<ManualNotificationHandler1>();
        using var provider = services.BuildServiceProvider();

        // act
        var description = provider.GetRequiredService<MediatorRuntime>().Describe();

        // assert
        description.MatchSnapshot();
    }
}
