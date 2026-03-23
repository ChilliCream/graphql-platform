using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator.Tests;

public sealed class NamedMediatorTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public NamedMediatorTests()
    {
        var services = new ServiceCollection();

        // Default (unnamed) mediator with its own pipeline
        var defaultBuilder = services.AddMediator();
        services.AddScoped<ICommandHandler<DefaultCommand, string>, DefaultCommandHandler>();
        defaultBuilder.ConfigureMediator(b => b.RegisterPipeline(new MediatorPipelineConfiguration
        {
            MessageType = typeof(DefaultCommand),
            ResponseType = typeof(string),
            Terminal = PipelineBuilder.BuildCommandTerminal<DefaultCommand, string>()
        }));

        // Named mediator "billing" with its own pipeline
        var billingBuilder = services.AddMediator("billing");
        services.AddScoped<ICommandHandler<BillingCommand, string>, BillingCommandHandler>();
        billingBuilder.ConfigureMediator(b => b.RegisterPipeline(new MediatorPipelineConfiguration
        {
            MessageType = typeof(BillingCommand),
            ResponseType = typeof(string),
            Terminal = PipelineBuilder.BuildCommandTerminal<BillingCommand, string>()
        }));

        // Named mediator "shipping" with its own pipeline
        var shippingBuilder = services.AddMediator("shipping");
        services.AddScoped<ICommandHandler<ShippingCommand, string>, ShippingCommandHandler>();
        shippingBuilder.ConfigureMediator(b => b.RegisterPipeline(new MediatorPipelineConfiguration
        {
            MessageType = typeof(ShippingCommand),
            ResponseType = typeof(string),
            Terminal = PipelineBuilder.BuildCommandTerminal<ShippingCommand, string>()
        }));

        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task GetKeyedService_Should_ResolveNamedMediator()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var billingMediator = scope.ServiceProvider.GetRequiredKeyedService<IMediator>("billing");

        // Act
        var result = await billingMediator.SendAsync<string>(new BillingCommand("invoice-123"));

        // Assert
        Assert.Equal("billed:invoice-123", result);
    }

    [Fact]
    public async Task GetService_Should_ResolveCorrectMediator_When_DefaultAndNamedCoexist()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var defaultMediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var billingMediator = scope.ServiceProvider.GetRequiredKeyedService<IMediator>("billing");

        // Act
        var defaultResult = await defaultMediator.SendAsync<string>(new DefaultCommand("hello"));
        var billingResult = await billingMediator.SendAsync<string>(new BillingCommand("pay-456"));

        // Assert
        Assert.Equal("default:hello", defaultResult);
        Assert.Equal("billed:pay-456", billingResult);
    }

    [Fact]
    public async Task SendAsync_Should_DispatchIndependently_When_MultipleNamedMediatorsExist()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var billingMediator = scope.ServiceProvider.GetRequiredKeyedService<IMediator>("billing");
        var shippingMediator = scope.ServiceProvider.GetRequiredKeyedService<IMediator>("shipping");

        // Act
        var billingResult = await billingMediator.SendAsync<string>(new BillingCommand("b-1"));
        var shippingResult = await shippingMediator.SendAsync<string>(new ShippingCommand("s-1"));

        // Assert
        Assert.Equal("billed:b-1", billingResult);
        Assert.Equal("shipped:s-1", shippingResult);
    }

    [Fact]
    public void AddMediator_Should_CreateIsolatedRuntime_When_NamedDifferently()
    {
        // Arrange
        var defaultRuntime = _provider.GetRequiredService<MediatorRuntime>();
        var billingRuntime = _provider.GetRequiredKeyedService<MediatorRuntime>("billing");
        var shippingRuntime = _provider.GetRequiredKeyedService<MediatorRuntime>("shipping");

        // Assert
        Assert.NotSame(defaultRuntime, billingRuntime);
        Assert.NotSame(defaultRuntime, shippingRuntime);
        Assert.NotSame(billingRuntime, shippingRuntime);
    }

    [Fact]
    public void SendAsync_Should_ThrowInvalidOperationException_When_MessageNotRegisteredOnNamedMediator()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var billingMediator = scope.ServiceProvider.GetRequiredKeyedService<IMediator>("billing");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => billingMediator.SendAsync<string>(new DefaultCommand("wrong")).AsTask().GetAwaiter().GetResult());
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}

public sealed record DefaultCommand(string Value) : ICommand<string>;

public sealed record BillingCommand(string InvoiceId) : ICommand<string>;

public sealed record ShippingCommand(string ShipmentId) : ICommand<string>;

public sealed class DefaultCommandHandler : ICommandHandler<DefaultCommand, string>
{
    public ValueTask<string> HandleAsync(DefaultCommand command, CancellationToken cancellationToken)
        => new($"default:{command.Value}");
}

public sealed class BillingCommandHandler : ICommandHandler<BillingCommand, string>
{
    public ValueTask<string> HandleAsync(BillingCommand command, CancellationToken cancellationToken)
        => new($"billed:{command.InvoiceId}");
}

public sealed class ShippingCommandHandler : ICommandHandler<ShippingCommand, string>
{
    public ValueTask<string> HandleAsync(ShippingCommand command, CancellationToken cancellationToken)
        => new($"shipped:{command.ShipmentId}");
}
