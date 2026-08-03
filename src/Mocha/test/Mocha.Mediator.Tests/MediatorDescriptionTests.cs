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

    [Fact]
    public void Describe_Should_ReturnSource_When_HandlerConfigurationHasSource()
    {
        // arrange
        var source = new SourceMetadata
        {
            Assembly = "Mocha.Mediator.Tests",
            RepositoryUrl = "https://github.com/example/mocha",
            Commit = "abc123",
            XmlDocumentation = "<summary>Manual query handler.</summary>",
            DeclarationLocation = new DeclarationLocation("ManualQueryHandler.cs", null, 1, 1, 5, 2)
        };
        var services = new ServiceCollection();
        services.AddMediator()
            .ConfigureOptions(o => o.ServiceName = "orders")
            .AddHandler<ManualQueryHandler>(d => d.Extend().Configuration.Source = source);
        using var provider = services.BuildServiceProvider();

        // act
        var description = provider.GetRequiredService<MediatorRuntime>().Describe();

        // assert
        var handler = description.Handlers.Single(h => h.Name == nameof(ManualQueryHandler));
        Assert.Same(source, handler.Source);
    }
}
