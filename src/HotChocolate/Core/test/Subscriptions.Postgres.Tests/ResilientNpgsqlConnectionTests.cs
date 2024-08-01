using HotChocolate.Tests;
using Npgsql;
using Squadron;
using Xunit.Abstractions;

namespace HotChocolate.Subscriptions.Postgres;

public class ResilientNpgsqlConnectionTests : IClassFixture<PostgreSqlResource>
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
        Func<CancellationToken, ValueTask> onConnect = _ => ValueTask.CompletedTask;
        Func<CancellationToken, ValueTask> onDisconnect = _ => ValueTask.CompletedTask;

        var resilientNpgsqlConnection =
            new ResilientNpgsqlConnection(_events, ConnectionFactory, onConnect, onDisconnect);

        await resilientNpgsqlConnection.Initialize(CancellationToken.None);
        var connection = resilientNpgsqlConnection.Connection;

        // Act
        await connection!.CloseAsync();

        // Assert
        SpinWait.SpinUntil(
            () => connection != resilientNpgsqlConnection.Connection,
            TimeSpan.FromSeconds(1));
        Assert.NotEqual(connection, resilientNpgsqlConnection.Connection);
    }

    [Fact]
    public async Task Reconnect_Should_CallOnDisconnect_When_ConnectionIsClosed()
    {
        // Arrange
        var onConnectCalled = 0;
        var onDisconnectCalled = 0;

        Func<CancellationToken, ValueTask> onConnect = _ =>
        {
            onConnectCalled++;
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
        SpinWait.SpinUntil(
            () => onDisconnectCalled == 1 && onConnectCalled == 2,
            TimeSpan.FromSeconds(1));

        Assert.Equal(1, onDisconnectCalled);
        Assert.Equal(2, onConnectCalled);
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
