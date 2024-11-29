using System.Collections.Concurrent;
using HotChocolate.Tests;
using Npgsql;
using Squadron;
using Xunit.Abstractions;

namespace HotChocolate.Subscriptions.Postgres;

public class PostgresChannelTests
    : IClassFixture<PostgreSqlResource>
    , IAsyncLifetime
{
    private readonly PostgreSqlResource _resource;
    private readonly string _dbName = $"DB_{Guid.NewGuid():N}";
    private readonly string _channelName;
    private readonly SubscriptionTestDiagnostics _events;

    public PostgresChannelTests(PostgreSqlResource resource, ITestOutputHelper output)
    {
        _events = new SubscriptionTestDiagnostics(output);
        _resource = resource;
        _channelName = $"channel_{Guid.NewGuid():N}";
        _options = new PostgresSubscriptionOptions
        {
            ConnectionFactory = ConnectionFactory, ChannelName = _channelName,
        };
    }

    private PostgresSubscriptionOptions _options;

    [Fact]
    public async Task Subscribe_Should_ReceiveMessage_When_MessageIsSent()
    {
        // Arrange
        var topicName = "test";
        var channel = new PostgresChannel(_events, _options);
        await channel.EnsureInitialized(CancellationToken.None);

        var receivedMessages = new List<string>();
        var listener = new PostgresChannelObserver(topicName, e => receivedMessages.Add(e));

        // Act
        channel.Subscribe(listener);

        using var testChannel = new TestChannel(SyncConnectionFactory, _options.ChannelName);

        await testChannel.SendMessageAsync("aaaaaaaaaaaaaaaaaaaaaaaa:dGVzdA==:foobar");

        // Assert
        SpinWait.SpinUntil(() => receivedMessages.Count > 0, TimeSpan.FromSeconds(5));
        Assert.Equal("foobar", Assert.Single(receivedMessages));
    }

    [Fact]
    public async Task SendMessage_Should_SendMessageOverChannel()
    {
        // Arrange
        var channel = new PostgresChannel(_events, _options);
        await channel.EnsureInitialized(CancellationToken.None);

        using var testChannel = new TestChannel(SyncConnectionFactory, _channelName);

        // Act
        var message =
            PostgresMessageEnvelope.Create("test", "foobar", _options.MaxMessagePayloadSize);

        await channel.SendAsync(message, CancellationToken.None);

        // Assert
        await testChannel.WaitForNotificationAsync();

        Assert.Equal("dGVzdA==:foobar", Assert.Single(testChannel.ReceivedMessages)[25..]);
    }

    [Fact]
    public async Task Subscribe_Should_ReceiveManySequential_Messages()
    {
        // Arrange
        var topicName = "test";
        const int messageCount = 20;
        var channel = new PostgresChannel(_events, _options);
        await channel.EnsureInitialized(CancellationToken.None);

        var receivedMessages = new ConcurrentBag<string>();
        var listener = new PostgresChannelObserver(topicName, e => receivedMessages.Add(e));

        // Act
        channel.Subscribe(listener);

        using var testChannel = new TestChannel(SyncConnectionFactory, _channelName);

        for (var i = 0; i < messageCount; i++)
        {
            var messageId = i.ToString();
            messageId = messageId.PadLeft(24, '0');
            await testChannel.SendMessageAsync($"{messageId}:dGVzdA==:foobar");
        }

        // Assert
        SpinWait.SpinUntil(() => receivedMessages.Count == messageCount, TimeSpan.FromSeconds(5));
        Assert.Equal(messageCount, receivedMessages.Count);
        Assert.All(receivedMessages, m => Assert.Equal("foobar", m));
    }

    [Fact]
    public async Task Subscribe_Should_ReceiveManyConcurrentMessages_From_ManyConnections()
    {
        // Arrange
        var topicName = "test";

        var channel = new PostgresChannel(_events, _options);
        await channel.EnsureInitialized(CancellationToken.None);

        var receivedMessages = new ConcurrentBag<string>();
        var listener = new PostgresChannelObserver(topicName, e => receivedMessages.Add(e));

        // Act
        channel.Subscribe(listener);

        await Parallel.ForEachAsync(
            Enumerable.Range(0, 1000),
            new ParallelOptions { MaxDegreeOfParallelism = 10, },
            async (i, _) =>
            {
                using var testChannel = new TestChannel(SyncConnectionFactory, _channelName);
                var messageId = i.ToString();
                messageId = messageId.PadLeft(24, '0');
                await testChannel.SendMessageAsync($"{messageId}:dGVzdA==:foobar");
            });

        // Assert
        SpinWait.SpinUntil(() => receivedMessages.Count == 1000, TimeSpan.FromSeconds(5));
        Assert.Equal(1000, receivedMessages.Count);
        Assert.All(receivedMessages, m => Assert.Equal("foobar", m));
    }

    [Fact]
    public async Task Subscribe_Should_ReceiveManyConcurrentMessages_From_SinlgeConnections()
    {
        // Arrange
        var topicName = "test";

        var channel = new PostgresChannel(_events, _options);
        await channel.EnsureInitialized(CancellationToken.None);

        var receivedMessages = new ConcurrentBag<string>();
        var listener = new PostgresChannelObserver(topicName, e => receivedMessages.Add(e));

        // Act
        channel.Subscribe(listener);

        using var testChannel = new TestChannel(SyncConnectionFactory, _channelName);

        await Parallel.ForEachAsync(
            Enumerable.Range(0, 1000),
            new ParallelOptions { MaxDegreeOfParallelism = 10, },
            async (i, _) =>
            {
                var messageId = i.ToString();
                messageId = messageId.PadLeft(24, '0');
                await testChannel.SendMessageAsync($"{messageId}:dGVzdA==:foobar");
            });

        // Assert
        SpinWait.SpinUntil(() => receivedMessages.Count == 1000, TimeSpan.FromSeconds(5));
        Assert.Equal(1000, receivedMessages.Count);
        Assert.All(receivedMessages, m => Assert.Equal("foobar", m));
    }

    [Fact]
    public async Task SendAsync_Should_SendAndReceive()
    {
        // Arrange
        var topicName = "test";
        var channel = new PostgresChannel(_events, _options);
        await channel.EnsureInitialized(CancellationToken.None);

        var receivedMessages = new ConcurrentBag<string>();
        var listener = new PostgresChannelObserver(topicName, e => receivedMessages.Add(e));
        channel.Subscribe(listener);

        // Act
        var message =
            PostgresMessageEnvelope.Create("test", "foobar", _options.MaxMessagePayloadSize);

        await channel.SendAsync(message, CancellationToken.None);

        // Assert
        SpinWait.SpinUntil(() => receivedMessages.Count == 1, TimeSpan.FromSeconds(1));
        Assert.Equal("foobar", Assert.Single(receivedMessages));
    }

    [Fact]
    public async Task SendAsync_Should_SendAllMessages_When_CalledConcurrently()
    {
        // Arrange
        var topicName = "test";

        var channel = new PostgresChannel(_events, _options);
        await channel.EnsureInitialized(CancellationToken.None);

        var receivedMessages = new ConcurrentBag<string>();
        var listener = new PostgresChannelObserver(topicName, e => receivedMessages.Add(e));
        channel.Subscribe(listener);

        // Act
        await Parallel.ForEachAsync(
            Enumerable.Range(0, 1000),
            new ParallelOptions { MaxDegreeOfParallelism = 10, },
            async (_, ct) =>
            {
                var message = PostgresMessageEnvelope
                    .Create("test", "foobar", _options.MaxMessagePayloadSize);

                await channel.SendAsync(message, ct);
            });

        // Assert
        SpinWait.SpinUntil(() => receivedMessages.Count == 1000, TimeSpan.FromSeconds(1));
        Assert.Equal(1000, receivedMessages.Count);
        Assert.All(receivedMessages, m => Assert.Equal("foobar", m));
    }

    [Fact]
    public async Task Subscribe_Should_AllowForManySubscribers()
    {
        // Arrange
        var topicName = "test";

        var channel = new PostgresChannel(_events, _options);
        await channel.EnsureInitialized(CancellationToken.None);

        var receivedMessages = new ConcurrentBag<string>();

        for (var i = 0; i < 100; i++)
        {
            channel.Subscribe(new PostgresChannelObserver(topicName, e => receivedMessages.Add(e)));
        }

        // Act
        using var testChannel = new TestChannel(SyncConnectionFactory, _channelName);
        await testChannel.SendMessageAsync("aaaaaaaaaaaaaaaaaaaaaaaa:dGVzdA==:foobar");

        // Assert
        SpinWait.SpinUntil(() => receivedMessages.Count == 100, TimeSpan.FromSeconds(1));
        Assert.Equal(100, receivedMessages.Count);
        Assert.All(receivedMessages, m => Assert.Equal("foobar", m));
    }

    [Fact]
    public async Task Usubscribe_Should_StopListeningToMessages()
    {
        // Arrange
        var topicName = "test";

        var channel = new PostgresChannel(_events, _options);
        await channel.EnsureInitialized(CancellationToken.None);

        var receivedMessages = new ConcurrentBag<string>();
        var disposable =
            channel.Subscribe(new PostgresChannelObserver(topicName, e => receivedMessages.Add(e)));

        using var testChannel = new TestChannel(SyncConnectionFactory, _channelName);
        await testChannel.SendMessageAsync("aaaaaaaaaaaaaaaaaaaaaaaa:dGVzdA==:foobar");
        SpinWait.SpinUntil(() => receivedMessages.Count == 1, TimeSpan.FromSeconds(1));

        // Act
        disposable.Dispose();
        await testChannel.SendMessageAsync("aaaaaaaaaaaaaaaaaaaaaaaa:dGVzdA==:foobar");

        // Assert
        await Task.Delay(1000);
        Assert.Single(receivedMessages);
    }

    [Fact]
    public async Task Observable_Should_StayIntact_When_ReconnectAfterConnectionDrop()
    {
        // Arrange
        var topicName = "test";
        var reconnected = false;
        NpgsqlConnection? connection = null;
        var options = new PostgresSubscriptionOptions()
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
        var channel = new PostgresChannel(_events, options);
        await channel.EnsureInitialized(CancellationToken.None);

        using var testChannel = new TestChannel(SyncConnectionFactory, _channelName);

        var receivedMessages = new ConcurrentBag<string>();
        channel.Subscribe(new PostgresChannelObserver(topicName, e => receivedMessages.Add(e)));

        // Act
        SpinWait.SpinUntil(() => connection is not null, TimeSpan.FromSeconds(1));

        try
        {
            await connection!.DisposeAsync();
        }
        catch
        {
            // we will get a connection is waiting exception here
        }

        SpinWait.SpinUntil(() => reconnected, TimeSpan.FromSeconds(1));

        await testChannel.SendMessageAsync("aaaaaaaaaaaaaaaaaaaaaaaa:dGVzdA==:foobar");

        // Assert
        SpinWait.SpinUntil(() => receivedMessages.Count == 1, TimeSpan.FromSeconds(1));
        Assert.Single(receivedMessages);
    }

    [Fact]
    public async Task Observable_Should_StayIntact_When_ReconnectAfterConnectionDropMultipleTries()
    {
        // Arrange
        var topicName = "test";
        var reconnected = false;
        var tries = 0;
        NpgsqlConnection? connection = null;
        var options = new PostgresSubscriptionOptions()
        {
            ConnectionFactory = async ct =>
            {
                if (connection is null)
                {
                    connection = await ConnectionFactory(ct);
                    return connection;
                }

                tries++;
                if (tries < 3)
                {
                    throw new Exception("Test");
                }

                reconnected = true;

                return await ConnectionFactory(ct);
            },
            ChannelName = _channelName,
        };
        var channel = new PostgresChannel(_events, options);
        await channel.EnsureInitialized(CancellationToken.None);

        using var testChannel = new TestChannel(SyncConnectionFactory, _channelName);

        var receivedMessages = new ConcurrentBag<string>();
        channel.Subscribe(new PostgresChannelObserver(topicName, e => receivedMessages.Add(e)));

        // Act
        SpinWait.SpinUntil(() => connection is not null, TimeSpan.FromSeconds(1));

        try
        {
            await connection!.DisposeAsync();
        }
        catch
        {
            // we will get a connection is waiting exception here
        }

        SpinWait.SpinUntil(() => reconnected, TimeSpan.FromSeconds(1));

        await testChannel.SendMessageAsync("aaaaaaaaaaaaaaaaaaaaaaaa:dGVzdA==:foobar");

        // Assert
        SpinWait.SpinUntil(() => receivedMessages.Count == 1, TimeSpan.FromSeconds(1));
        Assert.Single(receivedMessages);
    }

    private NpgsqlConnection SyncConnectionFactory()
    {
        var connection = _resource.GetConnection(_dbName);

        return connection;
    }

    private ValueTask<NpgsqlConnection> ConnectionFactory(CancellationToken cancellationToken)
    {
        var connection = _resource.GetConnection(_dbName);

        return ValueTask.FromResult(connection);
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _resource.CreateDatabaseAsync(_dbName);
    }

    /// <inheritdoc />
    public Task DisposeAsync()
        => Task.CompletedTask;
}
