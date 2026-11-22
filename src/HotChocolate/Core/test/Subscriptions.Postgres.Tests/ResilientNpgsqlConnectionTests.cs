using CookieCrumble.Resources;
using System.Data;
using HotChocolate.Tests;
using Npgsql;

namespace HotChocolate.Subscriptions.Postgres;

public class ResilientNpgsqlConnectionTests
{
    private readonly PostgreSqlResource _resource;
    private readonly SubscriptionTestDiagnostics _events;

    public ResilientNpgsqlConnectionTests(PostgreSqlResource resource, ITestOutputHelper output)
    {
        _events = new SubscriptionTestDiagnostics(output);
        _resource = resource;
    }

    [Fact]
    public async Task Initialize_Should_InitializeAsyncEventHandler_When_Called()
    {
        // Arrange
        var onConnectCalled = false;
        var onDisconnectCalled = false;

        Func<CancellationToken, ValueTask> onConnect = _ =>
        {
            onConnectCalled = true;
            return ValueTask.CompletedTask;
        };
        Func<CancellationToken, ValueTask> onDisconnect = _ =>
        {
            onDisconnectCalled = true;
            return ValueTask.CompletedTask;
        };

        var resilientNpgsqlConnection =
            new ResilientNpgsqlConnection(_events, ConnectionFactory, onConnect, onDisconnect);

        // Act
        await resilientNpgsqlConnection.Initialize(CancellationToken.None);

        // Assert
        Assert.True(onConnectCalled);
        Assert.False(onDisconnectCalled);
        Assert.NotNull(resilientNpgsqlConnection.Connection);
    }

    [Fact]
    public async Task Initialize_Should_Initialize_When_ConnectionIsAlreadyOpen()
    {
        // Arrange
        var onConnectCalled = false;

        Func<CancellationToken, ValueTask> onConnect = _ =>
        {
            onConnectCalled = true;
            return ValueTask.CompletedTask;
        };
        Func<CancellationToken, ValueTask> onDisconnect = _ => ValueTask.CompletedTask;

        var resilientNpgsqlConnection = new ResilientNpgsqlConnection(
            _events,
            ConnectionFactoryAlreadyOpen,
            onConnect,
            onDisconnect);

        // Act
        await resilientNpgsqlConnection.Initialize(CancellationToken.None);

        // Assert
        Assert.True(onConnectCalled);
        Assert.NotNull(resilientNpgsqlConnection.Connection);
    }

    [Fact]
    public async Task DisposeAsync_Should_Disconnect()
    {
        // Arrange
        var onDisconnectCalled = false;

        Func<CancellationToken, ValueTask> onConnect = _ => ValueTask.CompletedTask;
        Func<CancellationToken, ValueTask> onDisconnect = _ =>
        {
            onDisconnectCalled = true;
            return ValueTask.CompletedTask;
        };

        var resilientNpgsqlConnection =
            new ResilientNpgsqlConnection(_events, ConnectionFactory, onConnect, onDisconnect);

        await resilientNpgsqlConnection.Initialize(CancellationToken.None);

        // Act
        await resilientNpgsqlConnection.DisposeAsync();

        // Assert
        Assert.True(onDisconnectCalled);
    }

    [Fact]
    public async Task Reconnect_Should_ReconnectWhenConnectionIsClosed()
    {
        // Arrange
        var onConnectCalled = 0;
        var reconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        ValueTask onConnect(CancellationToken _)
        {
            // Initialize connects once; the reconnect after the drop is the second connect.
            if (++onConnectCalled == 2)
            {
                reconnected.TrySetResult();
            }

            return ValueTask.CompletedTask;
        }
        Func<CancellationToken, ValueTask> onDisconnect = _ => ValueTask.CompletedTask;

        var resilientNpgsqlConnection =
            new ResilientNpgsqlConnection(_events, ConnectionFactory, onConnect, onDisconnect);

        await resilientNpgsqlConnection.Initialize(CancellationToken.None);
        var connection = resilientNpgsqlConnection.Connection;

        // Act
        await connection!.CloseAsync();

        // Assert
        await reconnected.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Assert.NotEqual(connection, resilientNpgsqlConnection.Connection);
    }

    [Fact]
    public async Task Reconnect_Should_CallOnDisconnect_When_ConnectionIsClosed()
    {
        // Arrange
        var onConnectCalled = 0;
        var onDisconnectCalled = 0;
        var reconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Func<CancellationToken, ValueTask> onConnect = _ =>
        {
            // Initialize connects once; the reconnect after the drop is the second connect.
            if (++onConnectCalled == 2)
            {
                reconnected.TrySetResult();
            }

            return ValueTask.CompletedTask;
        };
        Func<CancellationToken, ValueTask> onDisconnect = _ =>
        {
            onDisconnectCalled++;
            return ValueTask.CompletedTask;
        };

        var resilientNpgsqlConnection =
            new ResilientNpgsqlConnection(_events, ConnectionFactory, onConnect, onDisconnect);
        await resilientNpgsqlConnection.Initialize(CancellationToken.None);

        // Act
        await resilientNpgsqlConnection.Connection!.CloseAsync();

        // Assert
        await reconnected.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Assert.Equal(1, onDisconnectCalled);
        Assert.Equal(2, onConnectCalled);
    }

    [Fact]
    public async Task DisposeAsync_Should_CloseConnection_When_ReconnectIsUnderWay()
    {
        // arrange
        var connections = new List<NpgsqlConnection>();
        var reconnectStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async ValueTask<NpgsqlConnection> connectionFactory(CancellationToken cancellationToken)
        {
            var connection = new NpgsqlConnection(_resource.ConnectionString);

            if (connections.Count == 1)
            {
                reconnectStarted.TrySetResult();

                try
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // the reconnect opens its connection a moment after dispose has cancelled it
                    await Task.Delay(100, CancellationToken.None);
                    await connection.OpenAsync(CancellationToken.None);
                }
            }

            connections.Add(connection);
            return connection;
        }

        Func<CancellationToken, ValueTask> onConnect = _ => ValueTask.CompletedTask;
        Func<CancellationToken, ValueTask> onDisconnect = _ => ValueTask.CompletedTask;

        var resilientNpgsqlConnection =
            new ResilientNpgsqlConnection(_events, connectionFactory, onConnect, onDisconnect);
        await resilientNpgsqlConnection.Initialize(TestContext.Current.CancellationToken);
        await resilientNpgsqlConnection.Connection!.CloseAsync();
        await reconnectStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);

        // act
        await resilientNpgsqlConnection.DisposeAsync().AsTask().WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            [ConnectionState.Closed, ConnectionState.Closed],
            connections.Select(c => c.State).ToArray());
    }

    private ValueTask<NpgsqlConnection> ConnectionFactory(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_resource.ConnectionString);
        return ValueTask.FromResult(connection);
    }

    private async ValueTask<NpgsqlConnection> ConnectionFactoryAlreadyOpen(
        CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_resource.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
