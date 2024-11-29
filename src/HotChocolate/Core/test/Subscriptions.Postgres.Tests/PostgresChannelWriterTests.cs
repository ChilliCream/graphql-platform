using System.Data;
using HotChocolate.Tests;
using Npgsql;
using Squadron;
using Xunit.Abstractions;

namespace HotChocolate.Subscriptions.Postgres;

public class PostgresChannelWriterTests
    : IClassFixture<PostgreSqlResource>
    , IAsyncLifetime
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
            ConnectionFactory = ConnectionFactory, ChannelName = _channelName,
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
            new ParallelOptions { MaxDegreeOfParallelism = 10, },
            async (_, _) =>
            {
                var message =
                    PostgresMessageEnvelope.Create("test", "test", _options.MaxMessagePayloadSize);

                await postgresChannelWriter.SendAsync(message, CancellationToken.None);
            });

        // Assert
        while (testChannel.ReceivedMessages.Count < 1000)
        {
            await testChannel.WaitForNotificationAsync().WaitAsync(TimeSpan.FromSeconds(10));
        }

        Assert.Equal(1000, testChannel.ReceivedMessages.Count);
    }

    [Fact]
    public async Task Initialize_Should_InitializeResilientNpgsqlConnection_When_Called()
    {
        // Arrange
        var connected = false;
        var options = new PostgresSubscriptionOptions()
        {
            ConnectionFactory = async ct =>
            {
                connected = true;
                return await ConnectionFactory(ct);
            },
            ChannelName = _channelName,
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
        var reconnected = false;
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

                reconnected = true;

                return await ConnectionFactory(ct);
            },
            ChannelName = _channelName,
        };
        var postgresChannelWriter = new PostgresChannelWriter(_events, options);
        await postgresChannelWriter.Initialize(CancellationToken.None);

        // Act
        await connection!.CloseAsync();

        // Assert
        SpinWait.SpinUntil(() => reconnected, TimeSpan.FromSeconds(5));
        Assert.True(reconnected);
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeConnection()
    {
        // Arrange
        NpgsqlConnection? connection = null;
        var options = new PostgresSubscriptionOptions()
        {
            ConnectionFactory = async ct =>
            {
                connection = await ConnectionFactory(ct);
                return connection;
            },
            ChannelName = _channelName,
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
    public Task InitializeAsync()
    {
        return _resource.CreateDatabaseAsync(_dbName);
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;
}
