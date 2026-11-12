using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using static Mocha.Mediator.Tests.ActivityTestHelper;

namespace Mocha.Mediator.Tests;

public sealed class OpenTelemetryTests
{
    [Fact]
    public async Task SendAsync_Should_CreateActivity_When_CommandSent()
    {
        // arrange
        using var capture = CaptureActivities(out var activities);

        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // act
        await mediator.SendAsync(new TracedCommand("traced"), TestContext.Current.CancellationToken);

        // assert
        activities.MatchSnapshot();
    }

    [Fact]
    public async Task QueryAsync_Should_CreateActivity_When_QuerySent()
    {
        // arrange
        using var capture = CaptureActivities(out var activities);

        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // act
        var result = await mediator.QueryAsync(new TracedQuery(1), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("result: 1", result);
        activities.MatchSnapshot();
    }

    [Fact]
    public async Task PublishAsync_Should_CreateActivity_When_NotificationPublished()
    {
        // arrange
        using var capture = CaptureActivities(out var activities);

        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // act
        await mediator.PublishAsync(new TracedNotification("traced"), TestContext.Current.CancellationToken);

        // assert
        activities.MatchSnapshot();
    }

    [Fact]
    public async Task SendAsync_Should_RecordException_When_HandlerThrows()
    {
        // arrange
        using var capture = CaptureActivities(out var activities);

        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.SendAsync(new FailingCommand("boom"), TestContext.Current.CancellationToken).AsTask());

        // assert
        activities.MatchSnapshot();
    }

    [Fact]
    public async Task SendAsync_Should_NestActivities_When_HandlerDispatchesMessages()
    {
        // arrange
        using var capture = CaptureActivities(out var activities);

        await using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // act
        await mediator.SendAsync(new OuterCommand("traced"), TestContext.Current.CancellationToken);

        // assert
        activities.MatchSnapshot();
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        var builder = services.AddMediator();

        services.AddScoped<TracedCommandHandler>();
        services.AddScoped<TracedQueryHandler>();
        services.AddScoped<TracedNotificationHandler>();
        services.AddScoped<FailingCommandHandler>();
        services.AddScoped<OuterCommandHandler>();

        builder.ConfigureMediator(b =>
        {
            b.AddHandler<TracedCommandHandler>();
            b.AddHandler<TracedQueryHandler>();
            b.AddHandler<TracedNotificationHandler>();
            b.AddHandler<FailingCommandHandler>();
            b.AddHandler<OuterCommandHandler>();
        });

        builder.AddInstrumentation();

        return services.BuildServiceProvider();
    }

    public sealed record TracedCommand(string Value) : ICommand;

    public sealed record TracedQuery(int Id) : IQuery<string>;

    public sealed record TracedNotification(string Value) : INotification;

    public sealed record FailingCommand(string Value) : ICommand;

    public sealed record OuterCommand(string Value) : ICommand;

    public sealed class TracedCommandHandler : ICommandHandler<TracedCommand>
    {
        public ValueTask HandleAsync(TracedCommand command, CancellationToken cancellationToken)
            => default;
    }

    public sealed class TracedQueryHandler : IQueryHandler<TracedQuery, string>
    {
        public ValueTask<string> HandleAsync(TracedQuery query, CancellationToken cancellationToken)
            => new($"result: {query.Id}");
    }

    public sealed class TracedNotificationHandler : INotificationHandler<TracedNotification>
    {
        public ValueTask HandleAsync(TracedNotification notification, CancellationToken cancellationToken)
            => default;
    }

    public sealed class FailingCommandHandler : ICommandHandler<FailingCommand>
    {
        public ValueTask HandleAsync(FailingCommand command, CancellationToken cancellationToken)
            => throw new InvalidOperationException("handler failed");
    }

    public sealed class OuterCommandHandler(IMediator mediator) : ICommandHandler<OuterCommand>
    {
        public async ValueTask HandleAsync(OuterCommand command, CancellationToken cancellationToken)
        {
            await mediator.SendAsync(new TracedCommand(command.Value), cancellationToken);
            await mediator.PublishAsync(new TracedNotification(command.Value), cancellationToken);
        }
    }
}
