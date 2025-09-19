using Npgsql;

namespace HotChocolate.Subscriptions.Postgres;

public class TestChannel : IDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly string _channelName;
    private readonly Func<NpgsqlConnection> _connectionFactory;
    private readonly object _lock = new();

    public List<string> ReceivedMessages { get; } = [];

    public TestChannel(Func<NpgsqlConnection> connectionFactory, string channelName)
    {
        _connection = connectionFactory();
        _connectionFactory = connectionFactory;
        _connection.Open();

        _connection.Notification += (_, e) =>
        {
            if (e.Channel == channelName)
            {
                lock (_lock)
                {
                    ReceivedMessages.Add(e.Payload);
                }
            }
        };

        using var command = new NpgsqlCommand($"LISTEN {channelName}", _connection);
        command.ExecuteNonQuery();
        _channelName = channelName;
    }

    public async Task SendMessageAsync(string message)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync();
        await using var command =
            new NpgsqlCommand("SELECT pg_notify(@channel, @message);", connection);

        command.Parameters.AddWithValue("channel", _channelName);
        command.Parameters.AddWithValue("message", message);

        await command.ExecuteNonQueryAsync();
    }

    public async Task WaitForNotificationAsync()
    {
        await _connection.WaitAsync();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
