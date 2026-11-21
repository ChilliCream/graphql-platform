using CookieCrumble.Resources;
using System.Data;
using HotChocolate.Tests;
using Npgsql;

namespace HotChocolate.Subscriptions.Postgres;

public class PostgresChannelWriterTests
    : IAsyncLifetime
{
    private readonly PostgreSqlResource _resource;
    private readonly string _dbName = $"DB_{Guid.NewGuid():N}";
    private readonly string _channelName;
    private readonly PostgresSubscriptionOptions _options;
    private readonly SubscriptionTestDiagnostics _events;

    public PostgresChannelWriterTests(PostgreSqlResource resource, ITestOutputHelper output)
    {
        _events = new SubscriptionTestDiagnostics(output);
        _resource = resource;
        _channelName = $"channel_{Guid.NewGuid():N}";
        _options = new PostgresSubscriptionOptions
        {
            ConnectionFactory = ConnectionFactory,
            ChannelName = _channelName
        };
    }

    [Fact]
    public async Task SendAsync_Should_WriteMessageToChannel_When_CalledWithValidInput()
    {
        // Arrange
        var postgresChannelWriter = new PostgresChannelWriter(_events, _options);
        await postgresChannelWriter.Initialize(CancellationToken.None);
        var message =
            PostgresMessageEnvelope.Create("test", "test", _options.MaxMessagePayloadSize);
        var testChannel = new TestChannel(SyncConnectionFactory, _channelName);

        // Act
        await postgresChannelWriter.SendAsync(message, CancellationToken.None);

        // Assert
        await testChannel.WaitForNotificationAsync();
        var result = Assert.Single(testChannel.ReceivedMessages);
        Assert.Equal("dGVzdA==:test", result[25..]);
    }

    [Fact]
    public async Task SendAsync_Should_WriteManyMessage_When_CalledManyTimes()
    {
        // Arrange
        var postgresChannelWriter = new PostgresChannelWriter(_events, _options);
        await postgresChannelWriter.Initialize(CancellationToken.None);
        var testChannel = new TestChannel(SyncConnectionFactory, _channelName);

        // Act
        await Parallel.ForEachAsync(Enumerable.Range(0, 1000),
            new ParallelOptions { MaxDegreeOfParallelism = 10 },
            async (_, _) =>
            {
                var message =
                    PostgresMessageEnvelope.Create("test", "test", _options.MaxMessagePayloadSize);

                await postgresChannelWriter.SendAsync(message, CancellationToken.None);
            });

        // Assert
        while (testChannel.ReceivedMessages.Count < 1000)
        {
            await testChannel.WaitForNotificationAsync().WaitAsync(
                TimeSpan.FromSeconds(10),
                TestContext.Current.CancellationToken);
        }

        Assert.Equal(1000, testChannel.ReceivedMessages.Count);
    }

    [Fact]
    public async Task Initialize_Should_InitializeResilientNpgsqlConnection_When_Called()
    {
        // Arrange
        var connected = false;
        var options = new PostgresSubscriptionOptions
        {
            ConnectionFactory = async ct =>
            {
                connected = true;
                return await ConnectionFactory(ct);
            },
            ChannelName = _channelName
        };
        var postgresChannelWriter = new PostgresChannelWriter(_events, options);

        // Act
        await postgresChannelWriter.Initialize(CancellationToken.None);

        // Assert
        SpinWait.SpinUntil(() => connected, TimeSpan.FromSeconds(5));
        Assert.True(connected);
    }

    [Fact]
    public async Task Initialize_Should_ReconnectOnConnectionDrop()
    {
        // Arrange
        var reconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        NpgsqlConnection? connection = null;
        var options = new PostgresSubscriptionOptions
        {
            ConnectionFactory = async ct =>
            {
                if (connection is null)
                {
                    connection = await ConnectionFactory(ct);
                    return connection;
                }

                reconnected.TrySetResult();

                return await ConnectionFactory(ct);
            },
            ChannelName = _channelName
        };
        var postgresChannelWriter = new PostgresChannelWriter(_events, options);
        await postgresChannelWriter.Initialize(CancellationToken.None);

        // Act
        await connection!.CloseAsync();

        // Assert
        await reconnected.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeConnection()
    {
        // Arrange
        NpgsqlConnection? connection = null;
        var options = new PostgresSubscriptionOptions
        {
            ConnectionFactory = async ct =>
            {
                connection = await ConnectionFactory(ct);
                return connection;
            },
            ChannelName = _channelName
        };
        var postgresChannelWriter = new PostgresChannelWriter(_events, options);
        await postgresChannelWriter.Initialize(CancellationToken.None);

        Assert.True(SpinWait.SpinUntil(
            () => connection!.State == ConnectionState.Open,
            TimeSpan.FromSeconds(5)));
        // Act
        await postgresChannelWriter.DisposeAsync();

        // Assert
        SpinWait.SpinUntil(
            () => connection!.State == ConnectionState.Closed,
            TimeSpan.FromSeconds(5));
        Assert.Equal(ConnectionState.Closed, connection!.State);
    }

    [Fact]
    public async Task DisposeAsync_Should_CloseConnection_When_SendIsUnderWay()
    {
        // arrange
        // pg_notify is shadowed to keep the send under way for 500 ms after the cancel, which can be raised twice
        await _resource.RunSqlScriptAsync(
            """
            CREATE FUNCTION public.pg_notify(channel text, message character varying)
            RETURNS void LANGUAGE plpgsql AS $$
            BEGIN
                BEGIN
                    PERFORM pg_catalog.pg_sleep(60);
                EXCEPTION WHEN query_canceled THEN
                    PERFORM pg_catalog.pg_sleep(0.5);
                END;
            EXCEPTION WHEN query_canceled THEN
                PERFORM pg_catalog.pg_sleep(0.5);
            END $$;
            """,
            _dbName);
        NpgsqlConnection? connection = null;
        var options = new PostgresSubscriptionOptions
        {
            ConnectionFactory = async ct =>
            {
                connection = await ConnectionFactory(ct);
                return connection;
            }
        };
        var postgresChannelWriter = new PostgresChannelWriter(_events, options);
        await postgresChannelWriter.Initialize(TestContext.Current.CancellationToken);
        var message =
            PostgresMessageEnvelope.Create("test", "test", options.MaxMessagePayloadSize);
        await postgresChannelWriter.SendAsync(message, TestContext.Current.CancellationToken);
        await WaitUntilSendIsUnderWayAsync();

        // act
        await postgresChannelWriter.DisposeAsync().AsTask().WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(ConnectionState.Closed, connection!.State);
    }

    [Fact]
    public async Task DisposeAsync_Should_CloseConnection_When_ReconnectIsUnderWay()
    {
        // arrange
        var connections = new List<NpgsqlConnection>();
        var reconnectStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposeCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var options = new PostgresSubscriptionOptions
        {
            ConnectionFactory = async ct =>
            {
                var connection = await ConnectionFactory(ct);
                await connection.OpenAsync(ct);
                connections.Add(connection);

                if (connections.Count == 2)
                {
                    // the reconnect hands over its connection only after dispose has been called
                    reconnectStarted.TrySetResult();
                    await disposeCalled.Task;
                }

                return connection;
            },
            ChannelName = _channelName
        };
        var postgresChannelWriter = new PostgresChannelWriter(_events, options);
        await postgresChannelWriter.Initialize(TestContext.Current.CancellationToken);
        await connections[0].CloseAsync();
        await reconnectStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);

        // act
        var dispose = postgresChannelWriter.DisposeAsync().AsTask();
        disposeCalled.SetResult();
        await dispose.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(ConnectionState.Closed, connections[1].State);
    }

    private async Task WaitUntilSendIsUnderWayAsync()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        await using var connection = _resource.GetConnection(_dbName);
        await connection.OpenAsync(timeout.Token);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT EXISTS (
                SELECT FROM pg_stat_activity
                WHERE datname = current_database() AND wait_event = 'PgSleep');
            """;

        while (await command.ExecuteScalarAsync(timeout.Token) is false)
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private ValueTask<NpgsqlConnection> ConnectionFactory(CancellationToken cancellationToken)
    {
        var connection = _resource.GetConnection(_dbName);

        return ValueTask.FromResult(connection);
    }

    private NpgsqlConnection SyncConnectionFactory()
    {
        var connection = _resource.GetConnection(_dbName);

        return connection;
    }

    /// <inheritdoc />
    public ValueTask InitializeAsync()
    {
        return new ValueTask(_resource.CreateDatabaseAsync(_dbName));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await using var connection = _resource.GetConnection(_dbName);
        NpgsqlConnection.ClearPool(connection);
    }
}
