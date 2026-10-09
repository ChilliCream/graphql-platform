using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator.Tests;

public class MediatorDispatchTests
{
    [Fact]
    public async Task SendAsync_Should_DispatchToHandler_When_VoidCommandSent()
    {
        // Arrange
        DispatchVoidCommandHandler.WasInvoked = false;
        var sp = DispatchTestHelper.BuildDefaultProvider();
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new DispatchVoidCommand("hello"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(DispatchVoidCommandHandler.WasInvoked);
    }

    [Fact]
    public async Task SendAsync_Should_ThrowInvalidOperationException_When_PipelineNotRegistered()
    {
        // Arrange - register mediator but no pipelines
        var sp = DispatchTestHelper.BuildProvider((_, _) => { });
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.SendAsync(new DispatchVoidCommand("missing"), TestContext.Current.CancellationToken)
                .AsTask());
    }

    [Fact]
    public async Task SendAsync_Should_PropagateException_When_HandlerThrows()
    {
        // Arrange
        var sp = DispatchTestHelper.BuildDefaultProvider();
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.SendAsync(new DispatchThrowingCommand("boom"), TestContext.Current.CancellationToken)
                .AsTask());
        Assert.Equal("handler-exploded", ex.Message);
    }

    [Fact]
    public async Task SendAsync_Should_PassCancellationTokenToHandler_When_TokenProvided()
    {
        // Arrange
        DispatchTokenCapturingHandler.CapturedToken = default;
        var sp = DispatchTestHelper.BuildDefaultProvider();
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await mediator.SendAsync(new DispatchTokenCapturingCommand(), token);

        // Assert
        Assert.Equal(token, DispatchTokenCapturingHandler.CapturedToken);
    }

    [Fact]
    public async Task SendAsync_Should_ReturnCorrectResponse_When_CommandWithResponseSent()
    {
        // Arrange
        var sp = DispatchTestHelper.BuildDefaultProvider();
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new DispatchCommand("test"), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("handled", result.Data);
    }

    [Fact]
    public async Task SendAsync_Should_AwaitAsyncHandler_When_HandlerYields()
    {
        // Arrange - DispatchAsyncCommandHandler uses Task.Yield() so it's truly async
        var sp = DispatchTestHelper.BuildDefaultProvider();
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new DispatchAsyncCommand("async"), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("async-result", result.Data);
    }

    [Fact]
    public async Task QueryAsync_Should_ReturnCorrectResponse_When_QueryDispatched()
    {
        // Arrange
        var sp = DispatchTestHelper.BuildDefaultProvider();
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.QueryAsync(new DispatchQuery(42), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("query-result", result.Data);
    }

    [Fact]
    public async Task PublishAsync_Should_InvokeHandler_When_NotificationPublished()
    {
        // Arrange
        DispatchNotificationHandler.WasInvoked = false;
        var sp = DispatchTestHelper.BuildDefaultProvider();
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.PublishAsync(new DispatchNotification("ping"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(DispatchNotificationHandler.WasInvoked);
    }

    [Fact]
    public async Task PublishAsync_Should_InvokeAllHandlers_When_MultipleHandlersRegistered()
    {
        // Arrange
        DispatchNotificationHandler.WasInvoked = false;
        DispatchSecondNotificationHandler.WasInvoked = false;
        var sp = DispatchTestHelper.BuildMultiNotificationProvider();
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.PublishAsync(new DispatchNotification("fan-out"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(DispatchNotificationHandler.WasInvoked);
        Assert.True(DispatchSecondNotificationHandler.WasInvoked);
    }

    [Fact]
    public async Task SendAsync_Should_ThrowInvalidOperationException_When_NoPipelinesRegistered()
    {
        // Arrange - no pipelines registered
        var sp = DispatchTestHelper.BuildProvider((_, _) => { });
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.SendAsync(new DispatchCommand("missing"), TestContext.Current.CancellationToken).AsTask());
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.QueryAsync(new DispatchQuery(1), TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task PublishAsync_Should_Complete_When_NoNotificationHandlerRegistered()
    {
        // arrange
        var sp = DispatchTestHelper.BuildProvider((_, _) => { });
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // act
        var task = mediator.PublishAsync(new DispatchNotification("unhandled"), TestContext.Current.CancellationToken);

        // assert
        Assert.True(task.IsCompletedSuccessfully);
        await task;
    }

    [Fact]
    public async Task PublishAsync_Should_Complete_When_UntypedNotificationHasNoHandler()
    {
        // arrange
        var sp = DispatchTestHelper.BuildProvider((_, _) => { });
        using var scope = sp.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // act
        var task = publisher.PublishAsync(
            (object)new DispatchNotification("unhandled"),
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(task.IsCompletedSuccessfully);
        await task;
    }

    [Fact]
    public async Task PublishAsync_Should_ThrowArgumentException_When_ObjectIsNotANotification()
    {
        // arrange
        var sp = DispatchTestHelper.BuildProvider((_, _) => { });
        using var scope = sp.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => publisher.PublishAsync(new DispatchVoidCommand("x"), TestContext.Current.CancellationToken).AsTask());

        // assert
        Assert.Equal(
            "Type 'Mocha.Mediator.Tests.DispatchVoidCommand' does not implement INotification. "
            + "If this is a command or query, use SendAsync or QueryAsync instead. (Parameter 'notification')",
            exception.Message);
    }

    [Fact]
    public async Task SendAsync_Should_DispatchAndReturnResult_When_CalledViaUntypedISender()
    {
        // Arrange
        var sp = DispatchTestHelper.BuildDefaultProvider();
        using var scope = sp.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act - dispatch a command-with-response via the untyped overload
        var result = await sender.SendAsync(
            (object)new DispatchCommand("untyped"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        var response = Assert.IsType<DispatchResponse>(result);
        Assert.Equal("handled", response.Data);
    }

    [Fact]
    public async Task PublishAsync_Should_DispatchCorrectly_When_CalledViaUntypedIPublisher()
    {
        // Arrange
        DispatchNotificationHandler.WasInvoked = false;
        var sp = DispatchTestHelper.BuildDefaultProvider();
        using var scope = sp.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // Act
        await publisher.PublishAsync(
            (object)new DispatchNotification("untyped"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(DispatchNotificationHandler.WasInvoked);
    }
}
