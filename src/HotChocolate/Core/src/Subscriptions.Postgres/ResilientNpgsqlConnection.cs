using System.Data;
using HotChocolate.Subscriptions.Diagnostics;
using Npgsql;
using static HotChocolate.Subscriptions.Postgres.PostgresResources;

namespace HotChocolate.Subscriptions.Postgres;

internal sealed class ResilientNpgsqlConnection : IAsyncDisposable
{
    private const int _waitOnFailureInMs = 500;

    private readonly ISubscriptionDiagnosticEvents _diagnosticEvents;
    private readonly Func<CancellationToken, ValueTask<NpgsqlConnection>> _connectionFactory;
    private readonly Func<CancellationToken, ValueTask> _onConnect;
    private readonly Func<CancellationToken, ValueTask> _onDisconnect;
    private readonly AsyncTaskDispatcher _asyncTaskDispatcher;
    private readonly SemaphoreSlim _sync = new(1, 1);

    public ResilientNpgsqlConnection(
        ISubscriptionDiagnosticEvents diagnosticEvents,
        Func<CancellationToken, ValueTask<NpgsqlConnection>> connectionFactory,
        Func<CancellationToken, ValueTask> onConnect,
        Func<CancellationToken, ValueTask> onDisconnect)
    {
        _diagnosticEvents = diagnosticEvents;
        _connectionFactory = connectionFactory;
        _onConnect = onConnect;
        _onDisconnect = onDisconnect;
        _asyncTaskDispatcher = new AsyncTaskDispatcher(OnReconnectAsync);
    }

    public NpgsqlConnection? Connection { get; private set; }

    public async Task Initialize(CancellationToken cancellationToken)
    {
        await _asyncTaskDispatcher.Initialize(cancellationToken);
    }

    private async Task OnReconnectAsync(CancellationToken cancellationToken)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            await Disconnect(cancellationToken);
            await Connect(cancellationToken);
        }
        catch (Exception ex)
        {
            // when we fail to reconnect, it's probably because we could establish a connection to
            // the database. We need to trigger the reconnection again, so that it tries to connect
            // again.
            // To avoid uncontrolled requests to the database, we wait waitTimeInMs before we
            // trigger

            var message = string
                .Format(ResilientConnection_FailedToConnect, _waitOnFailureInMs, ex.Message);
            _diagnosticEvents.ProviderInfo(message);

            try
            {
                await Task.Delay(_waitOnFailureInMs, cancellationToken);
            }
            catch
            {
                // make sure that in case of a cancellation or other issues, we do not deadlock the
                // reconnection
            }

            _asyncTaskDispatcher.Dispatch();
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task Connect(CancellationToken cancellationToken)
    {
        var connection = await _connectionFactory(cancellationToken);
        if (connection.State == ConnectionState.Closed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        connection.StateChange += OnConnectionStateChanged;
        Connection = connection;

        await _onConnect(cancellationToken);
    }

    private async Task Disconnect(CancellationToken cancellationToken)
    {
        try
        {
            if (Connection is not null)
            {
                Connection.StateChange -= OnConnectionStateChanged;

                try
                {
                    await _onDisconnect(cancellationToken);
                }
                catch (Exception)
                {
                    // on disconnect we ignore all exceptions
                }

                await Connection.DisposeAsync();
            }
        }
        catch (Exception)
        {
            // on disconnect we ignore all exceptions
        }

        Connection = null;
    }

    private void OnConnectionStateChanged(object sender, StateChangeEventArgs e)
    {
        if (e.CurrentState is ConnectionState.Broken or ConnectionState.Closed)
        {
            _diagnosticEvents.ProviderInfo(ResilientConnection_LostConnection);
            _asyncTaskDispatcher.Dispatch();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Disconnect(CancellationToken.None);
    }
}
